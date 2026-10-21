using System;
using UnityEngine;
using Zenject;

/// <summary>
/// Puts the player at the spawn point when the scene starts and brings it back there a delay after its death.
/// Bound in <see cref="GameInstaller"/>.
/// </summary>
public class PlayerRespawner : IInitializable, ITickable, IDisposable
{
    [Serializable]
    public class Settings
    {
        [Tooltip("Where the player starts and respawns after death. If empty, the player's start position in the scene is used.")]
        public Transform SpawnPoint;
        [Tooltip("Seconds between the player's death and respawn.")]
        [Min(0f)] public float RespawnDelay = 5f;
    }

    private readonly Player _player;
    private readonly Settings _settings;
    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;
    private bool _isWaiting;
    private float _respawnTime;

    public PlayerRespawner(Player player, Settings settings)
    {
        _player = player;
        _settings = settings;
    }

    public void Initialize()
    {
        Transform point = _settings.SpawnPoint != null ? _settings.SpawnPoint : _player.transform;
        _spawnPosition = point.position;
        _spawnRotation = point.rotation;

        _player.Teleport(_spawnPosition, _spawnRotation);
        _player.Health.Died += OnPlayerDied;
    }

    public void Dispose()
    {
        _player.Health.Died -= OnPlayerDied;
    }

    public void Tick()
    {
        if (!_isWaiting || Time.time < _respawnTime)
            return;

        _isWaiting = false;
        _player.Respawn(_spawnPosition, _spawnRotation);
    }

    private void OnPlayerDied()
    {
        _isWaiting = true;
        _respawnTime = Time.time + _settings.RespawnDelay;
    }
}
