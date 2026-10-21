using UnityEngine;
using Zenject;

/// <summary>
/// Put next to the <see cref="ClientSpawner"/>: sells items from the <see cref="TruckShowcase"/> to its clients.
/// A client buys an item if the item's interest is at least the client's one; the price goes to the player's wallet
/// only once the client has taken the item.
/// Checked when an item is put up for sale (against all walking clients) and when a client spawns
/// (against all shown items). Each client buys at most one item per walk, then walks to the showcase's
/// client point to take it (see <see cref="ClientBuyingState"/>).
/// The money goes to the injected player's <see cref="Wallet"/>.
/// </summary>
[RequireComponent(typeof(ClientSpawner))]
public class ClientSales : MonoBehaviour
{
    [Tooltip("The showcase this spawner's clients buy from.")]
    [SerializeField] private TruckShowcase _showcase;

    private ClientSpawner _spawner;
    private Wallet _wallet;

    private void Awake()
    {
        _spawner = GetComponent<ClientSpawner>();

        if (_showcase == null)
            Debug.LogError($"{nameof(ClientSales)}: showcase is not set.", this);
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

    [Inject]
    private void Construct(Wallet wallet)
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

        if (_showcase.ClientPoint != null)
        {
            client.StartBuying(_showcase, slot, () => Reward(item));
        }
        else
        {
            _showcase.Take(slot);
            client.MarkBought();
            Reward(item);
        }

        return true;
    }

    // Paid only once the client has taken the item.
    private void Reward(DishData item)
    {
        _wallet.Add(item.Price);
    }
}
