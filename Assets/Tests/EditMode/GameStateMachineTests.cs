using System;
using System.Reflection;
using Game.Bootstrap;
using Game.Core.Enums;
using Game.Core.Messages;
using MessagePipe;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>
    /// Edit-mode unit tests for the GameStateMachine transition table.
    /// Uses reflection to inject no-op publishers, bypassing MessagePipe/VContainer entirely.
    /// </summary>
    [TestFixture]
    public class GameStateMachineTests
    {
        private GameStateMachine _sut;

        [SetUp]
        public void SetUp()
        {
            _sut = new GameStateMachine(
                new NoOpPublisher<TimeScalePause>(),
                new NoOpPublisher<TimeScaleResume>(),
                new NoOpPublisher<GameStateChanged>(),
                new NoOpPublisher<GameStarted>(),
                new NoOpPublisher<GamePaused>(),
                new NoOpPublisher<GameResumed>());
        }

        #region Valid Transitions

        [Test]
        public void TransitionTo_MainMenuToLoading_Succeeds()
        {
            Assert.That(_sut.CurrentState, Is.EqualTo(GameState.MainMenu));
            Assert.That(_sut.TransitionTo(GameState.Loading), Is.True);
            Assert.That(_sut.CurrentState, Is.EqualTo(GameState.Loading));
        }

        [Test]
        public void TransitionTo_LoadingToGameplay_Succeeds()
        {
            _sut.ForceState(GameState.Loading);
            Assert.That(_sut.TransitionTo(GameState.Gameplay), Is.True);
            Assert.That(_sut.CurrentState, Is.EqualTo(GameState.Gameplay));
        }

        [Test]
        public void TransitionTo_GameplayToPaused_Succeeds()
        {
            _sut.ForceState(GameState.Gameplay);
            Assert.That(_sut.TransitionTo(GameState.Paused), Is.True);
            Assert.That(_sut.CurrentState, Is.EqualTo(GameState.Paused));
        }

        [Test]
        public void TransitionTo_PausedToGameplay_Resumes()
        {
            _sut.ForceState(GameState.Paused);
            Assert.That(_sut.TransitionTo(GameState.Gameplay), Is.True);
            Assert.That(_sut.CurrentState, Is.EqualTo(GameState.Gameplay));
        }

        [Test]
        public void TransitionTo_GameplayToGameOver_Succeeds()
        {
            _sut.ForceState(GameState.Gameplay);
            Assert.That(_sut.TransitionTo(GameState.GameOver), Is.True);
            Assert.That(_sut.CurrentState, Is.EqualTo(GameState.GameOver));
        }

        [Test]
        public void TransitionTo_GameOverToMainMenu_Succeeds()
        {
            _sut.ForceState(GameState.GameOver);
            Assert.That(_sut.TransitionTo(GameState.MainMenu), Is.True);
            Assert.That(_sut.CurrentState, Is.EqualTo(GameState.MainMenu));
        }

        [Test]
        public void TransitionTo_MainMenuToIntroVideo_Succeeds()
        {
            Assert.That(_sut.CurrentState, Is.EqualTo(GameState.MainMenu));
            Assert.That(_sut.TransitionTo(GameState.IntroVideo), Is.True);
            Assert.That(_sut.CurrentState, Is.EqualTo(GameState.IntroVideo));
        }

        [Test]
        public void TransitionTo_IntroVideoToLoading_Succeeds()
        {
            _sut.ForceState(GameState.IntroVideo);
            Assert.That(_sut.TransitionTo(GameState.Loading), Is.True);
            Assert.That(_sut.CurrentState, Is.EqualTo(GameState.Loading));
        }

        [Test]
        public void TransitionTo_PausedToMainMenu_Succeeds()
        {
            _sut.ForceState(GameState.Paused);
            Assert.That(_sut.TransitionTo(GameState.MainMenu), Is.True);
            Assert.That(_sut.CurrentState, Is.EqualTo(GameState.MainMenu));
        }

        #endregion

        #region Invalid Transitions

        [Test]
        public void TransitionTo_MainMenuToPaused_Rejected()
        {
            Assert.That(_sut.CurrentState, Is.EqualTo(GameState.MainMenu));
            Assert.That(_sut.TransitionTo(GameState.Paused), Is.False);
            Assert.That(_sut.CurrentState, Is.EqualTo(GameState.MainMenu));
        }

        [Test]
        public void TransitionTo_GameOverToGameplay_Rejected()
        {
            _sut.ForceState(GameState.GameOver);
            Assert.That(_sut.TransitionTo(GameState.Gameplay), Is.False);
            Assert.That(_sut.CurrentState, Is.EqualTo(GameState.GameOver));
        }

        [Test]
        public void TransitionTo_QuitGameToMainMenu_Rejected()
        {
            _sut.ForceState(GameState.QuitGame);
            Assert.That(_sut.TransitionTo(GameState.MainMenu), Is.False);
            Assert.That(_sut.CurrentState, Is.EqualTo(GameState.QuitGame));
        }

        #endregion

        #region CanTransitionTo

        [Test]
        public void CanTransitionTo_ValidPair_ReturnsTrue()
        {
            Assert.That(_sut.CurrentState, Is.EqualTo(GameState.MainMenu));
            Assert.That(_sut.CanTransitionTo(GameState.Loading), Is.True);
        }

        [Test]
        public void CanTransitionTo_InvalidPair_ReturnsFalse()
        {
            Assert.That(_sut.CurrentState, Is.EqualTo(GameState.MainMenu));
            Assert.That(_sut.CanTransitionTo(GameState.Paused), Is.False);
        }

        #endregion

        #region ForceState

        [Test]
        public void ForceState_BypassesValidation()
        {
            Assert.That(_sut.CurrentState, Is.EqualTo(GameState.MainMenu));
            _sut.ForceState(GameState.Paused);
            Assert.That(_sut.CurrentState, Is.EqualTo(GameState.Paused));
        }

        #endregion

        #region Helpers

        private  void InjectNoOpPublishers()
        {
            var fields = typeof(GameStateMachine).GetFields(
                BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (!field.FieldType.IsGenericType) continue;
                if (field.FieldType.GetGenericTypeDefinition() != typeof(IPublisher<>)) continue;

                var implType = typeof(NoOpPublisher<>).MakeGenericType(
                    field.FieldType.GetGenericArguments());
                field.SetValue(_sut, Activator.CreateInstance(implType));
            }
        }

        private class NoOpPublisher<T> : IPublisher<T>
        {
            public void Publish(T message) { }
        }

        #endregion
    }
}   