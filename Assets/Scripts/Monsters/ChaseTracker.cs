using System;
using R3;
using UnityEngine;

/// <summary>
/// Scene-wide chase state: every <see cref="MonsterChaser"/> reports here when it starts and stops chasing,
/// and anyone interested (e.g. <see cref="AudioManager"/>) reads whether at least one of them chases the player.
/// Bound in <see cref="GameInstaller"/>.
/// </summary>
public class ChaseTracker : IDisposable
{
    private readonly ReactiveProperty<bool> _isPlayerChased = new ReactiveProperty<bool>(false);
    private int _chasingCount;
    private bool _isDisposed;

    /// <summary>True while at least one monster chases the player.</summary>
    public ReadOnlyReactiveProperty<bool> IsPlayerChased => _isPlayerChased;

    /// <summary>Called by a chaser on every change of its own chase state; every true must be matched by one false.</summary>
    public void SetChasing(bool isChasing)
    {
        // Chasers are turned off after the tracker on scene unload.
        if (_isDisposed)
            return;

        _chasingCount = Mathf.Max(0, _chasingCount + (isChasing ? 1 : -1));
        _isPlayerChased.Value = _chasingCount > 0;
    }

    public void Dispose()
    {
        _isDisposed = true;
        _isPlayerChased.Dispose();
    }
}
