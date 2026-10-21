using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

/// <summary>
/// Crafting place: opens the <see cref="CraftingWindow"/> when the player enters the trigger
/// and closes it when the player leaves. Cooks with the truck's <see cref="TruckKitchen"/>.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CraftingStation : MonoBehaviour
{
    [Tooltip("The hub whose kitchen cooks here and whose storage the ingredients come from.")]
    [FormerlySerializedAs("_truck")]
    [SerializeField] private HubZone _hubZone;

    private CraftingWindow _window;
    private TruckKitchen _kitchen;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    [Inject]
    private void Construct(CraftingWindow window)
    {
        _window = window;
    }

    private void Awake()
    {
        if (_hubZone != null)
            _kitchen = _hubZone.GetComponent<TruckKitchen>();

        if (_kitchen == null)
            Debug.LogError($"{nameof(CraftingStation)}: hub zone with a TruckKitchen is not set.", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_kitchen != null && other.TryGetComponent(out Player player))
            _window.Open(_kitchen, player);
    }

    private void OnTriggerExit(Collider other)
    {
        if (_window.IsOpen && other.TryGetComponent(out Player _))
            _window.Close();
    }
}
