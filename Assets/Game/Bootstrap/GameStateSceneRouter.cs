using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using Game.Core;
using Game.Core.Enums;
using Game.Core.Messages;
using VContainer.Unity;

namespace Game.Bootstrap
{
    /// <summary>
    /// Executes the scene side of game flow: reacts to GameStateChanged messages
    /// and routes the appropriate scene load/unload/transition.
    /// Coordinates saves before returning to menu from gameplay.
    /// Does NOT decide state transitions — that is GameFlowSystem's job.
    /// </summary>
    public class GameStateSceneRouter : IStartable, IDisposable
    {
        #region Dependencies

        private ISubscriber<GameStateChanged> _stateChangedSub;
        private ISubscriber<SaveRequest> _saveRequestSub;
        private ISubscriber<SaveCompleted> _saveCompletedSub;
        private ISubscriber<NavigateToMenu> _navigateToMenuSub;
        private IPublisher<SaveRequest> _saveRequestPublisher;
        private IPublisher<SceneLoadRequested> _sceneLoadPublisher;

        private readonly GameStateMachine _stateMachine;
        private readonly SceneRegistry _sceneRegistry;
        private readonly SceneLoaderService _loader;
        private readonly SceneTransitionOrchestrator _orchestrator;

        private readonly List<IDisposable> _subscriptions = new(4);
        private CancellationTokenSource _cts;
        private CancellationTokenSource _saveTimeoutCts;

        private bool _isTransitioning;
        private bool _isSaveComplete;
        private bool _disposed;

        #endregion

        #region Construction

        [Inject]
        public GameStateSceneRouter(
            GameStateMachine stateMachine,
            SceneRegistry sceneRegistry,
            SceneLoaderService loader,
            SceneTransitionOrchestrator orchestrator)
        {
            _stateMachine = stateMachine;
            _sceneRegistry = sceneRegistry;
            _loader = loader;
            _orchestrator = orchestrator;
        }

        #endregion

        #region Lifecycle

        void IStartable.Start()
        {
            _cts = new CancellationTokenSource();

            _stateChangedSub = GlobalMessagePipe.GetSubscriber<GameStateChanged>();
            _saveRequestSub = GlobalMessagePipe.GetSubscriber<SaveRequest>();
            _saveCompletedSub = GlobalMessagePipe.GetSubscriber<SaveCompleted>();
            _navigateToMenuSub = GlobalMessagePipe.GetSubscriber<NavigateToMenu>();
            _saveRequestPublisher = GlobalMessagePipe.GetPublisher<SaveRequest>();
            _sceneLoadPublisher = GlobalMessagePipe.GetPublisher<SceneLoadRequested>();

            _subscriptions.Add(_stateChangedSub.Subscribe(OnStateChanged));
            _subscriptions.Add(_saveRequestSub.Subscribe(OnSaveRequested));
            _subscriptions.Add(_saveCompletedSub.Subscribe(OnSaveCompleted));
            _subscriptions.Add(_navigateToMenuSub.Subscribe(OnNavigateToMenu));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _saveTimeoutCts?.Cancel();
            _saveTimeoutCts?.Dispose();
            _saveTimeoutCts = null;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            foreach (var sub in _subscriptions)
                sub?.Dispose();
            _subscriptions.Clear();
        }

        #endregion

        #region State Change Routing

        private void OnStateChanged(GameStateChanged msg)
        {
            switch (msg.State)
            {
                case GameState.MainMenu:
                    HandleMainMenuAsync(_cts.Token).Forget();
                    break;
                case GameState.IntroVideo:
                    HandleIntroAsync(_cts.Token).Forget();
                    break;
                case GameState.QuitGame:
                    Application.Quit();
                    break;
            }
}

        #endregion

        #region Save Coordination

        private void OnSaveRequested(SaveRequest msg)
        {
            _isSaveComplete = false;
            _saveTimeoutCts?.Cancel();
            _saveTimeoutCts?.Dispose();
            _saveTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        }

        private void OnSaveCompleted(SaveCompleted msg)
        {
            _isSaveComplete = true;
        }

        #endregion

        #region Navigation Handlers

        private void OnNavigateToMenu(NavigateToMenu msg)
        {
            if (_stateMachine.CurrentState == GameState.MainMenu)
                return;  // ← Already on MainMenu. The GameStateChanged publish already triggered the load.

            _stateMachine.TransitionTo(GameState.MainMenu);
            // SetState → publishes GameStateChanged(MainMenu) → HandleMainMenuAsync fires
        }

        #endregion

        #region Transition Logic

        private async UniTask HandleMainMenuAsync(CancellationToken token)
        {
            if (_isTransitioning) return;

            // Already on the target scene — nothing to do
            if (SceneManager.GetActiveScene().name == _sceneRegistry.MainMenuScene) return;

            _isTransitioning = true;
            _isSaveComplete = false;

            try
            {
                string activeScene = SceneManager.GetActiveScene().name;
                bool isFromGameplay = activeScene != _sceneRegistry.MainMenuScene
                                   && activeScene != "Bootstrap";

                if (isFromGameplay)
                {
                    _saveRequestPublisher.Publish(new SaveRequest("SceneRouter", true));
                    await UniTask.WaitUntil(() => _isSaveComplete)
                        .AttachExternalCancellation(_saveTimeoutCts.Token);
                }

                token.ThrowIfCancellationRequested();

                if (isFromGameplay)
                {
                    await _orchestrator.TransitionAsync(
                        sceneName: _sceneRegistry.MainMenuScene,
                        onComplete: null,
                        spawnAtDoor: false,
                        door: DoorToSpawnAt.None,
                        fromRight: false);
                }
                else
                {
                    await _loader.EnsureLoadedAndActivate(_sceneRegistry.MainMenuScene);
                }
            }
            catch (OperationCanceledException)
            {
                // Save timeout or scope disposed — proceed without save
            }
            catch (Exception e)
            {
                Debug.LogError($"[SceneRouter] Transition failed: {e.Message}");
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        private async UniTask HandleIntroAsync(CancellationToken token)
        {
            await _orchestrator.TransitionAsync(
                sceneName: _sceneRegistry.IntroScene,
                onComplete: null,
                spawnAtDoor: false,
                door: DoorToSpawnAt.None,
                fromRight: false,
                sourceSceneToUnload: _sceneRegistry.MainMenuScene);
        }

        #endregion
    }
}   