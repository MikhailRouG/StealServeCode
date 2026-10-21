using UnityEngine;

/// <summary>
/// UI root: put on the parent object of all UI canvases (the editor UI builders put new canvases under it).
/// The UI parts get the player through injection (see <see cref="GameInstaller"/>); the root only exposes
/// the windows that are bound for other objects to open.
/// </summary>
public class GameUI : MonoBehaviour
{
    [Tooltip("If empty, the first CraftingWindow among the children (even inactive) is used.")]
    [SerializeField] private CraftingWindow _craftingWindow;

    /// <summary>The crafting window; opened by <see cref="TruckCraftingMenu"/>. Safe to read in an installer.</summary>
    public CraftingWindow CraftingWindow
    {
        get
        {
            if (_craftingWindow == null)
                _craftingWindow = GetComponentInChildren<CraftingWindow>(true);

            return _craftingWindow;
        }
    }
}
