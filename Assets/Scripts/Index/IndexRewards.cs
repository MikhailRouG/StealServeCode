using System;
using System.Collections.Generic;

/// <summary>
/// Plain C# model of the index rewards: which <see cref="IndexCategoryData"/> rewards are claimed.
/// A category's reward can be claimed once all its items are discovered (see <see cref="PlayerDiscoveries"/>);
/// it pays money into the <see cref="Wallet"/> and adds a speed bonus to the <see cref="PlayerStats"/>.
/// Shown and used by the <see cref="IndexWindow"/>. Bound in <see cref="GameInstaller"/>.
/// </summary>
public class IndexRewards
{
    private readonly HashSet<IndexCategoryData> _claimed = new HashSet<IndexCategoryData>();
    private readonly PlayerDiscoveries _discoveries;
    private readonly Wallet _wallet;
    private readonly PlayerStats _stats;

    /// <summary>Raised with the category after its reward is claimed.</summary>
    public event Action<IndexCategoryData> Claimed;

    public IndexRewards(PlayerDiscoveries discoveries, Wallet wallet, PlayerStats stats)
    {
        _discoveries = discoveries;
        _wallet = wallet;
        _stats = stats;
    }

    public bool IsClaimed(IndexCategoryData category) => _claimed.Contains(category);

    public bool CanClaim(IndexCategoryData category) =>
        category != null && !IsClaimed(category) && category.IsComplete(_discoveries);

    /// <summary>Gives the category's reward once. Returns false if it is claimed already or not complete.</summary>
    public bool TryClaim(IndexCategoryData category)
    {
        if (!CanClaim(category))
            return false;

        _claimed.Add(category);
        _wallet.Add(category.RewardMoney);
        _stats.AddSpeedBonus(category.RewardSpeedStat);
        Claimed?.Invoke(category);
        return true;
    }
}
