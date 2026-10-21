using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hub zone (the food truck) with item storage. When the player enters its trigger,
/// everything in the player's inventory is moved into the storage.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HubZone : MonoBehaviour
{
    private readonly CarStorage _storage = new CarStorage();

    public CarStorage Storage => _storage;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Player player) && player.Inventory.Count > 0)
            Unload(player);
    }

    /// <summary>Moves all items from the player's inventory into the storage and grants their experience.</summary>
    public void Unload(Player player)
    {
        Inventory inventory = player.Inventory;
        foreach (ItemData item in new List<ItemData>(inventory.Items))
        {
            if (inventory.Remove(item))
            {
                Storage.Add(item);
                player.Level.AddExperience(item.Experience);
                player.Discoveries.MarkDelivered(item);
            }
        }
    }
}
