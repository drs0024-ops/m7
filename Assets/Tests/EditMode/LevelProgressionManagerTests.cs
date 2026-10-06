using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Game.Bootstrap;
using Game.Core.Messages;
using Game.Gameplay.Level;
using Game.Core;
using Game.Core.Data;

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
        private LevelConfigSO _config;

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

            _config = ScriptableObject.CreateInstance<LevelConfigSO>();
            _config.allLevels = new List<LevelProgressionData>
            {
                new LevelProgressionData
                {
                    levelName = "Level1",
                    scenes = new List<SceneField>
                    {
                        new SceneField("Facility"),
                        new SceneField("Level1_Scene2")
                    }
                },
                new LevelProgressionData
                {
                    levelName = "Level2",
                    scenes = new List<SceneField>
                    {
                        new SceneField("Level2_Scene1")
                    }
                }
            };
            _config.startingLevelIndex = 0;

            _sut = new LevelProgressionManager(
                _config,
                _transitionDirector,
                _completeSub,
                _restartSub,
                _loadSub,
                _gameCompletedPub,
                _sceneLoadedPub,
                _progressionPub,
                new SaveableRegistry());
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_config);
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
        public void GetSaveData_AfterStartLevel_ContainsLevelProgression()
        {
            _sut.StartLevel(1).Forget();

            var saveData = (LevelProgressionSaveData)_sut.GetSaveData();

            Assert.That(saveData.SaveId, Is.EqualTo("LevelProgression"));
            Assert.That(saveData.levelName, Is.EqualTo("Level2"));
            Assert.That(saveData.scenes, Has.Count.EqualTo(1));
            Assert.That(saveData.scenes[0].SceneName, Is.EqualTo("Level2_Scene1"));
        }

        [Test]
        public void StartLevel_SetsIndexAndLoadsFirstScene()
        {
            _ = _sut.StartLevel(0);

            Assert.That(_sut.CurrentLevelIndex, Is.EqualTo(0));
            Assert.That(_sut.CurrentSceneInLevelIndex, Is.EqualTo(0));
            Assert.That(_transitionDirector.LastLoadedScene, Is.EqualTo("Facility"));
        }

        [Test]
        public void StartLevel_InvalidIndex_DoesNothing()
        {
            _sut.StartLevel(99).Forget();

            Assert.That(_sut.CurrentLevelIndex, Is.EqualTo(0));
            Assert.That(_transitionDirector.LastLoadedScene, Is.Null);
        }

        [Test]
        public void LoadNextScene_AdvancesWithinLevel()
        {
            _sut.StartLevel(0).Forget();
            _sut.LoadNextScene().Forget();

            Assert.That(_sut.CurrentSceneInLevelIndex, Is.EqualTo(1));
            Assert.That(_transitionDirector.LastLoadedScene, Is.EqualTo("Level1_Scene2"));
        }

        [Test]
        public void LoadNextScene_AdvancesToNextLevel()
        {
            _sut.StartLevel(0).Forget();
            _sut.LoadNextScene().Forget(); // → Level1_Scene2
            _sut.LoadNextScene().Forget(); // → Level2_Scene1 (new level)

            Assert.That(_sut.CurrentLevelIndex, Is.EqualTo(1));
            Assert.That(_sut.CurrentSceneInLevelIndex, Is.EqualTo(0));
            Assert.That(_transitionDirector.LastLoadedScene, Is.EqualTo("Level2_Scene1"));
        }

        [Test]
        public void LoadNextScene_PublishesGameCompleted_OnLastScene()
        {
            _sut.StartLevel(1).Forget(); // Level2 (only 1 scene)
            _sut.LoadNextScene().Forget(); // no next scene → GameCompleted

            Assert.That(_gameCompletedPub.Messages.Count, Is.EqualTo(1));
        }
    }
}   