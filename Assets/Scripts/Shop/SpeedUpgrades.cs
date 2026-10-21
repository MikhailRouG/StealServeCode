using System;

/// <summary>
/// Plain C# model of the shop's speed upgrade: how many levels of <see cref="StatUpgradeData"/> are bought.
/// Buying spends money from the <see cref="Wallet"/> and adds the level's bonus to the <see cref="PlayerStats"/>.
/// Shown and used by the <see cref="ShopWindow"/>. Bound in <see cref="GameInstaller"/>.
/// </summary>
public class SpeedUpgrades
{
    private readonly StatUpgradeData _data;
    private readonly Wallet _wallet;
    private readonly PlayerStats _stats;

    /// <summary>How many levels are bought, 0 at the start.</summary>
    public int BoughtLevels { get; private set; }
    public bool IsMaxed => _data == null || BoughtLevels >= _data.MaxLevel;
    /// <summary>The level bought next. Check <see cref="IsMaxed"/> first.</summary>
    public StatUpgradeData.UpgradeLevel Next => _data.GetNext(BoughtLevels);
    public bool CanBuy => !IsMaxed && (Next.price <= 0 || _wallet.CanSpend(Next.price));

    /// <summary>Raised after a level is bought.</summary>
    public event Action Changed;

    public SpeedUpgrades(StatUpgradeData data, Wallet wallet, PlayerStats stats)
    {
        _data = data;
        _wallet = wallet;
        _stats = stats;
    }

    /// <summary>Buys the next level if there is one and the player has the money.</summary>
    public bool TryBuy()
    {
        if (!CanBuy)
            return false;

        StatUpgradeData.UpgradeLevel next = Next;
        if (next.price > 0 && !_wallet.TrySpend(next.price))
            return false;

        BoughtLevels++;
        _stats.AddSpeedBonus(next.amount);
        Changed?.Invoke();
        return true;
    }
}
