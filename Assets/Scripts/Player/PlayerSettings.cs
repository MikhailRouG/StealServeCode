using System;
using UnityEngine;

/// <summary>
/// Designer-tunable player setup. The player's models (inventory, wallet, level, health) are created in code
/// from these values and start empty; only this setup is saved with the scene. Set in <see cref="GameInstaller"/>.
/// </summary>
[Serializable]
public class PlayerSettings
{
    [Min(1)] public int MaxHealth = 100;
    [Tooltip("Max number of items the player carries at once. 0 = unlimited.")]
    [Min(0)] public int MaxItems = 1;
    [Tooltip("Experience and speed stat per level.")]
    public LevelCurveData LevelCurve;
}
