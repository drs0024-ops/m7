using NUnit.Framework;
using VContainer;
using VContainer.Unity;
using Game.Gameplay.Player;
using Game.Gameplay.World;
using MessagePipe;
using Game.Core;

namespace Game.Tests
{
    [TestFixture]
    public class IntegrationScopeTest
    {
        [Test]
        public void SceneScope_ResolvesAllExpectedTypes()
        {
            using var scope = LifetimeScope.Create(builder =>
            {
                // Mirror FacilitySceneScope registrations (minimal)
                builder.RegisterMessagePipe(o => o.EnableCaptureStackTrace = true);

                builder.RegisterInstance(new CharacterSceneConfig
                {
                    SpawnPlayerOnStart = false,
                    DoorSpawnOffset = 3f
                });

                builder.RegisterInstance(new LevelStateSnapshot(10, 50f));

                builder.Register<CheckpointManager>(Lifetime.Scoped);
                builder.Register<PlayerSpawnerService>(Lifetime.Scoped);
                builder.Register<PlayerHealth>(Lifetime.Scoped);
            });

            Assert.DoesNotThrow(() => scope.Container.Resolve<CheckpointManager>());
            Assert.DoesNotThrow(() => scope.Container.Resolve<PlayerSpawnerService>());
        }
    }
}   