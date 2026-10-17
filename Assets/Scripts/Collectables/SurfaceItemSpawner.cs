using System;
using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
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
/// A killed guard reappears next to its item after a delay.
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

    private Player _player;
    private readonly Dictionary<Monster, CollectableItem> _itemByMonster = new Dictionary<Monster, CollectableItem>();
    private readonly Dictionary<CollectableItem, Monster> _guardByItem = new Dictionary<CollectableItem, Monster>();
    // Collected capsules that wait for their item to leave the player's inventory.
    private readonly List<CollectableItem> _pending = new List<CollectableItem>();

    /// <summary>Remembers the player for the guard monsters to chase and spawns everything.</summary>
    public void Init(Player player)
    {
        _player = player;

        if (_player != null)
        {
            Inventory inventory = _player.Inventory;
            inventory.Changed += RespawnDropped;
            Disposable.Create(() => inventory.Changed -= RespawnDropped).AddTo(Subscriptions);
        }

        Init();
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
        IReadOnlyList<ItemData> carried = _player.Inventory.Items;

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
    // A killed guard is inactive and reappears next to the item anyway.
    protected override void OnRespawned(CollectableItem item)
    {
        if (!_guardByItem.TryGetValue(item, out Monster monster) || monster == null || !monster.isActiveAndEnabled)
            return;

        if (monster.TryGetComponent(out MonsterChaser chaser))
            chaser.Guard(GetGuardPosition(item));
    }

    private void SpawnGuard(Monster prefab, CollectableItem item)
    {
        Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        Monster monster = Instantiate(prefab, GetGuardPosition(item), rotation);
        monster.DestroyOnDeath = false;
        _itemByMonster.Add(monster, item);
        _guardByItem.Add(item, monster);

        monster.Died
            .Subscribe(dead => StartCoroutine(RespawnGuardAfterDelay(dead)))
            .AddTo(Subscriptions);

        if (!monster.TryGetComponent(out MonsterChaser chaser))
            return;

        chaser.Init(_player);

        // The item is taken: its guard chases the player. Killed guards are inactive and don't react.
        item.Collected
            .Where(_ => chaser != null && chaser.isActiveAndEnabled)
            .Subscribe(_ => chaser.StartChase())
            .AddTo(Subscriptions);
    }

    private IEnumerator RespawnGuardAfterDelay(Monster monster)
    {
        yield return new WaitForSeconds(_monsterRespawnDelay);

        if (monster == null || !_itemByMonster.TryGetValue(monster, out CollectableItem item) || item == null)
            yield break;

        monster.transform.position = GetGuardPosition(item);
        monster.gameObject.SetActive(true);
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
