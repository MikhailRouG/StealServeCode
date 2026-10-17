using UnityEngine;

/// <summary>
/// UI root: put on the parent object of all UI canvases. <see cref="Bootstrap"/> initializes the whole UI through it,
/// so new UI only has to be wired up here. Empty references are looked up among the children (even inactive).
/// </summary>
public class GameUI : MonoBehaviour
{
    [SerializeField] private PlayerUI _playerUI;
    [SerializeField] private IndexWindow _indexWindow;
    [SerializeField] private CraftingWindow _craftingWindow;

    /// <summary>The crafting window; opened by <see cref="TruckCraftingMenu"/>. Safe to read in Awake.</summary>
    public CraftingWindow CraftingWindow
    {
        get
        {
            if (_craftingWindow == null)
                _craftingWindow = GetComponentInChildren<CraftingWindow>(true);

            return _craftingWindow;
        }
    }

    /// <summary>Gives the player to every UI part that needs it. Called once by <see cref="Bootstrap"/>.</summary>
    public void Init(Player player)
    {
        if (_playerUI == null)
            _playerUI = GetComponentInChildren<PlayerUI>(true);
        if (_indexWindow == null)
            _indexWindow = GetComponentInChildren<IndexWindow>(true);

        if (_playerUI != null)
            _playerUI.Init(player.Wallet, player.Level, player.Move);
        if (_indexWindow != null)
            _indexWindow.Init(player);
    }
}
