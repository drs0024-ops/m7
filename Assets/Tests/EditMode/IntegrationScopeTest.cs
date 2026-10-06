using NUnit.Framework;
using VContainer;
using Game.Gameplay.Player;
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
            var builder = new ContainerBuilder();

            builder.RegisterMessagePipe();

            builder.RegisterInstance(new CharacterSceneConfig
            {
                SpawnPlayerOnStart = false,
                DoorSpawnOffset = 3f
            });

            builder.RegisterInstance(new LevelStateSnapshot(10, 50f));

            builder.Register<SaveableRegistry>(Lifetime.Singleton)
                .As<ISaveableRegistry>()
                .AsSelf();

            builder.Register<IPlayerFactory, FakePlayerFactory>(Lifetime.Scoped);

            using var container = builder.Build();

            Assert.DoesNotThrow(() => container.Resolve<SaveableRegistry>());
            Assert.DoesNotThrow(() => container.Resolve<ISaveableRegistry>());
            Assert.DoesNotThrow(() => container.Resolve<IPlayerFactory>());
            Assert.DoesNotThrow(() => container.Resolve<CharacterSceneConfig>());
            Assert.DoesNotThrow(() => container.Resolve<LevelStateSnapshot>());
        }
    }
}   