using UnityEngine;

/// <summary>
/// Watches the player's <see cref="PlayerDiscoveries"/> and grants the reward item once every item
/// in the <see cref="ItemSetQuestData"/> set has been delivered to the hub at least once.
/// Unlike <see cref="RequestPoint"/>, this needs no physical trigger zone — it checks on every discovery.
/// </summary>
public class ItemSetQuest : MonoBehaviour
{
    [SerializeField] private ItemSetQuestData _quest;

    private Player _player;
    private bool _completed;

    /// <summary>Starts watching for the quest's items. Called once by <see cref="Bootstrap"/>.</summary>
    public void Init(Player player)
    {
        _player = player;
        _player.Discoveries.Discovered += OnDiscovered;
        TryComplete();
    }

    private void OnDestroy()
    {
        if (_player != null)
            _player.Discoveries.Discovered -= OnDiscovered;
    }

    private void OnDiscovered(ItemData item) => TryComplete();

    private void TryComplete()
    {
        if (_completed || _quest == null || _quest.RequiredItems == null)
            return;

        foreach (ItemData required in _quest.RequiredItems)
        {
            if (!_player.Discoveries.HasDelivered(required))
                return;
        }

        _completed = true;
        _player.Inventory.Add(_quest.RewardItem);
    }
}
