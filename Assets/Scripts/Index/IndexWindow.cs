using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zenject;

/// <summary>
/// Item index: a scroll list of <see cref="IndexCategoryData"/> rows (picture, item grid, progress),
/// details of the selected item on the right and the reward of its category, which can be claimed
/// once all the category's items are discovered. Only shows the <see cref="IndexRewards"/> model
/// and sends claims to it; the claimed rewards live in the model.
/// Put on an always-active object (the canvas); the window itself is <see cref="_panel"/>.
/// Opened and closed with <see cref="_toggleKey"/>. Gets the player through injection (see <see cref="GameInstaller"/>).
/// </summary>
public class IndexWindow : MonoBehaviour
{
    [SerializeField] private List<IndexCategoryData> _categories = new List<IndexCategoryData>();
    [SerializeField] private Key _toggleKey = Key.I;
    [SerializeField] private GameObject _panel;
    [SerializeField] private Button _closeButton;
    [SerializeField] private WindowTween _animation = new WindowTween(WindowTween.Style.SlideFromLeft);

    [Header("Categories")]
    [SerializeField] private IndexCategoryView _categoryPrefab;
    [SerializeField] private IndexSlotView _slotPrefab;
    [SerializeField] private ScrollRect _scroll;
    [Tooltip("Content of the scroll view; category rows are created in it.")]
    [SerializeField] private Transform _categoriesContainer;
    [Tooltip("Slot color by item level: element 0 = level 1. Higher levels reuse the last color.")]
    [SerializeField] private Color[] _levelColors =
    {
        new Color32(0x8A, 0x8A, 0x8A, 0xFF),
        new Color32(0x3C, 0xC8, 0x3C, 0xFF),
        new Color32(0x2E, 0x6B, 0xE6, 0xFF),
        new Color32(0xC8, 0x2E, 0xD9, 0xFF),
        new Color32(0xF2, 0xB8, 0x1C, 0xFF),
    };
    [SerializeField] private Color _silhouetteColor = new Color(0f, 0f, 0f, 0.55f);

    [Header("Total progress")]
    [SerializeField] private RectTransform _unlockedFill;
    [SerializeField] private TMP_Text _unlockedText;

    [Header("Selected item")]
    [SerializeField] private Image _detailsIcon;
    [SerializeField] private TMP_Text _detailsName;
    [SerializeField] private TMP_Text _detailsLevel;
    [SerializeField] private TMP_Text _detailsPrice;

    [Header("Reward of the selected item's category")]
    [SerializeField] private TMP_Text _rewardMoneyText;
    [SerializeField] private TMP_Text _rewardSpeedText;
    [SerializeField] private Button _claimButton;
    [SerializeField] private TMP_Text _claimText;

    private readonly List<IndexCategoryView> _views = new List<IndexCategoryView>();
    private Player _player;
    private PlayerDiscoveries _discoveries;
    private IndexRewards _rewards;
    private IndexCategoryData _selectedCategory;
    private ItemData _selectedItem;

    public bool IsOpen => _animation.IsShown;

    private void Awake()
    {
        _closeButton.onClick.AddListener(Close);
        _claimButton.onClick.AddListener(Claim);
        CreateViews();
        _animation.Init(_panel);
        _animation.Hide();
    }

    private void OnDestroy()
    {
        _closeButton.onClick.RemoveListener(Close);
        _claimButton.onClick.RemoveListener(Claim);

        if (_discoveries != null)
            _discoveries.Discovered -= OnDiscovered;
        if (_rewards != null)
            _rewards.Claimed -= OnClaimed;
    }

    [Inject]
    private void Construct(Player player, PlayerDiscoveries discoveries, IndexRewards rewards)
    {
        // The player is only needed to stop it while the window is open.
        _player = player;
        _discoveries = discoveries;
        _rewards = rewards;

        _discoveries.Discovered += OnDiscovered;
        _rewards.Claimed += OnClaimed;
    }

    /// <summary>Shows the window. Does nothing while another menu has the player stopped.</summary>
    public void Open()
    {
        if (IsOpen || _player.Move.IsFrozen)
            return;

        // Freezing also unlocks the cursor.
        _player.Move.Freeze(true);
        _animation.Show();
        Refresh();
        _scroll.verticalNormalizedPosition = 1f;
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        _animation.Hide();
        _player.Move.Freeze(false);
    }

    /// <summary>Sets the fill width of a progress bar and its label.</summary>
    public static void SetBar(RectTransform fill, TMP_Text text, int value, int max, string label)
    {
        fill.anchorMax = new Vector2(max > 0 ? (float)value / max : 0f, fill.anchorMax.y);
        text.text = label;
    }

    private void CreateViews()
    {
        foreach (IndexCategoryData category in _categories)
        {
            if (category == null)
                continue;

            IndexCategoryView view = Instantiate(_categoryPrefab, _categoriesContainer);
            view.Init(category, _slotPrefab);
            view.ItemClicked += Select;
            _views.Add(view);
        }

        if (_views.Count > 0)
        {
            _selectedCategory = _views[0].Category;
            _selectedItem = _selectedCategory.Items.Length > 0 ? _selectedCategory.Items[0] : null;
        }
    }

    private void OnDiscovered(ItemData item)
    {
        if (IsOpen)
            Refresh();
    }

    private void OnClaimed(IndexCategoryData category)
    {
        if (IsOpen)
            Refresh();
    }

    private void Select(IndexCategoryView view, ItemData item)
    {
        _selectedCategory = view.Category;
        _selectedItem = item;
        Refresh();
    }

    private void Refresh()
    {
        foreach (IndexCategoryView view in _views)
            view.Refresh(_discoveries, _selectedItem, GetLevelColor, _silhouetteColor);

        ShowUnlocked();
        ShowDetails();
        ShowReward();
    }

    private void ShowUnlocked()
    {
        var allItems = new HashSet<ItemData>();
        foreach (IndexCategoryView view in _views)
        {
            foreach (ItemData item in view.Category.Items)
            {
                if (item != null)
                    allItems.Add(item);
            }
        }

        int unlocked = 0;
        foreach (ItemData item in allItems)
        {
            if (IsDiscovered(item))
                unlocked++;
        }

        SetBar(_unlockedFill, _unlockedText, unlocked, allItems.Count, $"Unlocked: {unlocked}/{allItems.Count}");
    }

    private void ShowDetails()
    {
        ItemData item = _selectedItem;
        bool discovered = item != null && IsDiscovered(item);

        _detailsIcon.sprite = item != null ? item.Icon : null;
        _detailsIcon.enabled = _detailsIcon.sprite != null;
        _detailsIcon.color = discovered ? Color.white : _silhouetteColor;

        _detailsName.text = discovered ? item.ItemName : IndexSlotView.UnknownName;
        _detailsLevel.text = discovered ? $"Lv. {item.Level}" : string.Empty;
        _detailsPrice.text = discovered && item is DishData dish ? $"${dish.Price}" : string.Empty;
    }

    private void ShowReward()
    {
        IndexCategoryData category = _selectedCategory;
        if (category == null)
        {
            _rewardMoneyText.text = string.Empty;
            _rewardSpeedText.text = string.Empty;
            _claimButton.interactable = false;
            _claimText.text = "LOCKED";
            return;
        }

        _rewardMoneyText.text = $"${category.RewardMoney}";
        _rewardSpeedText.text = $"+{category.RewardSpeedStat}";

        bool claimed = _rewards.IsClaimed(category);
        bool complete = category.IsComplete(_discoveries);
        _claimButton.interactable = _rewards.CanClaim(category);
        _claimText.text = claimed ? "CLAIMED!" : complete ? "CLAIM" : "LOCKED";
    }

    private void Claim()
    {
        _rewards.TryClaim(_selectedCategory);
    }

    private bool IsDiscovered(ItemData item) => _discoveries.HasDelivered(item);

    private Color GetLevelColor(int level)
    {
        if (_levelColors == null || _levelColors.Length == 0)
            return Color.gray;

        return _levelColors[Mathf.Clamp(level - 1, 0, _levelColors.Length - 1)];
    }
}
