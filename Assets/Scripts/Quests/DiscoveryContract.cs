using UnityEngine;

/// <summary>
/// Watches the player's <see cref="PlayerDiscoveries"/> and grants the experience of each
/// <see cref="DiscoveryContractData"/> when its item is delivered to the hub for the first time.
/// </summary>
public class DiscoveryContract : MonoBehaviour
{
    [SerializeField] private DiscoveryContractData[] _contracts;

    private Player _player;

    /// <summary>Starts watching for discoveries. Called once by <see cref="Bootstrap"/>.</summary>
    public void Init(Player player)
    {
        _player = player;
        _player.Discoveries.Discovered += OnDiscovered;
    }

    private void OnDestroy()
    {
        if (_player != null)
            _player.Discoveries.Discovered -= OnDiscovered;
    }

    private void OnDiscovered(ItemData item)
    {
        if (_contracts == null)
            return;

        foreach (DiscoveryContractData contract in _contracts)
        {
            if (contract != null && contract.Item == item)
                _player.Level.AddExperience(contract.Experience);
        }
    }
}
