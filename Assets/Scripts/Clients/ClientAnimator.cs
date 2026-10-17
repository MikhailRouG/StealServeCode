using UnityEngine;

/// <summary>
/// Feeds the <see cref="Client"/> speed into the Animator's "Speed" parameter (Idle <-> Walk).
/// </summary>
[RequireComponent(typeof(Client))]
public class ClientAnimator : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    [Tooltip("If empty, the first Animator in children is used.")]
    [SerializeField] private Animator _animator;

    private Client _client;

    private void Awake()
    {
        _client = GetComponent<Client>();
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
        {
            Debug.LogError($"{nameof(ClientAnimator)}: no Animator found in children.", this);
            enabled = false;
            return;
        }

        // Movement is driven by Client, not by the clips.
        _animator.applyRootMotion = false;
    }

    private void Update()
    {
        _animator.SetFloat(SpeedHash, _client.Speed);
    }
}
