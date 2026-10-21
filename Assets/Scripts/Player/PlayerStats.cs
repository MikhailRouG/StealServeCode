using System;

/// <summary>
/// Plain C# player stats (not a MonoBehaviour), created by the container (see <see cref="GameInstaller"/>). Kept on death.
/// The speed stat is the level's one (see <see cref="LevelCurveData"/>) plus permanent bonuses
/// (shop upgrades, index rewards). <see cref="PlayerMove"/> turns it into the walk speed.
/// </summary>
public class PlayerStats : IDisposable
{
    private readonly PlayerLevel _level;
    private float _bonusSpeedStat;

    /// <summary>Speed stat with all bonuses: 100 at the start (see <see cref="SpeedStat"/>).</summary>
    public float CurrentSpeedStat => SpeedStat.Clamp(_level.LevelSpeedStat + _bonusSpeedStat);
    /// <summary>Sum of all permanent bonuses on top of the level's speed stat.</summary>
    public float BonusSpeedStat => _bonusSpeedStat;

    /// <summary>Raised with the new <see cref="CurrentSpeedStat"/> on level up or a new bonus.</summary>
    public event Action<float> SpeedStatChanged;

    public PlayerStats(PlayerLevel level)
    {
        _level = level;
        _level.LevelUp += OnLevelUp;
    }

    public void Dispose()
    {
        _level.LevelUp -= OnLevelUp;
    }

    /// <summary>Adds a permanent bonus on top of the level's speed stat.</summary>
    public void AddSpeedBonus(float amount)
    {
        if (amount <= 0f)
            return;

        _bonusSpeedStat += amount;
        SpeedStatChanged?.Invoke(CurrentSpeedStat);
    }

    private void OnLevelUp(int level)
    {
        SpeedStatChanged?.Invoke(CurrentSpeedStat);
    }
}
