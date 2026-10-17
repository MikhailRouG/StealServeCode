using System;
using UnityEngine;

/// <summary>
/// Plain C# player level (not a MonoBehaviour), owned by <see cref="Player"/>. Kept on death.
/// Experience fills up to <see cref="ExperienceToNextLevel"/>, then the level goes up and the rest carries over.
/// </summary>
[Serializable]
public class PlayerLevel
{
    [SerializeField] private LevelCurveData _curve;

    [SerializeField, Min(1)] private int _level = 1;
    [SerializeField, Min(0)] private int _experience;

    public int Level => _level;
    /// <summary>Experience collected on the current level.</summary>
    public int Experience => _experience;
    public int ExperienceToNextLevel => _curve.GetExperienceToNextLevel(_level);

    /// <summary>Raised with (experience, experience to next level) whenever experience changes.</summary>
    public event Action<int, int> ExperienceChanged;
    /// <summary>Raised with the new level.</summary>
    public event Action<int> LevelUp;

    public void AddExperience(int amount)
    {
        if (amount <= 0)
            return;

        _experience += amount;
        RollOverLevels();
        ExperienceChanged?.Invoke(_experience, ExperienceToNextLevel);
    }

    /// <summary>
    /// Rolls any experience already on this level into level-ups, without raising <see cref="ExperienceChanged"/>.
    /// Call once after deserialization in case <see cref="Experience"/> was set directly (e.g. in the Inspector)
    /// without <see cref="Level"/> being kept in sync.
    /// </summary>
    public void Recalculate()
    {
        RollOverLevels();
    }

    // A big reward (or a desynced Experience/Level pair) can roll over several levels at once.
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
