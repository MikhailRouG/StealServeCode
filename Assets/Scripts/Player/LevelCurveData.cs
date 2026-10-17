using System;
using UnityEngine;

/// <summary>
/// Per-level stats, as an explicit, designer-tunable table.
/// Create via Assets > Create > Player > Level Curve.
/// </summary>
[CreateAssetMenu(fileName = "NewLevelCurve", menuName = "Player/Level Curve")]
public class LevelCurveData : ScriptableObject
{
    [Serializable]
    public struct LevelStep
    {
        [Tooltip("Experience needed to go from this level to the next.")]
        public int experienceToNextLevel;
        [Tooltip("Speed stat at this level. 100 = base walk speed, 1000 = x2, 10000 = x3 (see PlayerMove).")]
        public float speedStat;
    }

    [Tooltip("One entry per level, indexed from level 1 (index 0 = level 1). Levels beyond the list reuse the last entry.")]
    [SerializeField] private LevelStep[] _levels =
    {
        new LevelStep { experienceToNextLevel = 100, speedStat = 100f },
        new LevelStep { experienceToNextLevel = 150, speedStat = 126f },
        new LevelStep { experienceToNextLevel = 225, speedStat = 158f },
        new LevelStep { experienceToNextLevel = 340, speedStat = 200f },
        new LevelStep { experienceToNextLevel = 510, speedStat = 250f },
    };

    public int GetExperienceToNextLevel(int level) => GetStep(level).experienceToNextLevel;
    public float GetSpeedStat(int level) => GetStep(level).speedStat;

    private LevelStep GetStep(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, _levels.Length - 1);
        return _levels[index];
    }
}
