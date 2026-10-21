using UnityEngine;

/// <summary>
/// Client driven by a small state machine (see <see cref="ClientStateMachine"/>):
/// <list type="number">
/// <item><see cref="ClientWalkState"/> — walks along X to the target X and turns itself off there,
/// so <see cref="ClientSpawner"/> can reuse it;</item>
/// <item><see cref="ClientBuyingState"/> — after buying, walks to the showcase's client point, takes the item,
/// comes back to its path and walks on.</item>
/// </list>
/// Has an interest that decides which items it buys (see <see cref="ClientSales"/>).
/// </summary>
public class Client : MonoBehaviour
{
    [Tooltip("Seconds the client stands at the client point taking the item.")]
    [SerializeField, Min(0f)] private float _pickupDuration = 1f;

    private ClientStateMachine _stateMachine;
    private ClientWalkState _walkState;
    private ClientBuyingState _buyingState;

    /// <summary>Current movement speed, 0 when standing.</summary>
    public float Speed => _stateMachine.Current?.Speed ?? 0f;
    public float WalkSpeed { get; private set; }
    public float TargetX { get; private set; }
    public float PickupDuration => _pickupDuration;
    /// <summary>Buys an item only if the item's interest is at least this. New on every spawn.</summary>
    public int Interest { get; private set; }
    /// <summary>The client buys at most one item per walk.</summary>
    public bool HasBought { get; private set; }

    private void Awake()
    {
        _stateMachine = new ClientStateMachine();
        _walkState = new ClientWalkState(this);
        _buyingState = new ClientBuyingState(this, _stateMachine, _walkState);
    }

    private void OnDisable()
    {
        _stateMachine.SetState(null);
    }

    /// <summary>Called by the spawner: the client faces <paramref name="targetX"/> and walks to it along X.</summary>
    public void Init(float targetX, float speed, int interest)
    {
        TargetX = targetX;
        WalkSpeed = speed;
        Interest = interest;
        HasBought = false;

        _stateMachine.SetState(_walkState);
    }

    /// <summary>
    /// The item in <paramref name="slot"/> is chosen: go to the showcase's client point and take it.
    /// <paramref name="taken"/> is called when the item is taken (that's when the player gets paid).
    /// </summary>
    public void StartBuying(TruckShowcase showcase, int slot, System.Action taken)
    {
        HasBought = true;
        _buyingState.Setup(showcase, slot, taken);
        _stateMachine.SetState(_buyingState);
    }

    /// <summary>Marks the client as having bought without walking to the showcase.</summary>
    public void MarkBought()
    {
        HasBought = true;
    }

    private void Update()
    {
        _stateMachine.Tick(Time.deltaTime);
    }
}
