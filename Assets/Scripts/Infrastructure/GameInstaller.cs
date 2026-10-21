using UnityEngine;
using Zenject;

/// <summary>
/// Composition root of the game scene: put on the SceneContext object and add it to the context's Mono Installers.
/// Binds the scene-wide services; scene objects (even inactive) get them through their [Inject] methods,
/// which Zenject calls in SceneContext.Awake, before any other Awake.
/// Objects created at runtime that need services are instantiated through Zenject's <see cref="IInstantiator"/>.
/// Only the setup is serialized here; the game state (models) is created in code and starts empty.
/// </summary>
public class GameInstaller : MonoInstaller
{
    [SerializeField] private Player _player;
    [Tooltip("Root of all UI canvases.")]
    [SerializeField] private GameUI _ui;
    [SerializeField] private PlayerSettings _playerSettings = new PlayerSettings();
    [SerializeField] private PlayerRespawner.Settings _respawn = new PlayerRespawner.Settings();

    [Header("Progress")]
    [Tooltip("Price and speed stat bonus of every shop speed upgrade level.")]
    [SerializeField] private StatUpgradeData _speedUpgrade;

    public override void InstallBindings()
    {
        // Zenject can't pass a null argument, so the assets are checked here with a clear message.
        if (_playerSettings.LevelCurve == null)
            Debug.LogError($"{nameof(GameInstaller)}: Player Settings > Level Curve is not set.", this);
        if (_speedUpgrade == null)
            Debug.LogError($"{nameof(GameInstaller)}: Speed Upgrade is not set.", this);

        BindPlayerModels();

        Container.Bind<Player>().FromInstance(_player).AsSingle();
        Container.Bind<CraftingWindow>().FromInstance(_ui.CraftingWindow).AsSingle();

        Container.BindInterfacesAndSelfTo<ChaseTracker>().AsSingle();
        Container.BindInterfacesAndSelfTo<ZoneTracker>().AsSingle();
        Container.BindInterfacesTo<PlayerRespawner>().AsSingle().WithArguments(_respawn);

        // Progress models: the windows only show them and send commands to them.
        Container.Bind<SpeedUpgrades>().AsSingle().WithArguments(_speedUpgrade);
        Container.Bind<IndexRewards>().AsSingle();
    }

    // The player's state: plain C# models, one of each per scene.
    private void BindPlayerModels()
    {
        Container.Bind<PlayerHealth>().AsSingle().WithArguments(_playerSettings.MaxHealth);
        Container.Bind<Inventory>().AsSingle().WithArguments(_playerSettings.MaxItems);
        Container.Bind<Wallet>().AsSingle();
        Container.Bind<PlayerLevel>().AsSingle().WithArguments(_playerSettings.LevelCurve);
        Container.BindInterfacesAndSelfTo<PlayerStats>().AsSingle();
        Container.Bind<PlayerDiscoveries>().AsSingle();
    }
}
