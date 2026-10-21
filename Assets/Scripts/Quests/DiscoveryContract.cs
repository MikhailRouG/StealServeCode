using UnityEngine;
using Zenject;

/// <summary>
/// Watches the player's <see cref="PlayerDiscoveries"/> and grants the experience of each
/// <see cref="DiscoveryContractData"/> when its item is delivered to the hub for the first time.
/// </summary>
public class DiscoveryContract : MonoBehaviour
{
    [SerializeField] private DiscoveryContractData[] _contracts;

    private PlayerDiscoveries _discoveries;
    private PlayerLevel _level;

    [Inject]
    private void Construct(PlayerDiscoveries discoveries, PlayerLevel level)
    {
        _discoveries = discoveries;
        _level = level;
        _discoveries.Discovered += OnDiscovered;
    }

    private void OnDestroy()
    {
        if (_discoveries != null)
            _discoveries.Discovered -= OnDiscovered;
    }

    private void OnDiscovered(ItemData item)
    {
        if (_contracts == null)
            return;

        foreach (DiscoveryContractData contract in _contracts)
        {
            if (contract != null && contract.Item == item)
                _level.AddExperience(contract.Experience);
        }
    }
}
