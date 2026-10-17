using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Crafting window: recipe grid on the left, selected recipe details and the cook button on the right.
/// Cooking is done by the truck's <see cref="TruckKitchen"/>: it takes the ingredients from the storage and,
/// after the dish's cook time, puts the dish straight on the showcase. The cook button shows the time left.
/// The Storage tab lists everything that is in the storage (only to look at: items can't be put up for sale).
/// On both tabs the right half shows how the selected item is cooked (top) and its sale price and interest (bottom).
/// Opened by <see cref="TruckCraftingMenu"/> / <see cref="CraftingStation"/>. Build the UI via Tools > Crafting > Create Crafting UI.
/// </summary>
public class CraftingWindow : MonoBehaviour
{
    [SerializeField] private List<RecipeData> _recipes = new List<RecipeData>();

    [Header("Grid")]
    [SerializeField] private RecipeSlotView _slotPrefab;
    [SerializeField] private Transform _slotsContainer;

    [Header("Details")]
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private Image _resultIcon;
    [SerializeField] private IngredientView _ingredientPrefab;
    [SerializeField] private Transform _ingredientsContainer;
    [SerializeField] private Button _craftButton;
    [SerializeField] private Button _closeButton;

    [Header("Tabs (optional)")]
    [SerializeField] private Button _craftingTab;
    [SerializeField] private Button _storageTab;
    [SerializeField] private GameObject _craftingPage;
    [SerializeField] private GameObject _storagePage;
    [SerializeField] private Color _activeTabColor = new Color32(0xE8, 0xA2, 0x4C, 0xFF);
    [SerializeField] private Color _inactiveTabColor = new Color32(0xF2, 0xDB, 0xC2, 0xFF);
    [SerializeField] private Color _activeTabTextColor = Color.white;
    [SerializeField] private Color _inactiveTabTextColor = new Color32(0x6B, 0x42, 0x26, 0xFF);

    [Header("Storage (optional)")]
    [SerializeField] private StorageItemView _storageItemPrefab;
    [SerializeField] private Transform _storageContainer;
    [Tooltip("Shown while the storage is empty.")]
    [SerializeField] private GameObject _storageEmptyLabel;

    [Header("Item info (optional)")]
    [Tooltip("Sale price and interest of the selected recipe's result (Cooking tab).")]
    [SerializeField] private ItemSaleInfoView _craftingSaleInfo;
    [Tooltip("How the selected storage item is cooked (Storage tab).")]
    [SerializeField] private RecipeInfoView _storageRecipeInfo;
    [Tooltip("Sale price and interest of the selected storage item (Storage tab).")]
    [SerializeField] private ItemSaleInfoView _storageSaleInfo;

    private readonly List<RecipeSlotView> _slots = new List<RecipeSlotView>();
    private readonly List<IngredientView> _ingredients = new List<IngredientView>();
    private readonly List<StorageItemView> _storageItems = new List<StorageItemView>();
    private RecipeSlotView _selected;
    private CarStorage _storage;
    private TruckKitchen _kitchen;
    private ItemData _selectedStorageItem;
    private Player _player;
    private TMP_Text _craftLabel;
    private string _craftText;

    public bool IsOpen => gameObject.activeSelf;

    /// <summary>Raised after the window is shown.</summary>
    public event System.Action Opened;
    /// <summary>Raised after the window is hidden (close button or <see cref="Close"/>).</summary>
    public event System.Action Closed;

    private void Awake()
    {
        _craftButton.onClick.AddListener(Craft);
        _closeButton.onClick.AddListener(Close);
        if (_craftingTab != null)
            _craftingTab.onClick.AddListener(ShowCraftingTab);
        if (_storageTab != null)
            _storageTab.onClick.AddListener(ShowStorageTab);

        _craftLabel = _craftButton.GetComponentInChildren<TMP_Text>();
        if (_craftLabel != null)
            _craftText = _craftLabel.text;

        CreateSlots();
    }

    // The time left changes every frame while a dish is cooking.
    private void Update()
    {
        if (_kitchen != null && _kitchen.IsCooking)
            ShowCraftButton(_selected != null ? _selected.Recipe : null);
    }

    private void OnDestroy()
    {
        _craftButton.onClick.RemoveListener(Craft);
        _closeButton.onClick.RemoveListener(Close);
        if (_craftingTab != null)
            _craftingTab.onClick.RemoveListener(ShowCraftingTab);
        if (_storageTab != null)
            _storageTab.onClick.RemoveListener(ShowStorageTab);
        Unsubscribe();
    }

    public void ShowCraftingTab()
    {
        SetTab(storage: false);
    }

    public void ShowStorageTab()
    {
        SetTab(storage: true);
    }

    private void SetTab(bool storage)
    {
        if (_craftingPage != null)
            _craftingPage.SetActive(!storage);
        if (_storagePage != null)
            _storagePage.SetActive(storage);

        PaintTab(_craftingTab, !storage);
        PaintTab(_storageTab, storage);
    }

    private void PaintTab(Button tab, bool active)
    {
        if (tab == null)
            return;

        tab.image.color = active ? _activeTabColor : _inactiveTabColor;
        TMP_Text label = tab.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.color = active ? _activeTabTextColor : _inactiveTabTextColor;
    }

    /// <summary>Shows the window for the kitchen's storage and recipes. The player can't move while it is open.</summary>
    public void Open(TruckKitchen kitchen, Player player)
    {
        Unsubscribe();
        _kitchen = kitchen;
        _storage = kitchen != null ? kitchen.Storage : null;
        _player = player;
        if (_storage != null)
            _storage.Changed += Refresh;
        if (_kitchen != null)
            _kitchen.Changed += Refresh;

        // Freezing also unlocks the cursor.
        if (_player != null)
            _player.Move.Freeze(true);

        gameObject.SetActive(true);
        ShowCraftingTab();
        Select(_selected != null ? _selected : (_slots.Count > 0 ? _slots[0] : null));
        Opened?.Invoke();
    }

    public void Close()
    {
        Unsubscribe();
        _storage = null;
        _kitchen = null;

        if (_player != null)
            _player.Move.Freeze(false);

        _player = null;
        gameObject.SetActive(false);
        Closed?.Invoke();
    }

    private void Unsubscribe()
    {
        if (_storage != null)
            _storage.Changed -= Refresh;
        if (_kitchen != null)
            _kitchen.Changed -= Refresh;
    }

    private void CreateSlots()
    {
        foreach (RecipeData recipe in _recipes)
        {
            if (recipe == null)
                continue;

            RecipeSlotView slot = Instantiate(_slotPrefab, _slotsContainer);
            slot.Init(recipe);
            slot.Clicked += Select;
            _slots.Add(slot);
        }
    }

    private void Select(RecipeSlotView slot)
    {
        if (_selected != null)
            _selected.SetSelected(false);

        _selected = slot;

        if (_selected != null)
            _selected.SetSelected(true);

        Refresh();
    }

    private void Refresh()
    {
        RecipeData recipe = _selected != null ? _selected.Recipe : null;

        _nameText.text = recipe != null ? recipe.DisplayName : string.Empty;
        _resultIcon.sprite = recipe != null && recipe.Result != null ? recipe.Result.Icon : null;
        _resultIcon.enabled = _resultIcon.sprite != null;

        ShowIngredients(recipe);
        if (_craftingSaleInfo != null)
            _craftingSaleInfo.Show(recipe != null ? recipe.Result : null);

        ShowStorage();
        ShowCraftButton(recipe);
    }

    private void ShowCraftButton(RecipeData recipe)
    {
        _craftButton.interactable = _kitchen != null && _kitchen.CanCook(recipe);

        if (_craftLabel == null)
            return;

        if (_kitchen == null || !_kitchen.IsCooking)
            _craftLabel.text = recipe != null && recipe.Result != null ? $"Cook for {recipe.Result.CookTime:0.#}s" : _craftText;
        else if (_kitchen.IsWaitingForShowcase)
            _craftLabel.text = "Showcase full";
        else
            _craftLabel.text = $"Cooking {Mathf.CeilToInt(_kitchen.TimeLeft)}s";
    }

    private void ShowStorage()
    {
        if (_storageItemPrefab == null || _storageContainer == null)
            return;

        int count = _storage != null ? _storage.Items.Count : 0;

        while (_storageItems.Count < count)
        {
            StorageItemView view = Instantiate(_storageItemPrefab, _storageContainer);
            view.Clicked += SelectStorageItem;
            _storageItems.Add(view);
        }

        // The selected item is gone from the storage (sold out or spent on cooking).
        if (_selectedStorageItem != null && (_storage == null || _storage.GetCount(_selectedStorageItem) == 0))
            _selectedStorageItem = null;

        for (int i = 0; i < _storageItems.Count; i++)
        {
            bool visible = i < count;
            _storageItems[i].gameObject.SetActive(visible);
            if (!visible)
                continue;

            _storageItems[i].Init(_storage.Items[i]);
            _storageItems[i].SetSelected(_storageItems[i].Item == _selectedStorageItem);
        }

        if (_storageEmptyLabel != null)
            _storageEmptyLabel.SetActive(count == 0);

        if (_storageRecipeInfo != null)
            _storageRecipeInfo.Show(_selectedStorageItem, FindRecipe(_selectedStorageItem), _storage);
        if (_storageSaleInfo != null)
            _storageSaleInfo.Show(_selectedStorageItem);
    }

    private RecipeData FindRecipe(ItemData result)
    {
        if (result == null)
            return null;

        foreach (RecipeData recipe in _recipes)
        {
            if (recipe != null && recipe.Result == result)
                return recipe;
        }

        return null;
    }

    private void SelectStorageItem(StorageItemView view)
    {
        _selectedStorageItem = view.Item;
        Refresh();
    }

    private void ShowIngredients(RecipeData recipe)
    {
        int count = recipe != null ? recipe.Ingredients.Count : 0;

        while (_ingredients.Count < count)
            _ingredients.Add(Instantiate(_ingredientPrefab, _ingredientsContainer));

        for (int i = 0; i < _ingredients.Count; i++)
        {
            bool visible = i < count;
            _ingredients[i].gameObject.SetActive(visible);
            if (!visible)
                continue;

            Ingredient ingredient = recipe.Ingredients[i];
            int have = _storage != null && ingredient.Item != null ? _storage.GetCount(ingredient.Item) : 0;
            _ingredients[i].Init(ingredient, have);
        }
    }

    private void Craft()
    {
        if (_selected != null && _kitchen != null)
            _kitchen.TryCook(_selected.Recipe);
    }
}
