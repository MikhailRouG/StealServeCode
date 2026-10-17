using UnityEngine;

/// <summary>
/// Shared speed stat formula for the player and monsters. The stat starts at <see cref="Base"/>;
/// every x10 of the stat adds +1 to the multiplier (100 -> x1, 1000 -> x2, 10000 -> x3).
/// </summary>
public static class SpeedStat
{
    public const float Base = 100f;

    public static float Clamp(float stat) => Mathf.Max(stat, Base);

    public static float ToMultiplier(float stat) => 1f + Mathf.Log10(Clamp(stat) / Base);

    /// <summary>World speed for the stat, where <paramref name="baseSpeed"/> is the speed at stat 100.</summary>
    public static float ToWorldSpeed(float stat, float baseSpeed) => baseSpeed * ToMultiplier(stat);
}
