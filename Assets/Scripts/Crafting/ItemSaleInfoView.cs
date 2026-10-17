using TMPro;
using UnityEngine;

/// <summary>
/// Sale info of an item in <see cref="CraftingWindow"/>: price a client pays for it and how much clients want it.
/// Only dishes (<see cref="DishData"/>) can be sold; for other items the empty label is shown.
/// </summary>
public class ItemSaleInfoView : MonoBehaviour
{
    [SerializeField] private TMP_Text _priceText;
    [SerializeField] private TMP_Text _interestText;
    [Tooltip("Optional bar whose width follows the interest (0-100).")]
    [SerializeField] private RectTransform _interestFill;
    [Tooltip("Optional root of the values, hidden while there is no item.")]
    [SerializeField] private GameObject _content;
    [Tooltip("Optional label shown while there is no item.")]
    [SerializeField] private GameObject _emptyLabel;

    public void Show(ItemData item)
    {
        var dish = item as DishData;
        bool hasDish = dish != null;
        if (_content != null)
            _content.SetActive(hasDish);
        if (_emptyLabel != null)
            _emptyLabel.SetActive(!hasDish);

        if (!hasDish)
            return;

        _priceText.text = dish.Price.ToString();
        _interestText.text = $"{dish.Interest}%";

        if (_interestFill != null)
            _interestFill.anchorMax = new Vector2(dish.Interest / 100f, _interestFill.anchorMax.y);
    }
}
