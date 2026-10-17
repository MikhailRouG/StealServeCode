using UnityEngine;

/// <summary>
/// Put next to the <see cref="ClientSpawner"/>: sells items from the <see cref="TruckShowcase"/> to its clients.
/// A client buys an item if the item's interest is at least the client's one; the price goes to the player's wallet.
/// Checked when an item is put up for sale (against all walking clients) and when a client spawns
/// (against all shown items). Each client buys at most one item per walk, then walks to the showcase's
/// client point to take it (see <see cref="ClientBuyingState"/>).
/// Initialized by <see cref="Bootstrap"/>.
/// </summary>
[RequireComponent(typeof(ClientSpawner))]
public class ClientSales : MonoBehaviour
{
    [Tooltip("If empty, the first TruckShowcase in the scene is used.")]
    [SerializeField] private TruckShowcase _showcase;

    private ClientSpawner _spawner;
    private Wallet _wallet;

    private void Awake()
    {
        _spawner = GetComponent<ClientSpawner>();

        if (_showcase == null)
            _showcase = FindFirstObjectByType<TruckShowcase>();
    }

    private void OnEnable()
    {
        _spawner.Spawned += OnClientSpawned;
        if (_showcase != null)
            _showcase.Placed += OnItemPlaced;
    }

    private void OnDisable()
    {
        _spawner.Spawned -= OnClientSpawned;
        if (_showcase != null)
            _showcase.Placed -= OnItemPlaced;
    }

    /// <summary>Sets where the money goes. Called by <see cref="Bootstrap"/> before the clients spawn.</summary>
    public void Init(Wallet wallet)
    {
        _wallet = wallet;
    }

    private void OnClientSpawned(Client client)
    {
        TryBuy(client);
    }

    private void OnItemPlaced(DishData item)
    {
        foreach (Client client in _spawner.Clients)
        {
            if (client.gameObject.activeSelf && client.Interest <= item.Interest && TryBuy(client))
                return;
        }
    }

    private bool TryBuy(Client client)
    {
        if (_showcase == null || client.HasBought || !_showcase.TrySell(client.Interest, out DishData item, out int slot))
            return false;

        _wallet?.Add(item.Price);

        if (_showcase.ClientPoint != null)
        {
            client.StartBuying(_showcase, slot);
        }
        else
        {
            _showcase.Take(slot);
            client.MarkBought();
        }

        return true;
    }
}
