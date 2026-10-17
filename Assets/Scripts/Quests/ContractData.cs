using UnityEngine;

/// <summary>
/// Contract description: which item to bring, how many and how much money it pays.
/// Create via Assets > Create > Quests > Contract Data.
/// </summary>
[CreateAssetMenu(fileName = "NewContract", menuName = "Quests/Contract Data")]
public class ContractData : ScriptableObject
{
    [SerializeField] private ItemData _requiredItem;
    [SerializeField, Min(1)] private int _requiredAmount = 15;
    [Tooltip("Money paid to the player when the contract is completed.")]
    [SerializeField, Min(0)] private int _reward = 100;
    [Tooltip("Experience given to the player when the contract is completed.")]
    [SerializeField, Min(0)] private int _experience = 50;

    public ItemData RequiredItem => _requiredItem;
    public int RequiredAmount => _requiredAmount;
    public int Reward => _reward;
    public int Experience => _experience;
}
