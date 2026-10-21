using UnityEngine;
using Zenject;

/// <summary>
/// Player root: gives access to the player's components and models. Bound in <see cref="GameInstaller"/>.
/// The models (inventory, wallet, level, health...) are created by the container, not stored here;
/// the player only exposes them for code that gets the player from a collision (hub, request points)
/// and for its own components. Everything else can inject the models directly.
/// Placed at the spawn point by <see cref="PlayerRespawner"/>.
/// </summary>
[RequireComponent(typeof(PlayerMove))]
[RequireComponent(typeof(PlayerCollector))]
public class Player : MonoBehaviour
{
    private CharacterController _controller;

    public PlayerMove Move { get; private set; }
    public PlayerHealth Health { get; private set; }
    public Inventory Inventory { get; private set; }
    public Wallet Wallet { get; private set; }
    public PlayerLevel Level { get; private set; }
    public PlayerStats Stats { get; private set; }
    public PlayerDiscoveries Discoveries { get; private set; }

    [Inject]
    private void Construct(
        PlayerHealth health,
        Inventory inventory,
        Wallet wallet,
        PlayerLevel level,
        PlayerStats stats,
        PlayerDiscoveries discoveries)
    {
        _controller = GetComponent<CharacterController>();
        Move = GetComponent<PlayerMove>();

        Health = health;
        Inventory = inventory;
        Wallet = wallet;
        Level = level;
        Stats = stats;
        Discoveries = discoveries;

        Health.Died += OnDied;
    }

    private void OnDestroy()
    {
        if (Health != null)
            Health.Died -= OnDied;
    }

    /// <summary>Moves the player to the point, restores full health and turns the player back on.</summary>
    public void Respawn(Vector3 position, Quaternion rotation)
    {
        Teleport(position, rotation);
        Health.Restore();
        gameObject.SetActive(true);
    }

    // A dead player loses all items and is turned off until respawned.
    private void OnDied()
    {
        Inventory.Clear();
        gameObject.SetActive(false);
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        Move.Stop();

        // CharacterController overrides transform changes while enabled.
        _controller.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        _controller.enabled = true;
    }
}
