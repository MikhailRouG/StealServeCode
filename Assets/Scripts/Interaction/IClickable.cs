using UnityEngine;

/// <summary>
/// Anything the player can click on in the world. Clicks are found by <see cref="PlayerClicker"/>.
/// </summary>
public interface IClickable
{
    /// <summary>Called when the player clicks on the object's collider (or a child collider).</summary>
    void OnClicked(Player player);

    /// <summary>Whether a click on this collider counts. By default any collider of the object does.</summary>
    bool IsClickArea(Collider hitCollider) => true;
}
