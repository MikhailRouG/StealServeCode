using System;
using System.Collections.Generic;

/// <summary>
/// Plain C# inventory (not a MonoBehaviour), created by the container (see <see cref="GameInstaller"/>).
/// Starts empty and holds at most one item of each type.
/// </summary>
public class Inventory
{
    private readonly int _maxItems;
    private readonly List<ItemData> _items = new List<ItemData>();

    /// <summary>Raised after an item is added or removed.</summary>
    public event Action Changed;

    public int Count => _items.Count;

    public IReadOnlyList<ItemData> Items => _items;

    /// <summary>The most recently added item, or null if the inventory is empty.</summary>
    public ItemData LastItem => _items.Count > 0 ? _items[_items.Count - 1] : null;

    /// <param name="maxItems">Max number of items in total. 0 = unlimited.</param>
    public Inventory(int maxItems)
    {
        _maxItems = Math.Max(0, maxItems);
    }

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
