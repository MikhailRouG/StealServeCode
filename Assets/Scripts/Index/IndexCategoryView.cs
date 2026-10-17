using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One <see cref="IndexCategoryData"/> row of the <see cref="IndexWindow"/> scroll list:
/// picture with the title, the item grid and the progress bar.
/// </summary>
public class IndexCategoryView : MonoBehaviour
{
    [SerializeField] private Image _picture;
    [SerializeField] private TMP_Text _title;
    [SerializeField] private Transform _slotsContainer;
    [SerializeField] private RectTransform _progressFill;
    [SerializeField] private TMP_Text _progressText;

    private readonly List<IndexSlotView> _slots = new List<IndexSlotView>();

    public IndexCategoryData Category { get; private set; }

    /// <summary>Raised with this row and the item of the clicked slot.</summary>
    public event Action<IndexCategoryView, ItemData> ItemClicked;

    public void Init(IndexCategoryData category, IndexSlotView slotPrefab)
    {
        Category = category;
        _title.text = category.Title;

        // Without a picture the default (rounded) sprite is kept and tinted with the category color.
        if (category.Picture != null)
            _picture.sprite = category.Picture;
        _picture.color = category.Picture != null ? Color.white : category.Color;

        foreach (ItemData item in category.Items)
        {
            if (item == null)
                continue;

            IndexSlotView slot = Instantiate(slotPrefab, _slotsContainer);
            slot.Clicked += OnSlotClicked;
            _slots.Add(slot);
        }
    }

    public void Refresh(PlayerDiscoveries discoveries, ItemData selected, Func<int, Color> levelColor, Color silhouette)
    {
        int index = 0;
        foreach (ItemData item in Category.Items)
        {
            if (item == null)
                continue;

            bool discovered = discoveries.HasDelivered(item);
            _slots[index].Show(item, discovered, levelColor(item.Level), silhouette);
            _slots[index].SetSelected(item == selected);
            index++;
        }

        int count = Category.CountDiscovered(discoveries);
        IndexWindow.SetBar(_progressFill, _progressText, count, _slots.Count, $"{count}/{_slots.Count}");
    }

    private void OnSlotClicked(IndexSlotView slot)
    {
        ItemClicked?.Invoke(this, slot.Item);
    }
}
