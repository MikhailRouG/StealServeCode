using System;
using UnityEngine;

/// <summary>
/// Plain C# player health (not a MonoBehaviour), created by the container (see <see cref="GameInstaller"/>).
/// </summary>
public class PlayerHealth
{
    private readonly int _maxHealth;
    private int _currentHealth;

    public int MaxHealth => _maxHealth;
    public int CurrentHealth => _currentHealth;
    public bool IsDead => _currentHealth <= 0;

    /// <summary>Raised with (current, max) whenever health changes.</summary>
    public event Action<int, int> HealthChanged;
    public event Action Died;

    /// <summary>Creates health with the given max, fully healed.</summary>
    public PlayerHealth(int maxHealth)
    {
        _maxHealth = Mathf.Max(1, maxHealth);
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead)
            return;

        SetHealth(_currentHealth - amount);

        if (IsDead)
            Died?.Invoke();
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead)
            return;

        SetHealth(_currentHealth + amount);
    }

    /// <summary>Brings health back to max, also after death.</summary>
    public void Restore()
    {
        SetHealth(_maxHealth);
    }

    private void SetHealth(int value)
    {
        _currentHealth = Mathf.Clamp(value, 0, _maxHealth);
        HealthChanged?.Invoke(_currentHealth, _maxHealth);
    }
}
