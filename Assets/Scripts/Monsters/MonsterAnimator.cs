using R3;
using UnityEngine;

/// <summary>
/// Feeds <see cref="MonsterChaser"/> state into the Animator:
/// "Chase" when the monster starts running, "Unchase" when it stops, "Attack" on every hit.
/// </summary>
[RequireComponent(typeof(MonsterChaser))]
public class MonsterAnimator : MonoBehaviour
{
    private static readonly int ChaseHash = Animator.StringToHash("Chase");
    private static readonly int UnchaseHash = Animator.StringToHash("Unchase");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    [Tooltip("If empty, the first Animator in children is used.")]
    [SerializeField] private Animator _animator;

    private readonly CompositeDisposable _subscriptions = new CompositeDisposable();

    private void OnDestroy()
    {
        _subscriptions.Dispose();
    }

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
        {
            Debug.LogError($"{nameof(MonsterAnimator)}: no Animator found in children.", this);
            enabled = false;
            return;
        }

        // Movement is driven by CharacterController, not by the clips.
        _animator.applyRootMotion = false;
    }

    private void Start()
    {
        if (_animator == null)
            return;

        MonsterChaser chaser = GetComponent<MonsterChaser>();

        // Skip the current value: the Animator already starts in its default (idle) state.
        chaser.IsRunning
            .Skip(1)
            .Subscribe(OnRunningChanged)
            .AddTo(_subscriptions);

        chaser.Attacked
            .Subscribe(_ => SetTrigger(AttackHash))
            .AddTo(_subscriptions);
    }

    private void OnRunningChanged(bool isRunning)
    {
        // Reset the opposite trigger so a stale one doesn't fire later.
        if (_animator.isActiveAndEnabled)
            _animator.ResetTrigger(isRunning ? UnchaseHash : ChaseHash);

        SetTrigger(isRunning ? ChaseHash : UnchaseHash);
    }

    private void SetTrigger(int hash)
    {
        // A killed monster is inactive; its Animator restarts from the default state when it comes back.
        if (_animator.isActiveAndEnabled)
            _animator.SetTrigger(hash);
    }
}
