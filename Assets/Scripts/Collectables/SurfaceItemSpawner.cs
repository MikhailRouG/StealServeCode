using System;
using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

[Serializable]
public class ItemSpawnEntry
{
    public ItemData Item;
    [Min(0)] public int Count = 5;
    [Tooltip("Monster that guards every item of this entry and chases the player who picks it up. Optional.")]
    public Monster Monster;
}

/// <summary>
/// Places item capsules on the surface. A collected item reappears at another point as soon as
/// the player no longer carries an item of its type (delivered, unloaded at the hub, lost on death).
/// An item can have a guard monster. The spawner tracks whether the item is taken and switches its guard:
/// taken — the guard chases the player; back on the map — the guard stops and walks to it.
/// A killed guard reappears next to its item after a delay. Guards are created through Zenject, so they get their services injected.
/// With a <see cref="Zone"/> on the same object everything runs only while the zone is active (the player is near):
/// capsules and standing guards are turned off with it, but a guard that chases the player keeps chasing,
/// walks back to its item once the chase is over and only then hides.
/// </summary>
public class SurfaceItemSpawner : SurfaceSpawner<CollectableItem>
{
    [Header("Spawns")]
    [Tooltip("Capsule prefab that is created for every item and shows the item's model.")]
    [SerializeField] private CollectableItem _capsulePrefab;
    [SerializeField] private ItemSpawnEntry[] _spawns;

    [Header("Guard monsters")]
    [Tooltip("Monsters appear at this distance from their item, random between X and Y.")]
    [SerializeField] private Vector2 _guardRadius = new Vector2(1f, 3f);
    [Tooltip("Seconds a killed monster stays hidden before it reappears next to its item.")]
    [SerializeField, Min(0f)] private float _monsterRespawnDelay = 5f;
    [Tooltip("Layers a monster can stand on.")]
    [SerializeField] private LayerMask _groundMask = ~0;

    private const float GroundRaycastHeight = 2f;
    private const int MaxGuardPlacementAttempts = 10;

    private Inventory _inventory;
    private IInstantiator _instantiator;
    private readonly Dictionary<Monster, CollectableItem> _itemByMonster = new Dictionary<Monster, CollectableItem>();
    private readonly Dictionary<CollectableItem, Monster> _guardByItem = new Dictionary<CollectableItem, Monster>();
    // Collected capsules that wait for their item to leave the player's inventory.
    private readonly List<CollectableItem> _pending = new List<CollectableItem>();
    // Killed guards wait for their respawn delay; showing the zone must not bring them back early.
    private readonly HashSet<Monster> _deadGuards = new HashSet<Monster>();
    private bool _isZoneActive = true;

    [Inject]
    private void Construct(Inventory inventory, IInstantiator instantiator)
    {
        _inventory = inventory;
        _instantiator = instantiator;

        _inventory.Changed += RespawnDropped;
        Disposable.Create(() => _inventory.Changed -= RespawnDropped).AddTo(Subscriptions);
    }

    protected override void Start()
    {
        base.Start();

        // Without a zone everything stays on all the time.
        if (TryGetComponent(out Zone zone))
        {
            zone.IsActive
                .Subscribe(SetZoneActive)
                .AddTo(Subscriptions);
        }
    }

    protected override void SpawnAll()
    {
        if (_capsulePrefab == null || _spawns == null)
            return;

        foreach (ItemSpawnEntry entry in _spawns)
        {
            if (entry.Item == null)
                continue;

            for (int i = 0; i < entry.Count; i++)
            {
                CollectableItem item = Spawn(_capsulePrefab);
                item.SetData(entry.Item);

                if (entry.Monster != null)
                    SpawnGuard(entry.Monster, item);
            }
        }
    }

    protected override void OnSpawned(CollectableItem item)
    {
        item.DestroyOnCollect = false;
        item.Collected
            .Subscribe(OnItemCollected)
            .AddTo(Subscriptions);
    }

    // Collected after the item is already in the inventory (see PlayerCollector), so it waits for it to leave.
    private void OnItemCollected(CollectableItem item)
    {
        _pending.Add(item);
    }

    private void RespawnDropped()
    {
        IReadOnlyList<ItemData> carried = _inventory.Items;

        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            CollectableItem item = _pending[i];
            if (item != null && Contains(carried, item.Data))
                continue;

            _pending.RemoveAt(i);
            Respawn(item);
        }
    }

    private static bool Contains(IReadOnlyList<ItemData> items, ItemData item)
    {
        foreach (ItemData carried in items)
        {
            if (carried == item)
                return true;
        }
        return false;
    }

    // The item left the player and is back on the map: its guard stops chasing and walks to it.
    // A killed guard reappears next to the item anyway; one hidden with the zone just moves there.
    protected override void OnRespawned(CollectableItem item)
    {
        if (!_guardByItem.TryGetValue(item, out Monster monster) || monster == null || _deadGuards.Contains(monster))
            return;

        if (!monster.gameObject.activeSelf)
        {
            monster.transform.position = GetGuardPosition(item);
            return;
        }

        if (monster.TryGetComponent(out MonsterChaser chaser))
            chaser.Guard(GetGuardPosition(item));
    }

    // Capsules are turned off with the container; guards one by one, since a busy one (chasing or walking back) stays.
    private void SetZoneActive(bool active)
    {
        _isZoneActive = active;
        SetSpawnedActive(active);

        foreach (Monster monster in _itemByMonster.Keys)
        {
            if (monster == null || _deadGuards.Contains(monster))
                continue;

            if (active)
                monster.gameObject.SetActive(true);
            else if (!IsBusy(monster))
                monster.gameObject.SetActive(false);
        }
    }

    private static bool IsBusy(Monster monster) =>
        monster.TryGetComponent(out MonsterChaser chaser) && IsBusy(chaser);

    private static bool IsBusy(MonsterChaser chaser) => chaser.IsChasing.CurrentValue || chaser.IsReturning.CurrentValue;

    // The guard stayed on while the zone was turned off (it was chasing or walking back) and is now standing at its item: it hides.
    private void OnGuardIdle(Monster monster, MonsterChaser chaser)
    {
        // Not while the guard is being killed or turned off (its OnDisable resets the flags too).
        if (_isZoneActive || _deadGuards.Contains(monster) || !chaser.isActiveAndEnabled || IsBusy(chaser))
            return;

        monster.gameObject.SetActive(false);
    }

    private void SpawnGuard(Monster prefab, CollectableItem item)
    {
        Monster monster = _instantiator.InstantiatePrefabForComponent<Monster>(
            prefab, GetGuardPosition(item), prefab.transform.rotation, null);
        monster.DestroyOnDeath = false;
        _itemByMonster.Add(monster, item);
        _guardByItem.Add(item, monster);

        // Died fires before the monster turns itself off, so it already counts as dead in OnGuardIdle.
        monster.Died
            .Subscribe(OnGuardDied)
            .AddTo(Subscriptions);

        if (!monster.TryGetComponent(out MonsterChaser chaser))
            return;

        // The item is taken: its guard chases the player. Killed guards are inactive and don't react.
        item.Collected
            .Where(_ => chaser != null && chaser.isActiveAndEnabled)
            .Subscribe(_ => chaser.StartChase())
            .AddTo(Subscriptions);

        // The chase ends or the guard reaches its item; OnGuardIdle checks that both are over.
        chaser.IsChasing
            .Skip(1)
            .Where(isChasing => !isChasing)
            .Subscribe(_ => OnGuardIdle(monster, chaser))
            .AddTo(Subscriptions);
        chaser.IsReturning
            .Skip(1)
            .Where(isReturning => !isReturning)
            .Subscribe(_ => OnGuardIdle(monster, chaser))
            .AddTo(Subscriptions);
    }

    private void OnGuardDied(Monster monster)
    {
        _deadGuards.Add(monster);
        StartCoroutine(RespawnGuardAfterDelay(monster));
    }

    // Back next to the item; while the zone is off it stays hidden until the zone turns on.
    private IEnumerator RespawnGuardAfterDelay(Monster monster)
    {
        yield return new WaitForSeconds(_monsterRespawnDelay);

        _deadGuards.Remove(monster);
        if (monster == null || !_itemByMonster.TryGetValue(monster, out CollectableItem item) || item == null)
            yield break;

        monster.transform.position = GetGuardPosition(item);
        monster.gameObject.SetActive(_isZoneActive);
    }

    // Monsters have no gravity, so they are placed right on the ground.
    private Vector3 GetGuardPosition(CollectableItem item)
    {
        Vector3 center = item.transform.position;
        for (int attempt = 0; attempt < MaxGuardPlacementAttempts; attempt++)
        {
            if (TryGetPointNear(center, out Vector3 point))
                return point;
        }

        // The item floats at the height offset above the ground.
        return center - Vector3.up * HeightOffset;
    }

    // Random ground point directly to the left or right of the center (along X, same Z), found by a raycast down onto the scene.
    private bool TryGetPointNear(Vector3 center, out Vector3 point)
    {
        float minRadius = Mathf.Max(0f, Mathf.Min(_guardRadius.x, _guardRadius.y));
        float maxRadius = Mathf.Max(_guardRadius.x, _guardRadius.y);
        float side = Random.value < 0.5f ? -1f : 1f;
        float offsetX = side * Random.Range(minRadius, maxRadius);

        Vector3 origin = new Vector3(center.x + offsetX, center.y + GroundRaycastHeight, center.z);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, GroundRaycastHeight * 3f, _groundMask, QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            return true;
        }

        point = default;
        return false;
    }
}
