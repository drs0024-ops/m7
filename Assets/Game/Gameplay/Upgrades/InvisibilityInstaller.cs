using Game.Core.Messages;
using Game.Core.Interfaces;
using Game.Gameplay.Enemies.Runtime;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Upgrades
{
    /// <summary>Registers global invisibility brokers and scene-scoped services.</summary>
    public sealed class InvisibilityInstaller : IInstaller
    {
        /// <summary>Registers charge-system brokers using the root MessagePipe options.</summary>
        public void Install(IContainerBuilder builder)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            MessagePipeOptions options = builder.RegisterMessagePipe(settings => settings.EnableCaptureStackTrace = true);
#else
            MessagePipeOptions options = builder.RegisterMessagePipe();
#endif
            builder.RegisterMessageBroker<InvisibilityChargeChanged>(options);
            builder.RegisterMessageBroker<InvisibilityBroadcastStarted>(options);
            builder.RegisterMessageBroker<InvisibilityBroadcastStopped>(options);
            builder.RegisterMessageBroker<InvisibilityBroadcastStateChanged>(options);
            builder.RegisterMessageBroker<InvisibilityChargingChanged>(options);
            builder.RegisterMessageBroker<InvisibilityInterrupted>(options);
            builder.RegisterMessageBroker<InvisibilityTierChanged>(options);
            builder.RegisterMessageBroker<EnemyInvestigationRequested>(options);
            builder.RegisterMessageBroker<EnemyInvestigationCancelled>(options);
            builder.RegisterMessageBroker<EnemySpawned>(options);
            builder.RegisterMessageBroker<EnemyDespawned>(options);
            builder.RegisterMessageBroker<EnemyPerceptionChanged>(options);
            builder.RegisterMessageBroker<EnemyStateChanged>(options);
            builder.RegisterMessageBroker<EnemyMoveRequested>(options);
            builder.RegisterMessageBroker<EnemyStopRequested>(options);
            builder.RegisterMessageBroker<EnemyAttackRequested>(options);

            builder.RegisterBuildCallback(resolver =>
                GlobalMessagePipe.SetProvider(resolver.AsServiceProvider()));
        }

        /// <summary>Registers the scoped model and scene components in a facility scope.</summary>
        public static void InstallSceneScoped(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<InvisibilityModel>(Lifetime.Scoped)
                .As<IInvisibilityModel>()
                .AsSelf();
            builder.RegisterComponentInHierarchy<InvisibilityController>();
            builder.RegisterComponentInHierarchy<Game.Gameplay.Player.InvisibilityVisualPresenter>();
        }
    }
}