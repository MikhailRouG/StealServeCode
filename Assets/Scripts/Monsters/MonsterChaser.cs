using R3;
using UnityEngine;
using Zenject;

/// <summary>
/// Doesn't know about items: the spawner switches it between chasing and guarding.
/// Chases the injected <see cref="Player"/> and reports its chase state to the <see cref="ChaseTracker"/>.
/// <see cref="StartChase"/> — chase the player until <see cref="Guard"/> is called or the player dies.
/// <see cref="Guard"/> — stop chasing and walk to the guard point next to the item.
/// Moves a kinematic Rigidbody along the ground plane in FixedUpdate: no gravity, the height stays as spawned.
/// The collider is only for hits, triggers and blocking the player; it doesn't stop the monster's own movement.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class MonsterChaser : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Speed stat, same scale as the player's: 100 = base speed, 1000 = x2 (see SpeedStat).")]
    [SerializeField, Min(SpeedStat.Base)] private float _speedStat = 316f;
    [Tooltip("Speed at speed stat 100. Keep it equal to the player's base walk speed so the stats compare directly.")]
    [SerializeField, Min(0f)] private float _baseSpeed = 2f;
    [Tooltip("How fast the monster turns to face the player, degrees per second.")]
    [SerializeField] private float _turnSpeed = 540f;
    [Tooltip("The monster stops when it is this close to the player.")]
    [SerializeField, Min(0f)] private float _stopDistance = 1f;
    [Tooltip("Seconds the monster waits after the guarded item is picked up before it starts running.")]
    [SerializeField, Min(0f)] private float _chaseDelay = 0.5f;

    [Header("Attack")]
    [SerializeField, Min(0)] private int _damage = 10;
    [Tooltip("The monster hits the player when it is this close.")]
    [SerializeField, Min(0f)] private float _attackRange = 1.3f;
    [Tooltip("Seconds between hits.")]
    [SerializeField, Min(0f)] private float _attackCooldown = 1f;
    [Tooltip("Seconds the monster stands still after a hit before it moves again.")]
    [SerializeField, Min(0f)] private float _pauseAfterAttack = 1f;

    private Rigidbody _rigidbody;
    private Player _target;
    private float _nextAttackTime;
    private float _chaseStartTime;
    private float _moveResumeTime;
    private Vector3 _guardPoint;
    private bool _hasGuardPoint;

    // The monster counts as arrived at its guard point within this horizontal distance.
    private const float GuardPointReachDistance = 0.3f;

    private readonly ReactiveProperty<bool> _isChasing = new ReactiveProperty<bool>(false);
    private readonly ReactiveProperty<bool> _isRunning = new ReactiveProperty<bool>(false);
    private readonly ReactiveProperty<bool> _isReturning = new ReactiveProperty<bool>(false);
    private readonly Subject<Unit> _attacked = new Subject<Unit>();
    private readonly CompositeDisposable _subscriptions = new CompositeDisposable();

    /// <summary>True while the monster chases the player; subscribe to react to the start and end of a chase.</summary>
    public ReadOnlyReactiveProperty<bool> IsChasing => _isChasing;

    /// <summary>True while the monster runs after the player (after the chase delay) or walks to its guard point.</summary>
    public ReadOnlyReactiveProperty<bool> IsRunning => _isRunning;

    /// <summary>True while the monster walks back to its guard point after <see cref="Guard"/>; false once it is there.</summary>
    public ReadOnlyReactiveProperty<bool> IsReturning => _isReturning;

    /// <summary>Fires every time the monster hits the player.</summary>
    public Observable<Unit> Attacked => _attacked;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void OnDestroy()
    {
        _subscriptions.Dispose();
        _isChasing.Dispose();
        _isRunning.Dispose();
        _isReturning.Dispose();
        _attacked.Dispose();
    }

    private void OnEnable()
    {
        _isChasing.Value = false;
        _isRunning.Value = false;
        _isReturning.Value = false;
        _nextAttackTime = 0f;
        _moveResumeTime = 0f;
        _hasGuardPoint = false;
    }

    // A killed monster is disabled mid-chase: it must not keep counting as chasing while hidden.
    private void OnDisable()
    {
        _isChasing.Value = false;
        _isRunning.Value = false;
        _isReturning.Value = false;
    }

    [Inject]
    private void Construct(Player target, ChaseTracker chaseTracker)
    {
        _target = target;

        // ReactiveProperty skips repeated values, so every true is matched by exactly one false
        // (OnDisable drops it to false, so a killed or destroyed chaser doesn't stay counted).
        _isChasing
            .Skip(1)
            .Subscribe(chaseTracker.SetChasing)
            .AddTo(_subscriptions);
    }

    /// <summary>Starts chasing the target (the spawner calls it when the guarded item is picked up).</summary>
    public void StartChase()
    {
        if (!CanChase() || _isChasing.Value)
            return;

        _chaseStartTime = Time.time + _chaseDelay;
        _isReturning.Value = false;
        _isChasing.Value = true;
    }

    /// <summary>Stops the chase and walks to the point (the spawner calls it when the guarded item is back on the map).</summary>
    public void Guard(Vector3 point)
    {
        _guardPoint = point;
        _hasGuardPoint = true;

        // Returning is set first, so whoever reacts to the end of the chase already sees the monster on its way back.
        _isReturning.Value = true;
        _isChasing.Value = false;
    }

    // Kinematic Rigidbody moves belong in FixedUpdate; Time.deltaTime returns the fixed step here.
    private void FixedUpdate()
    {
        if (_isChasing.Value && !CanChase())
            _isChasing.Value = false;

        // During the chase delay the monster already counts as chasing but stands still and doesn't attack.
        bool isChasing = _isChasing.Value && Time.time >= _chaseStartTime;
        Vector3 velocity = Vector3.zero;
        bool isRunning = isChasing;
        if (isChasing)
        {
            velocity = GetVelocityTo(_target.transform.position, _stopDistance);

            // After a hit the monster keeps facing the player but doesn't move.
            if (Time.time < _moveResumeTime)
                velocity = Vector3.zero;
        }
        else if (!_isChasing.Value && _hasGuardPoint)
        {
            velocity = GetGuardPointVelocity();
            isRunning = velocity != Vector3.zero;
        }
        _isRunning.Value = isRunning;

        _rigidbody.MovePosition(_rigidbody.position + velocity * Time.deltaTime);

        if (isChasing)
            TryAttack();
    }

    private bool CanChase()
    {
        return _target != null && _target.gameObject.activeInHierarchy && !_target.Health.IsDead;
    }

    private void TryAttack()
    {
        if (_target.Health.IsDead || Time.time < _nextAttackTime)
            return;

        Vector3 toTarget = _target.transform.position - _rigidbody.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude > _attackRange * _attackRange)
            return;

        _target.Health.TakeDamage(_damage);
        _nextAttackTime = Time.time + _attackCooldown;
        _moveResumeTime = Time.time + _pauseAfterAttack;
        _attacked.OnNext(Unit.Default);
    }

    // Walks to the guard point and forgets it on arrival.
    private Vector3 GetGuardPointVelocity()
    {
        Vector3 velocity = GetVelocityTo(_guardPoint, GuardPointReachDistance);
        if (velocity == Vector3.zero)
        {
            _hasGuardPoint = false;
            _isReturning.Value = false;
        }

        return velocity;
    }

    // Turns towards the point and returns horizontal velocity to it, zero once within the stop distance.
    private Vector3 GetVelocityTo(Vector3 point, float stopDistance)
    {
        Vector3 toTarget = point - _rigidbody.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        Quaternion lookRotation = Quaternion.LookRotation(toTarget);
        _rigidbody.MoveRotation(Quaternion.RotateTowards(_rigidbody.rotation, lookRotation, _turnSpeed * Time.deltaTime));

        if (toTarget.sqrMagnitude <= stopDistance * stopDistance)
            return Vector3.zero;

        return toTarget.normalized * SpeedStat.ToWorldSpeed(_speedStat, _baseSpeed);
    }
}
