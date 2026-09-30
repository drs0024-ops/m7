using System;
using System.Collections.Generic;
using System.Threading;
using MessagePipe;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Core;
using Game.Core.Enums;
using Game.Core.Messages;
using VContainer.Unity;
using Cysharp.Threading.Tasks;

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

        private readonly ISubscriber<GameStateChanged> _stateChangedSub;
        private readonly ISubscriber<SaveRequest> _saveRequestSub;
        private readonly ISubscriber<SaveCompleted> _saveCompletedSub;
        private readonly ISubscriber<NavigateToMenu> _navigateToMenuSub;
        private readonly IPublisher<SaveRequest> _saveRequestPublisher;
        private readonly GameStateMachine _stateMachine;
        private readonly SceneRegistry _sceneRegistry;
        private readonly SceneLoaderService _loader;
        private readonly SceneTransitionOrchestrator _orchestrator;

        #endregion

        #region State

        private readonly List<IDisposable> _subscriptions = new(4);
        private CancellationTokenSource _cts;
        private CancellationTokenSource _saveTimeoutCts;

        private bool _isTransitioning;
        private bool _isSaveComplete;
        private bool _disposed;

        #endregion

        #region Construction

        // FIX #66: Removed unused IPublisher<SceneLoadRequested> dependency.
        public GameStateSceneRouter(
            GameStateMachine stateMachine,
            SceneRegistry sceneRegistry,
            SceneLoaderService loader,
            SceneTransitionOrchestrator orchestrator,
            ISubscriber<GameStateChanged> stateChangedSub,
            ISubscriber<SaveRequest> saveRequestSub,
            ISubscriber<SaveCompleted> saveCompletedSub,
            ISubscriber<NavigateToMenu> navigateToMenuSub,
            IPublisher<SaveRequest> saveRequestPublisher)
        {
            _stateMachine = stateMachine;
            _sceneRegistry = sceneRegistry;
            _loader = loader;
            _orchestrator = orchestrator;
            _stateChangedSub = stateChangedSub;
            _saveRequestSub = saveRequestSub;
            _saveCompletedSub = saveCompletedSub;
            _navigateToMenuSub = navigateToMenuSub;
            _saveRequestPublisher = saveRequestPublisher;
        }

        #endregion

        #region Lifecycle

        void IStartable.Start()
        {
            _cts = new CancellationTokenSource();

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

            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i].Dispose();
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
                    QuitApplication();
                    break;
            }
        }

        // FIX #65: Editor-safe quit
        private static void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Navigation Handlers

        private void OnNavigateToMenu(NavigateToMenu msg)
        {
            if (_stateMachine.CurrentState == GameState.MainMenu)
                return;

            _stateMachine.TransitionTo(GameState.MainMenu);
        }

        #endregion

        #region Save Coordination

        private void OnSaveRequested(SaveRequest msg)
        {
            // FIX #63: Do NOT reset _isSaveComplete here.
            // The caller (HandleMainMenuAsync) sets it to false BEFORE publishing,
            // eliminating the re-entrancy race.
            _saveTimeoutCts?.Cancel();
            _saveTimeoutCts?.Dispose();
            _saveTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        }

        private void OnSaveCompleted(SaveCompleted msg)
        {
            _isSaveComplete = true;
        }

        #endregion

        #region Transition Logic

        private async UniTask HandleMainMenuAsync(CancellationToken token)
        {
            if (_isTransitioning) return;

            if (SceneManager.GetActiveScene().name == _sceneRegistry.MainMenuScene) return;

            _isTransitioning = true;
            _isSaveComplete = false;  // FIX #63: Set here, not in OnSaveRequested

            try
            {
                string activeScene = SceneManager.GetActiveScene().name;
                // FIX #67: Use registry instead of hardcoded "Bootstrap"
                bool isFromGameplay = activeScene != _sceneRegistry.MainMenuScene
                                   && activeScene != _sceneRegistry.PersistentScene;

                if (isFromGameplay)
                {
                    // Save with timeout — timeout does NOT block the transition
                    _saveRequestPublisher.Publish(new SaveRequest("SceneRouter", true));
                    try
                    {
                        await UniTask.WaitUntil(() => _isSaveComplete)
                            .AttachExternalCancellation(_saveTimeoutCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.LogWarning("[SceneRouter] Save timed out, proceeding without save.");
                    }
                }

                token.ThrowIfCancellationRequested();

                if (isFromGameplay)
                {
                    await _orchestrator.TransitionAsync(
                        sceneName: _sceneRegistry.MainMenuScene,
                        onComplete: null,
                        spawnAtDoor: false,
                        door: DoorToSpawnAt.None,
                        fromRight: false,
                        sourceSceneToUnload: activeScene);
                }
                else
                {
                    await _loader.EnsureLoadedAndActivate(_sceneRegistry.MainMenuScene);
                }
            }
            catch (OperationCanceledException)
            {
                // Scope disposed — abort
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
            // FIX #64: Guard against re-entrant transitions
            if (_isTransitioning) return;

            _isTransitioning = true;
            try
            {
                await _orchestrator.TransitionAsync(
                    sceneName: _sceneRegistry.IntroScene,
                    onComplete: null,
                    spawnAtDoor: false,
                    door: DoorToSpawnAt.None,
                    fromRight: false,
                    sourceSceneToUnload: _sceneRegistry.MainMenuScene);
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        #endregion
    }
}   