using UnityEngine;

/// <summary>
/// Discovery contract: experience given the first time the item is ever delivered to the hub
/// (see <see cref="PlayerDiscoveries"/>).
/// Create via Assets > Create > Quests > Discovery Contract.
/// </summary>
[CreateAssetMenu(fileName = "NewDiscoveryContract", menuName = "Quests/Discovery Contract")]
public class DiscoveryContractData : ScriptableObject
{
    [SerializeField] private ItemData _item;
    [Tooltip("Experience given when the item is delivered to the hub for the first time.")]
    [SerializeField, Min(0)] private int _experience = 20;

    public ItemData Item => _item;
    public int Experience => _experience;
}
