using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Row of the shop's Stats tab: stat name, its upgrade level and current value with the next level's bonus,
/// and the buy button with the next level's price.
/// </summary>
public class ShopStatView : MonoBehaviour
{
    [SerializeField] private TMP_Text _valueText;
    [SerializeField] private Button _buyButton;
    [SerializeField] private TMP_Text _priceText;

    /// <summary>Raised when the buy button is pressed.</summary>
    public event Action Clicked;

    private void Awake()
    {
        _buyButton.onClick.AddListener(OnClicked);
    }

    private void OnDestroy()
    {
        _buyButton.onClick.RemoveListener(OnClicked);
    }

    /// <summary>Shows the stat with the next level's bonus and price.</summary>
    public void Show(float value, int level, float nextAmount, int nextPrice, bool canBuy)
    {
        _valueText.text = $"Lv. {level}   {value:0} <color=#6CF06C>+{nextAmount:0}</color>";
        _priceText.text = $"${nextPrice}";
        _buyButton.interactable = canBuy;
    }

    /// <summary>Shows the stat once every level is bought.</summary>
    public void ShowMaxed(float value, int level)
    {
        _valueText.text = $"Lv. {level}   {value:0}";
        _priceText.text = "MAX";
        _buyButton.interactable = false;
    }

    private void OnClicked()
    {
        Clicked?.Invoke();
    }
}
