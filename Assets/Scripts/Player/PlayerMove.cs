using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerMove : MonoBehaviour
{
    [Header("Speed")]
    [Tooltip("Speed stat per level. See LevelCurveData.")]
    [SerializeField] private LevelCurveData _levelCurve;
    [Tooltip("Walk speed at speed stat 100.")]
    [SerializeField, Min(0f)] private float _baseWalkSpeed = 2f;

    [Header("Rotation")]
    [Tooltip("How fast the player turns to face the movement direction, degrees per second.")]
    [SerializeField] private float _turnSpeed = 720f;
    [SerializeField] private bool _lockCursor = true;

    [Header("Jump & Gravity")]
    [SerializeField] private float _jumpHeight = 1.2f;
    [SerializeField] private float _gravity = -20f;
    [SerializeField] private float _groundedStickForce = -2f;

    private CharacterController _controller;
    private PlayerInputHandler _input;
    private Vector3 _horizontalVelocity;
    private float _verticalVelocity;
    private float _walkSpeed;
    private float _levelSpeedStat = SpeedStat.Base;
    private float _bonusSpeedStat;

    public bool IsGrounded => _controller.isGrounded;
    public float CurrentSpeed => _horizontalVelocity.magnitude;
    /// <summary>True while <see cref="Freeze"/> holds the player in place.</summary>
    public bool IsFrozen { get; private set; }
    /// <summary>Displayed speed stat: 100 at the start.</summary>
    public float CurrentSpeedStat { get; private set; } = SpeedStat.Base;

    /// <summary>Raised with the new speed stat.</summary>
    public event Action<float> SpeedStatChanged;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<PlayerInputHandler>();
    }

    private void OnEnable()
    {
        SetCursorLocked(!IsFrozen);
    }

    private void OnDisable()
    {
        SetCursorLocked(false);
    }

    private void Update()
    {
        Vector3 moveDirection = IsFrozen ? Vector3.zero : GetMoveDirection();
        RotateTowards(moveDirection);

        _horizontalVelocity = moveDirection * _walkSpeed;

        ApplyGravityAndJump();

        Vector3 velocity = _horizontalVelocity + Vector3.up * _verticalVelocity;
        _controller.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// Holds the player in place for menus: input and jumps are ignored and <see cref="CurrentSpeed"/> drops to 0,
    /// so the animator goes idle (unlike turning the component off). Gravity still applies. Frees the cursor while frozen.
    /// </summary>
    public void Freeze(bool frozen)
    {
        IsFrozen = frozen;
        if (frozen)
            _horizontalVelocity = Vector3.zero;

        if (isActiveAndEnabled)
            SetCursorLocked(!frozen);
    }

    /// <summary>Drops all current velocity, e.g. after a teleport.</summary>
    public void Stop()
    {
        _horizontalVelocity = Vector3.zero;
        _verticalVelocity = 0f;
    }

    /// <summary>Sets the speed stat for the given player level. Bonuses from <see cref="AddSpeedStat"/> are kept.</summary>
    public void SetLevel(int level)
    {
        _levelSpeedStat = _levelCurve.GetSpeedStat(level);
        ApplySpeedStat();
    }

    /// <summary>Adds a permanent bonus on top of the level's speed stat (e.g. an index reward).</summary>
    public void AddSpeedStat(float amount)
    {
        if (amount <= 0f)
            return;

        _bonusSpeedStat += amount;
        ApplySpeedStat();
    }

    private void ApplySpeedStat()
    {
        CurrentSpeedStat = SpeedStat.Clamp(_levelSpeedStat + _bonusSpeedStat);
        _walkSpeed = SpeedStat.ToWorldSpeed(CurrentSpeedStat, _baseWalkSpeed);
        SpeedStatChanged?.Invoke(CurrentSpeedStat);
    }

    // Smoothly turns the player to face the direction it is moving in.
    private void RotateTowards(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _turnSpeed * Time.deltaTime);
    }

    // Movement uses fixed world axes, independent of the player's facing.
    private Vector3 GetMoveDirection()
    {
        Vector2 input = Vector2.ClampMagnitude(_input.MoveInput, 1f);

        //Vector3 forward = Vector3.forward;
        //Vector3 right = Vector3.right;
        Vector3 right = -Vector3.forward;
        Vector3 forward = Vector3.right;
        return forward * input.y + right * input.x;
    }

    private void ApplyGravityAndJump()
    {
        if (_controller.isGrounded)
        {
            if (_verticalVelocity < 0f)
                _verticalVelocity = _groundedStickForce;

            if (!IsFrozen && _input.JumpPressed)
                _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
        }

        _verticalVelocity += _gravity * Time.deltaTime;
    }

    private void SetCursorLocked(bool locked)
    {
        if (!_lockCursor)
            return;

        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
