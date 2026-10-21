using UnityEngine;
using Zenject;

/// <summary>
/// Watches the player's <see cref="PlayerDiscoveries"/> and grants the reward item once every item
/// in the <see cref="ItemSetQuestData"/> set has been delivered to the hub at least once.
/// Unlike <see cref="RequestPoint"/>, this needs no physical trigger zone — it checks on every discovery.
/// </summary>
public class ItemSetQuest : MonoBehaviour
{
    [SerializeField] private ItemSetQuestData _quest;

    private PlayerDiscoveries _discoveries;
    private Inventory _inventory;
    private bool _completed;

    [Inject]
    private void Construct(PlayerDiscoveries discoveries, Inventory inventory)
    {
        _discoveries = discoveries;
        _inventory = inventory;
        _discoveries.Discovered += OnDiscovered;
    }

    private void Start()
    {
        TryComplete();
    }

    private void OnDestroy()
    {
        if (_discoveries != null)
            _discoveries.Discovered -= OnDiscovered;
    }

    private void OnDiscovered(ItemData item) => TryComplete();

    private void TryComplete()
    {
        if (_completed || _quest == null || _quest.RequiredItems == null)
            return;

        foreach (ItemData required in _quest.RequiredItems)
        {
            if (!_discoveries.HasDelivered(required))
                return;
        }

        _completed = true;
        _inventory.Add(_quest.RewardItem);
    }
}
