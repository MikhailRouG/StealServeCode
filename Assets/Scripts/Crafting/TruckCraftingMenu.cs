using Unity.Cinemachine;
using UnityEngine;
using Zenject;

/// <summary>
/// Put on the <see cref="HubZone"/>: when the player clicks on the truck (see <see cref="PlayerClicker"/>),
/// opens the <see cref="CraftingWindow"/> with the truck's <see cref="TruckKitchen"/> and switches to the menu Cinemachine camera.
/// Closing the window switches back.
/// The menu camera is turned off while the window is closed, so its priority must be higher than the gameplay camera's.
/// </summary>
[RequireComponent(typeof(TruckKitchen))]
public class TruckCraftingMenu : MonoBehaviour, IClickable
{
    [Tooltip("Camera that shows the truck while the menu is open.")]
    [SerializeField] private CinemachineCamera _menuCamera;
    [Tooltip("The player must be closer to the truck than this to open the menu. 0 = any distance.")]
    [SerializeField, Min(0f)] private float _maxPlayerDistance = 8f;
    [Tooltip("Collider to click to open the menu. If empty, any collider of the truck opens it.")]
    [SerializeField] private Collider _clickArea;

    private TruckKitchen _kitchen;
    private CraftingWindow _window;

    [Inject]
    private void Construct(CraftingWindow window)
    {
        _window = window;
    }

    private void Awake()
    {
        _kitchen = GetComponent<TruckKitchen>();
        SetMenuCamera(false);
    }

    private void OnEnable()
    {
        _window.Opened += OnWindowOpened;
        _window.Closed += OnWindowClosed;
    }

    private void OnDisable()
    {
        _window.Opened -= OnWindowOpened;
        _window.Closed -= OnWindowClosed;
    }

    public void OnClicked(Player player)
    {
        if (_window.IsOpen || !IsCloseEnough(player))
            return;

        _window.Open(_kitchen, player);
    }

    public bool IsClickArea(Collider hitCollider) => _clickArea == null || hitCollider == _clickArea;

    private bool IsCloseEnough(Player player)
    {
        return _maxPlayerDistance <= 0f
            || Vector3.Distance(player.transform.position, transform.position) <= _maxPlayerDistance;
    }

    private void OnWindowOpened()
    {
        SetMenuCamera(true);
    }

    private void OnWindowClosed()
    {
        SetMenuCamera(false);
    }

    private void SetMenuCamera(bool active)
    {
        if (_menuCamera != null)
            _menuCamera.gameObject.SetActive(active);
    }
}
