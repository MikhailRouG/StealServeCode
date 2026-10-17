using UnityEngine;

/// <summary>
/// Player root: gives access to the player's components and sets up its start state.
/// Initialized by <see cref="Bootstrap"/>.
/// </summary>
[RequireComponent(typeof(PlayerMove))]
[RequireComponent(typeof(PlayerCollector))]
public class Player : MonoBehaviour
{
    [SerializeField] private Inventory _inventory = new Inventory();
    [SerializeField] private PlayerHealth _health;
    [SerializeField] private Wallet _wallet = new Wallet();
    [SerializeField] private PlayerLevel _level = new PlayerLevel();

    private readonly PlayerDiscoveries _discoveries = new PlayerDiscoveries();
    private CharacterController _controller;

    public PlayerMove Move { get; private set; }
    public PlayerHealth Health => _health;
    public Inventory Inventory => _inventory;
    public Wallet Wallet => _wallet;
    public PlayerLevel Level => _level;
    public PlayerDiscoveries Discoveries => _discoveries;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        Move = GetComponent<PlayerMove>();

        _level.Recalculate();
        Move.SetLevel(_level.Level);
        _level.LevelUp += OnLevelUp;
    }

    private void OnDestroy()
    {
        if (_health != null)
            _health.Died -= OnDied;

        _level.LevelUp -= OnLevelUp;
    }

    public void Init(Vector3 position, Quaternion rotation, int maxHealth)
    {
        Teleport(position, rotation);
        _health = new PlayerHealth(maxHealth);
        _health.Died += OnDied;
    }

    /// <summary>Moves the player to the point, restores full health and turns the player back on.</summary>
    public void Respawn(Vector3 position, Quaternion rotation)
    {
        Teleport(position, rotation);
        _health.Restore();
        gameObject.SetActive(true);
    }

    // A dead player loses all items and is turned off until respawned.
    private void OnDied()
    {
        _inventory.Clear();
        gameObject.SetActive(false);
    }

    private void OnLevelUp(int newLevel) => Move.SetLevel(newLevel);

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        Move.Stop();

        // CharacterController overrides transform changes while enabled.
        _controller.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        _controller.enabled = true;
    }
}
