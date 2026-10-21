using System;
using System.Collections.Generic;

public class StoredItem
{
    public ItemData Item;
    public int Count;
}

/// <summary>
/// Plain C# item storage (not a MonoBehaviour), owned by <see cref="HubZone"/>.
/// Starts empty and keeps any number of items of each type.
/// </summary>
public class CarStorage
{
    private readonly List<StoredItem> _items = new List<StoredItem>();

    /// <summary>Raised after items are added or removed.</summary>
    public event Action Changed;

    /// <summary>Stored items, one entry per item type. Entries with 0 count are removed.</summary>
    public IReadOnlyList<StoredItem> Items => _items;

    /// <summary>Number of items of all types.</summary>
    public int TotalCount
    {
        get
        {
            int total = 0;
            foreach (StoredItem stored in _items)
                total += stored.Count;
            return total;
        }
    }

    public void Add(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return;

        StoredItem stored = Find(item);
        if (stored == null)
        {
            stored = new StoredItem { Item = item };
            _items.Add(stored);
        }

        stored.Count += amount;
        Changed?.Invoke();
    }

    /// <summary>Removes up to <paramref name="amount"/> items of the type. Returns how many were removed.</summary>
    public int Remove(ItemData item, int amount = 1)
    {
        StoredItem stored = Find(item);
        if (stored == null || stored.Count == 0 || amount <= 0)
            return 0;

        int removed = Math.Min(amount, stored.Count);
        stored.Count -= removed;
        if (stored.Count == 0)
            _items.Remove(stored);

        Changed?.Invoke();
        return removed;
    }

    /// <summary>How many items of the type are stored. 0 = not found.</summary>
    public int GetCount(ItemData item)
    {
        StoredItem stored = Find(item);
        return stored != null ? stored.Count : 0;
    }

    private StoredItem Find(ItemData item)
    {
        if (item == null)
            return null;

        return _items.Find(stored => stored.Item == item);
    }
}
