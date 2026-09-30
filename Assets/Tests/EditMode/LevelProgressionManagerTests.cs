using System;
using NUnit.Framework;
using Game.Bootstrap;
using Game.Core.Messages;
using Game.Tests;
using Game.Gameplay.Level;

namespace Game.Tests
{
    [TestFixture]
    public class LevelProgressionManagerTests
    {
        private FakeSubscriber<LevelCompleteSignal> _completeSub;
        private FakeSubscriber<RestartLevelSignal> _restartSub;
        private FakeSubscriber<LoadLevelSignal> _loadSub;
        private FakePublisher<GameCompletedSignal> _gameCompletedPub;
        private FakePublisher<SceneLoadedSignal> _sceneLoadedPub;
        private FakePublisher<LevelProgressionChangedSignal> _progressionPub;
        private FakeSceneTransitionDirector _transitionDirector;

        private LevelProgressionManager _sut;

        [SetUp]
        public void SetUp()
        {
            _completeSub = new FakeSubscriber<LevelCompleteSignal>();
            _restartSub = new FakeSubscriber<RestartLevelSignal>();
            _loadSub = new FakeSubscriber<LoadLevelSignal>();
            _gameCompletedPub = new FakePublisher<GameCompletedSignal>();
            _sceneLoadedPub = new FakePublisher<SceneLoadedSignal>();
            _progressionPub = new FakePublisher<LevelProgressionChangedSignal>();
            _transitionDirector = new FakeSceneTransitionDirector();

            var config = new LevelConfigSO();
            // ... populate config.allLevels with test data ...

            _sut = new LevelProgressionManager(
                config,
                _transitionDirector,
                _completeSub,
                _restartSub,
                _loadSub,
                _gameCompletedPub,
                _sceneLoadedPub,
                _progressionPub);
        }

        [Test]
        public void UpdateState_StoresValues()
        {
            _sut.UpdateState(42, 75f);

            Assert.That(_sut.CoinsCollected, Is.EqualTo(42));
            Assert.That(_sut.CurrentHealth, Is.EqualTo(75f));
        }

        [Test]
        public void CompleteLevel_ResetsCoinsAndHealth()
        {
            _sut.UpdateState(100, 50f);
            _sut.CompleteLevel();

            Assert.That(_sut.CoinsCollected, Is.EqualTo(0));
            Assert.That(_sut.CurrentHealth, Is.EqualTo(0f));
        }

        [Test]
        public void StartLevel_SetsIndexAndLoadsFirstScene()
        {
            _ = _sut.StartLevel(0);

            Assert.That(_sut.CurrentLevelIndex, Is.EqualTo(0));
            Assert.That(_sut.CurrentSceneInLevelIndex, Is.EqualTo(0));
        }
    }
}   