using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One cell of the <see cref="IndexWindow"/> grid. A discovered item shows its name and icon,
/// an undiscovered one shows "???" and a dark silhouette of the icon.
/// </summary>
[RequireComponent(typeof(Button))]
public class IndexSlotView : MonoBehaviour
{
    public const string UnknownName = "???";

    [SerializeField] private Image _background;
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private GameObject _selectedFrame;

    private Button _button;

    public ItemData Item { get; private set; }

    /// <summary>Raised with this slot when it is clicked.</summary>
    public event Action<IndexSlotView> Clicked;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        _button.onClick.RemoveListener(OnClick);
    }

    public void Show(ItemData item, bool discovered, Color background, Color silhouette)
    {
        Item = item;

        _background.color = background;
        _nameText.text = discovered ? item.ItemName : UnknownName;
        _icon.sprite = item.Icon;
        _icon.enabled = item.Icon != null;
        _icon.color = discovered ? Color.white : silhouette;
    }

    public void SetSelected(bool selected)
    {
        _selectedFrame.SetActive(selected);
    }

    private void OnClick()
    {
        Clicked?.Invoke(this);
    }
}
