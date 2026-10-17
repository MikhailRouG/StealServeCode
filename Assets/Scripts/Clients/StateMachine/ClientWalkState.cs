using UnityEngine;

/// <summary>
/// The client walks straight along the X axis to its target X and turns itself off there,
/// so <see cref="ClientSpawner"/> can reuse it. Y and Z don't change.
/// </summary>
public class ClientWalkState : IClientState
{
    private readonly Client _client;

    public ClientWalkState(Client client)
    {
        _client = client;
    }

    public float Speed => _client.WalkSpeed;

    public void Enter()
    {
        float direction = Mathf.Sign(_client.TargetX - _client.transform.position.x);
        if (direction != 0f)
            _client.transform.rotation = Quaternion.LookRotation(Vector3.right * direction);
    }

    public void Tick(float deltaTime)
    {
        Transform transform = _client.transform;
        Vector3 position = transform.position;
        position.x = Mathf.MoveTowards(position.x, _client.TargetX, _client.WalkSpeed * deltaTime);
        transform.position = position;

        if (position.x == _client.TargetX)
            _client.gameObject.SetActive(false);
    }

    public void Exit()
    {
    }
}
