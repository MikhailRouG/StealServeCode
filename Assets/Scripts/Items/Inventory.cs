using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plain C# inventory (not a MonoBehaviour). Holds at most one item of each type.
/// </summary>
[Serializable]
public class Inventory
{
    [Tooltip("Max number of items in total. 0 = unlimited.")]
    [SerializeField, Min(0)] private int _maxItems;
    [SerializeField] private List<ItemData> _items = new List<ItemData>();

    /// <summary>Raised after an item is added or removed.</summary>
    public event Action Changed;

    public int Count => _items.Count;

    public IReadOnlyList<ItemData> Items => _items;

    /// <summary>The most recently added item, or null if the inventory is empty.</summary>
    public ItemData LastItem => _items.Count > 0 ? _items[_items.Count - 1] : null;

    public bool CanAdd(ItemData item)
    {
        if (item == null || _items.Contains(item))
            return false;

        return _maxItems == 0 || _items.Count < _maxItems;
    }

    public bool Add(ItemData item)
    {
        if (!CanAdd(item))
            return false;

        _items.Add(item);
        Changed?.Invoke();
        return true;
    }

    public bool Remove(ItemData item)
    {
        if (item == null || !_items.Remove(item))
            return false;

        Changed?.Invoke();
        return true;
    }

    public void Clear()
    {
        if (_items.Count == 0)
            return;

        _items.Clear();
        Changed?.Invoke();
    }
}
