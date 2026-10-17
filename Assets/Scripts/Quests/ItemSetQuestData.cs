using UnityEngine;

/// <summary>
/// A collection quest: once every item in the set has been delivered to the hub at least once
/// (see <see cref="PlayerDiscoveries"/>), the player is given the reward item.
/// Create via Assets > Create > Quests > Item Set Quest.
/// </summary>
[CreateAssetMenu(fileName = "NewItemSetQuest", menuName = "Quests/Item Set Quest")]
public class ItemSetQuestData : ScriptableObject
{
    [Tooltip("The set of distinct item types that must each be delivered to the hub at least once.")]
    [SerializeField] private ItemData[] _requiredItems;
    [SerializeField] private ItemData _rewardItem;

    public ItemData[] RequiredItems => _requiredItems;
    public ItemData RewardItem => _rewardItem;
}
