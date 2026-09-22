using VContainer;
using VContainer.Unity;
using Game.Core.Interfaces;
using Game.Gameplay.Player;
using Game.Gameplay.Upgrades;
using Game.Gameplay.Achievements;
using Game.Gameplay.World;

namespace Game.Bootstrap.Scopes
{
    public class LevelLifetimeScope : LifetimeScope
    {
       protected override void Configure(IContainerBuilder builder)
        {
            // --- Player ---
            builder.Register<PlayerPrefabReference>(Lifetime.Scoped);
            builder.Register<IPlayerFactory, PlayerFactory>(Lifetime.Transient);

            builder.Register<PlayerSpawnerService>(Lifetime.Scoped)
                .AsSelf()
                .AsImplementedInterfaces();

            // --- Upgrades ---
            builder.Register<UpgradeStateManager>(Lifetime.Scoped)
                .AsSelf()
                .AsImplementedInterfaces();

            builder.Register<UpgradeOrchestrator>(Lifetime.Scoped)
                .As<IUpgradeManager>()
                .AsImplementedInterfaces();

            builder.Register<BombUpgradeHandler>(Lifetime.Scoped)
                .AsSelf()
                .AsImplementedInterfaces();

            // --- Achievements ---
            builder.Register<ObjectiveTracker>(Lifetime.Scoped)
                .AsSelf()
                .AsImplementedInterfaces();

            builder.RegisterComponentInHierarchy<CheckpointManager>()
                .AsImplementedInterfaces()
                .AsSelf();

            

            // --- UI ---
            //builder.Register<NotificationPanel>(Lifetime.Transient);

            builder.Register<OrbCounter>(Lifetime.Scoped)
                .AsSelf()
                .AsImplementedInterfaces();

            // --- Scene Components ---
            builder.RegisterComponentInHierarchy<InvisibilityInputHandler>()
                .AsImplementedInterfaces();
            builder.RegisterComponentInHierarchy<InvisibilityVisualPresenter>()
                .AsImplementedInterfaces();
        }   
    }
}   