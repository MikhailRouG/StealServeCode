using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

/// <summary>
/// Shop: tab buttons on top, each tab shows its own scroll page. For now there is only the Stats tab,
/// where the player buys speed stat levels for money. Only shows the <see cref="SpeedUpgrades"/> model
/// and sends purchases to it; the bought levels live in the model.
/// Put on an always-active object (the canvas); the window itself is <see cref="_panel"/>.
/// Opened by the HUD shop button. Gets the player through injection (see <see cref="GameInstaller"/>).
/// </summary>
public class ShopWindow : MonoBehaviour
{
    [Serializable]
    private class Tab
    {
        public Button button;
        public GameObject page;
    }

    [SerializeField] private GameObject _panel;
    [SerializeField] private Button _closeButton;
    [SerializeField] private WindowTween _animation = new WindowTween(WindowTween.Style.SlideFromLeft);

    [Header("Tabs")]
    [SerializeField] private Tab[] _tabs = Array.Empty<Tab>();
    [SerializeField] private Color _activeTabColor = new Color32(0x4F, 0xC3, 0xF7, 0xFF);
    [SerializeField] private Color _inactiveTabColor = new Color32(0x4A, 0x4D, 0x55, 0xFF);

    [Header("Stats")]
    [SerializeField] private ShopStatView _speedView;

    private Player _player;
    private Wallet _wallet;
    private PlayerStats _stats;
    private SpeedUpgrades _speedUpgrades;
    private int _activeTab;

    public bool IsOpen => _animation.IsShown;

    private void Awake()
    {
        _closeButton.onClick.AddListener(Close);
        for (int i = 0; i < _tabs.Length; i++)
        {
            int index = i;
            _tabs[i].button.onClick.AddListener(() => ShowTab(index));
        }

        _speedView.Clicked += BuySpeed;
        _animation.Init(_panel);
        _animation.Hide();
    }

    private void OnDestroy()
    {
        _closeButton.onClick.RemoveListener(Close);
        foreach (Tab tab in _tabs)
            tab.button.onClick.RemoveAllListeners();

        _speedView.Clicked -= BuySpeed;
        Unsubscribe();
    }

    [Inject]
    private void Construct(Player player, Wallet wallet, PlayerStats stats, SpeedUpgrades speedUpgrades)
    {
        // The player is only needed to stop it while the window is open.
        _player = player;
        _wallet = wallet;
        _stats = stats;
        _speedUpgrades = speedUpgrades;

        _wallet.Changed += OnMoneyChanged;
        _stats.SpeedStatChanged += OnSpeedStatChanged;
        _speedUpgrades.Changed += Refresh;
    }

    /// <summary>Shows the window. Does nothing while another menu has the player stopped.</summary>
    public void Open()
    {
        if (IsOpen || _player.Move.IsFrozen)
            return;

        // Freezing also unlocks the cursor.
        _player.Move.Freeze(true);
        _animation.Show();
        ShowTab(_activeTab);
        Refresh();
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        _animation.Hide();
        _player.Move.Freeze(false);
    }

    public void ShowTab(int index)
    {
        _activeTab = Mathf.Clamp(index, 0, Mathf.Max(0, _tabs.Length - 1));

        for (int i = 0; i < _tabs.Length; i++)
        {
            bool active = i == _activeTab;
            _tabs[i].page.SetActive(active);
            _tabs[i].button.image.color = active ? _activeTabColor : _inactiveTabColor;

            // Each page starts scrolled to the top.
            ScrollRect scroll = active ? _tabs[i].page.GetComponentInChildren<ScrollRect>() : null;
            if (scroll != null)
                scroll.verticalNormalizedPosition = 1f;
        }
    }

    private void Unsubscribe()
    {
        if (_player == null)
            return;

        _wallet.Changed -= OnMoneyChanged;
        _stats.SpeedStatChanged -= OnSpeedStatChanged;
        _speedUpgrades.Changed -= Refresh;
    }

    private void OnMoneyChanged(int money) => Refresh();

    private void OnSpeedStatChanged(float stat) => Refresh();

    private void Refresh()
    {
        if (_player == null || !IsOpen)
            return;

        float speed = _stats.CurrentSpeedStat;
        int level = _speedUpgrades.BoughtLevels;
        if (_speedUpgrades.IsMaxed)
        {
            _speedView.ShowMaxed(speed, level);
            return;
        }

        StatUpgradeData.UpgradeLevel next = _speedUpgrades.Next;
        _speedView.Show(speed, level, next.amount, next.price, _speedUpgrades.CanBuy);
    }

    private void BuySpeed()
    {
        _speedUpgrades.TryBuy();
    }
}
