using System.Collections;
using UnityEngine;

/// <summary>
/// Scene entry point: initializes systems in a fixed order once all Awake calls are done.
/// Put one on an empty object in the scene. Empty spawner lists are looked up in the scene.
/// </summary>
public class Bootstrap : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Player _player;
    [Tooltip("Where the player starts and respawns after death. If empty, the player's start position in the scene is used.")]
    [SerializeField] private Transform _playerSpawnPoint;
    [SerializeField, Min(1)] private int _playerMaxHealth = 100;
    [Tooltip("Seconds between the player's death and respawn.")]
    [SerializeField, Min(0f)] private float _playerRespawnDelay = 5f;

    [Header("Spawners")]
    [SerializeField] private SurfaceItemSpawner[] _itemSpawners;
    [SerializeField] private ClientSpawner[] _clientSpawners;
    [SerializeField] private ClientSales[] _clientSales;

    [Header("Quests")]
    [SerializeField] private ItemSetQuest[] _itemSetQuests;
    [SerializeField] private DiscoveryContract[] _discoveryContracts;

    [Header("UI")]
    [Tooltip("Root of all UI canvases. If empty, the first GameUI in the scene is used.")]
    [SerializeField] private GameUI _ui;

    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;

    private void Start()
    {
        ResolveReferences();

        if (_player == null)
        {
            Debug.LogError($"{nameof(Bootstrap)}: no player found.", this);
            return;
        }

        InitPlayer();

        if (_ui != null)
            _ui.Init(_player);

        foreach (SurfaceItemSpawner spawner in _itemSpawners)
        {
            if (spawner != null)
                spawner.Init(_player);
        }
        foreach (ClientSales sales in _clientSales)
        {
            if (sales != null)
                sales.Init(_player.Wallet);
        }

        foreach (ClientSpawner spawner in _clientSpawners)
        {
            if (spawner != null)
                spawner.Init();
        }

        foreach (ItemSetQuest quest in _itemSetQuests)
        {
            if (quest != null)
                quest.Init(_player);
        }

        foreach (DiscoveryContract contract in _discoveryContracts)
        {
            if (contract != null)
                contract.Init(_player);
        }
    }

    private void OnDestroy()
    {
        if (_player != null && _player.Health != null)
            _player.Health.Died -= OnPlayerDied;
    }

    private void InitPlayer()
    {
        Transform point = _playerSpawnPoint != null ? _playerSpawnPoint : _player.transform;
        _spawnPosition = point.position;
        _spawnRotation = point.rotation;

        _player.Init(_spawnPosition, _spawnRotation, _playerMaxHealth);
        _player.Health.Died += OnPlayerDied;
    }

    private void OnPlayerDied()
    {
        StartCoroutine(RespawnPlayerAfterDelay());
    }

    private IEnumerator RespawnPlayerAfterDelay()
    {
        yield return new WaitForSeconds(_playerRespawnDelay);
        _player.Respawn(_spawnPosition, _spawnRotation);
    }

    private void ResolveReferences()
    {
        if (_player == null)
            _player = FindFirstObjectByType<Player>();

        if (_ui == null)
            _ui = FindFirstObjectByType<GameUI>();

        if (_itemSpawners == null || _itemSpawners.Length == 0)
            _itemSpawners = FindObjectsByType<SurfaceItemSpawner>(FindObjectsSortMode.None);
        if (_clientSpawners == null || _clientSpawners.Length == 0)
            _clientSpawners = FindObjectsByType<ClientSpawner>(FindObjectsSortMode.None);

        if (_clientSales == null || _clientSales.Length == 0)
            _clientSales = FindObjectsByType<ClientSales>(FindObjectsSortMode.None);

        if (_itemSetQuests == null || _itemSetQuests.Length == 0)
            _itemSetQuests = FindObjectsByType<ItemSetQuest>(FindObjectsSortMode.None);

        if (_discoveryContracts == null || _discoveryContracts.Length == 0)
            _discoveryContracts = FindObjectsByType<DiscoveryContract>(FindObjectsSortMode.None);
    }
}
