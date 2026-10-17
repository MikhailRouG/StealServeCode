using UnityEngine;

/// <summary>
/// Feeds movement state from <see cref="PlayerMove"/> into the Animator (Idle <-> Run)
/// and switches to the carry animations while the inventory has items.
/// </summary>
[RequireComponent(typeof(PlayerMove))]
[RequireComponent(typeof(Player))]
public class PlayerAnimator : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int GetItemHash = Animator.StringToHash("GetItem");
    private static readonly int LostItemHash = Animator.StringToHash("LostItem");

    [Tooltip("If empty, the first Animator in children is used.")]
    [SerializeField] private Animator _animator;
    [Tooltip("Smoothing time for the Speed parameter.")]
    [SerializeField] private float _speedDampTime = 0.1f;

    private PlayerMove _move;
    private Player _player;
    private bool _isCarrying;

    private void Awake()
    {
        _move = GetComponent<PlayerMove>();
        _player = GetComponent<Player>();
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
        {
            Debug.LogError($"{nameof(PlayerAnimator)}: no Animator found in children.", this);
            enabled = false;
            return;
        }

        // Movement is driven by CharacterController, not by the clips.
        _animator.applyRootMotion = false;
    }

    private void OnEnable()
    {
        _player.Inventory.Changed += OnInventoryChanged;

        // The Animator restarts from its default state when the player is re-enabled (e.g. after respawn).
        _isCarrying = false;
        OnInventoryChanged();
    }

    private void OnDisable()
    {
        _player.Inventory.Changed -= OnInventoryChanged;
    }

    private void Update()
    {
        _animator.SetFloat(SpeedHash, _move.CurrentSpeed, _speedDampTime, Time.deltaTime);
    }

    private void OnInventoryChanged()
    {
        if (_animator == null)
            return;

        bool isCarrying = _player.Inventory.Count > 0;
        if (isCarrying == _isCarrying)
            return;

        _isCarrying = isCarrying;
        _animator.ResetTrigger(isCarrying ? LostItemHash : GetItemHash);
        _animator.SetTrigger(isCarrying ? GetItemHash : LostItemHash);
    }
}
