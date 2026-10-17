/// <summary>
/// Anything the player can pick up. The collector decides what to do with the item's data.
/// </summary>
public interface ICollectable
{
    /// <summary>Item to put into the inventory, or null if this collectable isn't an inventory item.</summary>
    ItemData Data { get; }

    /// <summary>False if the item has already been picked up.</summary>
    bool IsAvailable { get; }

    /// <summary>Called by the collector after the item has been taken: play effects, hide or destroy the object.</summary>
    void OnCollected();
}
