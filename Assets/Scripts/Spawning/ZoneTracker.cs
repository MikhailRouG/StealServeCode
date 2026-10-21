using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using Zenject;

/// <summary>
/// The only place that knows where the player is: every frame checks the player's position against the
/// registered <see cref="Zone"/>s. Polling instead of triggers, because a trigger misses the exit when the player
/// is turned off on death or moved by <see cref="Player.Teleport"/>.
/// <see cref="CurrentZone"/> uses the exact bounds; a zone's activity uses its margins with hysteresis.
/// Bound in <see cref="GameInstaller"/>.
/// </summary>
public class ZoneTracker : ITickable, IDisposable
{
    private readonly Player _player;
    private readonly List<Zone> _zones = new List<Zone>();
    private readonly ReactiveProperty<Zone> _currentZone = new ReactiveProperty<Zone>(null);

    /// <summary>The zone the player stands in (the highest priority one if several), or null.</summary>
    public ReadOnlyReactiveProperty<Zone> CurrentZone => _currentZone;

    public ZoneTracker(Player player)
    {
        _player = player;
    }

    public void Register(Zone zone)
    {
        if (zone != null && !_zones.Contains(zone))
            _zones.Add(zone);
    }

    public void Unregister(Zone zone)
    {
        _zones.Remove(zone);
    }

    public void Tick()
    {
        // A dead player is turned off: keep everything as it is until the respawn.
        if (!_player.gameObject.activeInHierarchy)
            return;

        Vector3 position = _player.transform.position;
        Zone current = null;

        foreach (Zone zone in _zones)
        {
            if (zone.Contains(position) && (current == null || zone.Priority > current.Priority))
                current = zone;

            float margin = zone.IsActive.CurrentValue ? zone.ExitMargin : zone.EnterMargin;
            zone.SetIsActive(zone.Contains(position, margin));
        }

        _currentZone.Value = current;
    }

    public void Dispose()
    {
        _currentZone.Dispose();
    }
}
