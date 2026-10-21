using System;
using UnityEngine;

/// <summary>
/// Shop upgrade of a stat, as an explicit, designer-tunable table: for every upgrade level its price
/// and how much it adds to the stat. Bought in the <see cref="ShopWindow"/> one level after another;
/// once all levels are bought the upgrade is maxed out.
/// Create via Assets > Create > Shop > Stat Upgrade.
/// </summary>
[CreateAssetMenu(fileName = "NewStatUpgrade", menuName = "Shop/Stat Upgrade")]
public class StatUpgradeData : ScriptableObject
{
    [Serializable]
    public struct UpgradeLevel
    {
        [Tooltip("Money needed to buy this level.")]
        [Min(0)] public int price;
        [Tooltip("How much this level adds to the stat.")]
        [Min(0f)] public float amount;
    }

    [Tooltip("One entry per purchase, in order (index 0 = first purchase).")]
    [SerializeField] private UpgradeLevel[] _levels =
    {
        new UpgradeLevel { price = 50, amount = 10f },
        new UpgradeLevel { price = 75, amount = 10f },
        new UpgradeLevel { price = 115, amount = 15f },
        new UpgradeLevel { price = 170, amount = 15f },
        new UpgradeLevel { price = 250, amount = 20f },
    };

    /// <summary>How many levels can be bought.</summary>
    public int MaxLevel => _levels.Length;

    /// <summary>The level bought after <paramref name="boughtLevels"/> purchases. Check <see cref="MaxLevel"/> first.</summary>
    public UpgradeLevel GetNext(int boughtLevels) => _levels[boughtLevels];
}
