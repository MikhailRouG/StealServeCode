using System;
using System.Collections.Generic;

/// <summary>
/// Plain C# tracker (not a MonoBehaviour), owned by <see cref="Player"/>. Not persisted across sessions,
/// like the rest of the player's progress. Tracks which item types have ever been delivered to the hub.
/// </summary>
public class PlayerDiscoveries
{
    private readonly HashSet<ItemData> _everDelivered = new HashSet<ItemData>();

    /// <summary>Raised the first time an item type is ever delivered to the hub.</summary>
    public event Action<ItemData> Discovered;

    /// <summary>Marks the item as delivered to the hub. Returns true the first time ever.</summary>
    public bool MarkDelivered(ItemData item)
    {
        if (!_everDelivered.Add(item))
            return false;

        Discovered?.Invoke(item);
        return true;
    }

    public bool HasDelivered(ItemData item) => _everDelivered.Contains(item);
}
