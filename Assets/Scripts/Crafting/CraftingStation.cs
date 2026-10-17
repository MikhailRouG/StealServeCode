using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Crafting place: opens the <see cref="CraftingWindow"/> when the player enters the trigger
/// and closes it when the player leaves. Cooks with the truck's <see cref="TruckKitchen"/>.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CraftingStation : MonoBehaviour
{
    [Tooltip("If empty, the first CraftingWindow in the scene (even inactive) is used.")]
    [SerializeField] private CraftingWindow _window;
    [Tooltip("Where ingredients come from. If empty, the first HubZone in the scene is used.")]
    [FormerlySerializedAs("_truck")]
    [SerializeField] private HubZone _hubZone;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Start()
    {
        if (_window == null)
            _window = FindFirstObjectByType<CraftingWindow>(FindObjectsInactive.Include);

        if (_hubZone == null)
            _hubZone = FindFirstObjectByType<HubZone>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_window != null && _hubZone != null && other.TryGetComponent(out Player player))
            _window.Open(_hubZone.GetComponent<TruckKitchen>(), player);
    }

    private void OnTriggerExit(Collider other)
    {
        if (_window != null && _window.IsOpen && other.TryGetComponent(out Player _))
            _window.Close();
    }
}
