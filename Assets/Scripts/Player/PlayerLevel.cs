using System;

/// <summary>
/// Plain C# player level (not a MonoBehaviour), created by the container (see <see cref="GameInstaller"/>). Kept on death.
/// Starts at level 1 with no experience. Experience fills up to <see cref="ExperienceToNextLevel"/>,
/// then the level goes up and the rest carries over.
/// </summary>
public class PlayerLevel
{
    private readonly LevelCurveData _curve;
    private int _level = 1;
    private int _experience;

    public int Level => _level;
    /// <summary>Experience collected on the current level.</summary>
    public int Experience => _experience;
    public int ExperienceToNextLevel => _curve.GetExperienceToNextLevel(_level);
    /// <summary>Speed stat of the current level, without bonuses (see <see cref="PlayerStats"/>).</summary>
    public float LevelSpeedStat => _curve.GetSpeedStat(_level);

    /// <summary>Raised with (experience, experience to next level) whenever experience changes.</summary>
    public event Action<int, int> ExperienceChanged;
    /// <summary>Raised with the new level.</summary>
    public event Action<int> LevelUp;

    public PlayerLevel(LevelCurveData curve)
    {
        _curve = curve;
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0)
            return;

        _experience += amount;
        RollOverLevels();
        ExperienceChanged?.Invoke(_experience, ExperienceToNextLevel);
    }

    // A big reward can roll over several levels at once.
    private void RollOverLevels()
    {
        while (_experience >= ExperienceToNextLevel)
        {
            _experience -= ExperienceToNextLevel;
            _level++;
            LevelUp?.Invoke(_level);
        }
    }
}
