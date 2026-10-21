using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put on the <see cref="HubZone"/>: dishes (<see cref="DishData"/>) put up for sale, one per point.
/// Items are placed from the Storage tab of the <see cref="CraftingWindow"/> (see <see cref="TruckCraftingMenu"/>).
/// The shown items are plain models (no collider); clients buy them via <see cref="TrySell"/> (see <see cref="ClientSales"/>)
/// and take them at the client point via <see cref="Take"/>.
/// </summary>
[RequireComponent(typeof(HubZone))]
public class TruckShowcase : MonoBehaviour
{
    [Tooltip("Places for the items for sale. Each one holds a single item.")]
    [SerializeField] private List<Transform> _points = new List<Transform>();
    [Tooltip("Where a client comes to take the bought item. If empty, sold items are removed at once.")]
    [SerializeField] private Transform _clientPoint;

    private DishData[] _items;
    private GameObject[] _models;
    // Sold, but the client hasn't taken it yet: still shown, can't be sold again.
    private bool[] _reserved;

    /// <summary>Number of items on the points (including the sold ones not taken yet).</summary>
    public int Count { get; private set; }
    public bool HasFreePoint => FindPoint(null) >= 0;
    public Transform ClientPoint => _clientPoint;

    /// <summary>Raised after an item is put up for sale or taken.</summary>
    public event Action Changed;
    /// <summary>Raised with the item after it is put up for sale (after <see cref="Changed"/>).</summary>
    public event Action<DishData> Placed;

    private void Awake()
    {
        _items = new DishData[_points.Count];
        _models = new GameObject[_points.Count];
        _reserved = new bool[_points.Count];
    }

    /// <summary>Puts the item on the first free point. Returns false if all points are taken.</summary>
    public bool TryPlace(DishData item)
    {
        int index = FindPoint(null);
        if (item == null || index < 0)
            return false;

        _items[index] = item;
        _models[index] = CreateModel(item, _points[index]);
        Count++;
        Changed?.Invoke();
        Placed?.Invoke(item);
        return true;
    }

    /// <summary>
    /// Sells the first shown item whose interest is at least <paramref name="interest"/> (the client's one).
    /// The item stays on its point (reserved) until <see cref="Take"/> is called with <paramref name="slot"/>.
    /// Returns false if no item fits.
    /// </summary>
    public bool TrySell(int interest, out DishData item, out int slot)
    {
        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i] != null && !_reserved[i] && _items[i].Interest >= interest)
            {
                item = _items[i];
                slot = i;
                _reserved[i] = true;
                return true;
            }
        }

        item = null;
        slot = -1;
        return false;
    }

    /// <summary>Removes the sold item from <paramref name="slot"/> (the client took it).</summary>
    public void Take(int slot)
    {
        if (slot < 0 || slot >= _items.Length || !_reserved[slot])
            return;

        RemoveAt(slot);
    }

    /// <summary>The client left without taking the item from <paramref name="slot"/>: it is up for sale again.</summary>
    public void Release(int slot)
    {
        if (slot < 0 || slot >= _items.Length || !_reserved[slot])
            return;

        _reserved[slot] = false;
        DishData item = _items[slot];
        Changed?.Invoke();
        Placed?.Invoke(item);
    }

    private void RemoveAt(int index)
    {
        if (_models[index] != null)
            Destroy(_models[index]);

        _items[index] = null;
        _models[index] = null;
        _reserved[index] = false;
        Count--;
        Changed?.Invoke();
    }

    // Index of the point that holds the item; null = first free point. -1 if not found.
    private int FindPoint(DishData item)
    {
        for (int i = 0; i < _points.Count; i++)
        {
            if (_points[i] != null && _items[i] == item)
                return i;
        }

        return -1;
    }

    private static GameObject CreateModel(ItemData item, Transform point)
    {
        if (item.Model == null)
            return null;

        GameObject model = Instantiate(item.Model, point, false);
        model.name = item.Model.name;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        return model;
    }

}
