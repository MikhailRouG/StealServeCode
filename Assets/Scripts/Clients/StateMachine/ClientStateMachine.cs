/// <summary>Holds the current <see cref="IClientState"/> of a <see cref="Client"/> and ticks it.</summary>
public class ClientStateMachine
{
    public IClientState Current { get; private set; }

    /// <summary>Exits the current state and enters <paramref name="state"/> (null = no state).</summary>
    public void SetState(IClientState state)
    {
        IClientState previous = Current;
        Current = null;
        previous?.Exit();

        Current = state;
        Current?.Enter();
    }

    public void Tick(float deltaTime)
    {
        Current?.Tick(deltaTime);
    }
}
