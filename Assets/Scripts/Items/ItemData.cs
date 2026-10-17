using UnityEngine;

/// <summary>
/// Item description: name, icon, model, level and collect experience.
/// Cooked food that can be sold is a <see cref="DishData"/>.
/// Create via Assets > Create > Items > Item Data.
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Items/Item Data")]
public class ItemData : ScriptableObject
{
    [SerializeField] private string _itemName = "New Item";
    [SerializeField] private Sprite _icon;
    [Tooltip("Visual-only model. In the world it is shown inside the item capsule (CollectableItem), and in the player's hand.")]
    [SerializeField] private GameObject _model;
    [SerializeField, Min(1)] private int _level = 1;
    [Tooltip("Experience the player gets for collecting this item.")]
    [SerializeField, Min(0)] private int _experience = 0;

    public string ItemName => _itemName;
    public Sprite Icon => _icon;
    public GameObject Model => _model;
    public int Level => _level;
    public int Experience => _experience;
}
