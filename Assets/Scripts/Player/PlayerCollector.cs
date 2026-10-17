using UnityEngine;

/// <summary>
/// Picks up ICollectable objects when the player touches their trigger collider and puts them into the <see cref="Player"/> inventory.
/// </summary>
[RequireComponent(typeof(Player))]
public class PlayerCollector : MonoBehaviour
{
    private Player _player;

    private void Awake()
    {
        _player = GetComponent<Player>();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        TryCollect(hit.collider);
    }

    private void TryCollect(Collider other)
    {
        if (!other.TryGetComponent(out ICollectable collectable) || !collectable.IsAvailable)
            return;

        ItemData data = collectable.Data;
        if (data != null && !_player.Inventory.Add(data))
            return;

        collectable.OnCollected();
    }
}
