/// <summary>One state of the <see cref="Client"/> (see <see cref="ClientStateMachine"/>).</summary>
public interface IClientState
{
    /// <summary>Current movement speed, 0 when standing. Read by <see cref="ClientAnimator"/>.</summary>
    float Speed { get; }

    void Enter();
    void Tick(float deltaTime);
    void Exit();
}
