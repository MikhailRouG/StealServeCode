using System;

/// <summary>
/// Plain C# player money (not a MonoBehaviour), created by the container (see <see cref="GameInstaller"/>).
/// Starts at 0. Kept on death.
/// </summary>
public class Wallet
{
    private int _money;

    public int Money => _money;

    /// <summary>Raised with the new amount whenever money changes.</summary>
    public event Action<int> Changed;

    public void Add(int amount)
    {
        if (amount <= 0)
            return;

        _money += amount;
        Changed?.Invoke(_money);
    }

    public bool CanSpend(int amount) => amount > 0 && _money >= amount;

    /// <summary>Spends only if there is enough money. Returns false otherwise.</summary>
    public bool TrySpend(int amount)
    {
        if (!CanSpend(amount))
            return false;

        _money -= amount;
        Changed?.Invoke(_money);
        return true;
    }
}
