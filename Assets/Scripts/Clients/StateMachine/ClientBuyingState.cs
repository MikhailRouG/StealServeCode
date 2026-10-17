using UnityEngine;

/// <summary>
/// The client has bought an item: walks to the showcase's client point, stands there a moment
/// and takes the item (its model leaves the showcase), then walks back to where it left its path
/// and goes back to <see cref="ClientWalkState"/>.
/// </summary>
public class ClientBuyingState : IClientState
{
    private enum Phase
    {
        GoToPoint,
        Take,
        Return,
    }

    private readonly Client _client;
    private readonly ClientStateMachine _stateMachine;
    private readonly IClientState _walkState;

    private TruckShowcase _showcase;
    private int _slot;
    private bool _isTaken;
    private Vector3 _returnPosition;
    private Phase _phase;
    private float _takeTimer;

    public ClientBuyingState(Client client, ClientStateMachine stateMachine, IClientState walkState)
    {
        _client = client;
        _stateMachine = stateMachine;
        _walkState = walkState;
    }

    public float Speed => _phase == Phase.Take ? 0f : _client.WalkSpeed;

    /// <summary>Sets the bought item. Called right before entering the state.</summary>
    public void Setup(TruckShowcase showcase, int slot)
    {
        _showcase = showcase;
        _slot = slot;
    }

    public void Enter()
    {
        _isTaken = false;
        _returnPosition = _client.transform.position;
        _phase = Phase.GoToPoint;
    }

    public void Tick(float deltaTime)
    {
        switch (_phase)
        {
            case Phase.GoToPoint:
                if (MoveTo(_showcase.ClientPoint.position, deltaTime))
                {
                    TakeItem();
                    _takeTimer = _client.PickupDuration;
                    _phase = Phase.Take;
                }
                break;

            case Phase.Take:
                _takeTimer -= deltaTime;
                if (_takeTimer <= 0f)
                    _phase = Phase.Return;
                break;

            case Phase.Return:
                if (MoveTo(_returnPosition, deltaTime))
                    _stateMachine.SetState(_walkState);
                break;
        }
    }

    public void Exit()
    {
        // The client can be turned off on the way; the item is paid for, so free the slot anyway.
        TakeItem();
        _showcase = null;
    }

    private void TakeItem()
    {
        if (_isTaken || _showcase == null)
            return;

        _isTaken = true;
        _showcase.Take(_slot);
    }

    // Moves on the client's own height toward the target and faces the way. True when there.
    private bool MoveTo(Vector3 target, float deltaTime)
    {
        Transform transform = _client.transform;
        Vector3 position = transform.position;
        target.y = position.y;

        Vector3 direction = target - position;
        if (direction.sqrMagnitude > 0f)
            transform.rotation = Quaternion.LookRotation(direction);

        transform.position = Vector3.MoveTowards(position, target, _client.WalkSpeed * deltaTime);
        return transform.position == target;
    }
}
