using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One cell of the storage tab in <see cref="CraftingWindow"/>: item icon, name and count.
/// Clicking it selects the item (e.g. to put it up for sale).
/// </summary>
[RequireComponent(typeof(Button))]
public class StorageItemView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _countText;
    [Tooltip("Optional frame shown only while the item is selected.")]
    [SerializeField] private GameObject _selectedFrame;

    private Button _button;

    public ItemData Item { get; private set; }

    /// <summary>Raised with this view when it is clicked.</summary>
    public event Action<StorageItemView> Clicked;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        _button.onClick.RemoveListener(OnClick);
    }

    public void Init(StoredItem stored)
    {
        Item = stored.Item;

        _icon.sprite = Item != null ? Item.Icon : null;
        _icon.enabled = _icon.sprite != null;

        if (_nameText != null)
            _nameText.text = Item != null ? Item.ItemName : string.Empty;

        _countText.text = $"x{stored.Count}";
    }

    public void SetSelected(bool selected)
    {
        if (_selectedFrame != null)
            _selectedFrame.SetActive(selected);
    }

    private void OnClick()
    {
        Clicked?.Invoke(this);
    }
}
