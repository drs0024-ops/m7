using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Core.Enums;
using Game.Core.Interfaces;
using Game.Core.Messages;
using Game.Gameplay.Save;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap
{
    /// <summary>
    /// Message-driven game flow orchestrator.
    /// Reacts to StartGame/Pause/Resume/Load/Died/Climax/VideoFinished messages.
    /// Uses ExecuteTransition DRY pattern with last-known-good recovery.
    /// </summary>
    public class GameFlowSystem : IStartable, IDisposable
    {
        #region Dependencies

        private readonly GameStateMachine _stateMachine;
        private readonly GamePhaseController _phaseController;
        private readonly IInputSwitcher _inputSwitcher;
        private readonly GameFlowConfig _config;
        private readonly LevelLoader _levelLoader;
        private readonly SaveManager _saveManager;
        private readonly SceneRegistry _sceneRegistry;
        private readonly CutsceneSelector _cutsceneSelector;
        private readonly CutscenePlayer _cutscenePlayer;

        private IPublisher<NavigateToMenu> _navigateToMenuPublisher;
        private ISubscriber<StartGameRequested> _startSub;
        private ISubscriber<PauseGameRequested> _pauseSub;
        private ISubscriber<ResumeGameRequested> _resumeSub;
        private ISubscriber<LoadRequest> _loadSub;
        private ISubscriber<VideoFinished> _videoFinishedSub;
        private ISubscriber<PlayerDied> _diedSub;
        private ISubscriber<ClimaxTriggered> _climaxSub;
        private ISubscriber<ClimaxCompleted> _climaxCompletedSub;

        #endregion

        #region State

        private readonly List<IDisposable> _subscriptions = new();
        private CancellationTokenSource _cts;
        private bool _disposed;

        #endregion

        #region Construction

        [Inject]
        public GameFlowSystem(
            GameStateMachine stateMachine,
            GamePhaseController phaseController,
            IInputSwitcher inputSwitcher,
            GameFlowConfig config,
            LevelLoader levelLoader,
            SaveManager saveManager,
            SceneRegistry sceneRegistry,
            CutsceneSelector cutsceneSelector,
            CutscenePlayer cutscenePlayer)
        {
            _stateMachine = stateMachine;
            _phaseController = phaseController;
            _inputSwitcher = inputSwitcher;
            _config = config;
            _levelLoader = levelLoader;
            _saveManager = saveManager;
            _sceneRegistry = sceneRegistry;
            _cutsceneSelector = cutsceneSelector;
            _cutscenePlayer = cutscenePlayer;
        }

        #endregion

        #region IStartable

        void IStartable.Start()
        {
            _cts = new CancellationTokenSource();

            _navigateToMenuPublisher = GlobalMessagePipe.GetPublisher<NavigateToMenu>();
            _startSub = GlobalMessagePipe.GetSubscriber<StartGameRequested>();
            _pauseSub = GlobalMessagePipe.GetSubscriber<PauseGameRequested>();
            _resumeSub = GlobalMessagePipe.GetSubscriber<ResumeGameRequested>();
            _loadSub = GlobalMessagePipe.GetSubscriber<LoadRequest>();
            _videoFinishedSub = GlobalMessagePipe.GetSubscriber<VideoFinished>();
            _diedSub = GlobalMessagePipe.GetSubscriber<PlayerDied>();
            _climaxSub = GlobalMessagePipe.GetSubscriber<ClimaxTriggered>();
            _climaxCompletedSub = GlobalMessagePipe.GetSubscriber<ClimaxCompleted>();

            _subscriptions.Add(_startSub.Subscribe(_ => OnStartGameAsync().Forget()));
            _subscriptions.Add(_pauseSub.Subscribe(_ => OnPause()));
            _subscriptions.Add(_resumeSub.Subscribe(_ => OnResume()));
            _subscriptions.Add(_loadSub.Subscribe(_ => OnLoadAsync().Forget()));
            _subscriptions.Add(_videoFinishedSub.Subscribe(_ => OnVideoFinished()));
            _subscriptions.Add(_diedSub.Subscribe(_ => OnPlayerDied()));
            _subscriptions.Add(_climaxSub.Subscribe(_ => OnClimaxTriggered()));
            _subscriptions.Add(_climaxCompletedSub.Subscribe(_ => OnClimaxCompleted()));

            GameState target = _config.initialState;
            if (!_stateMachine.IsInState(target))
                _stateMachine.ForceState(target);

            _inputSwitcher.SwitchTo(target);
            InitialLoadAsync().Forget();
        }

        #endregion

        #region Message Handlers

        private async UniTask OnStartGameAsync()
        {
            await ExecuteTransition(GameState.Gameplay, GamePhase.Gameplay, async () =>
            {
                await _levelLoader.LoadLevelAsync(_sceneRegistry.FirstLevelScene, null);

                var cutscene = _cutsceneSelector.GetForNewGame();
                if (cutscene != null)
                {
                    _phaseController.SetPhase(GamePhase.Cinematic);
                    await _cutscenePlayer.PlayAsync(cutscene);
                }
            });
        }

        private void OnPause()
        {
            if (_disposed) return;
            if (!_stateMachine.IsInState(GameState.Gameplay)) return;

            _stateMachine.TransitionTo(GameState.Paused);
            _phaseController.SetPhase(GamePhase.None);
            _inputSwitcher.SwitchTo(GameState.Paused);
        }

        private void OnResume()
        {
            if (_disposed) return;
            if (!_stateMachine.IsInState(GameState.Paused)) return;

            _stateMachine.TransitionTo(GameState.Gameplay);
            _phaseController.SetPhase(GamePhase.Gameplay);
            _inputSwitcher.SwitchTo(GameState.Gameplay);
        }

        private async UniTask OnLoadAsync()
        {
            string levelName = _saveManager.CurrentLevelName;
            if (string.IsNullOrEmpty(levelName)) return;

            await ExecuteTransition(GameState.Gameplay, GamePhase.Gameplay, async () =>
            {
                await _levelLoader.LoadLevelAsync(levelName, null);

                var cutscene = _cutsceneSelector.GetForLoad(levelName);
                if (cutscene != null)
                {
                    _phaseController.SetPhase(GamePhase.Cinematic);
                    await _cutscenePlayer.PlayAsync(cutscene);
                }
            });
        }

        private void OnVideoFinished()
        {
            ExecuteTransition(GameState.MainMenu, GamePhase.Menu, () => UniTask.CompletedTask).Forget();
        }

        private void OnPlayerDied()
        {
            if (_disposed) return;
            if (!_stateMachine.IsInState(GameState.Gameplay)) return;

            _stateMachine.TransitionTo(GameState.GameOver);
            _phaseController.SetPhase(GamePhase.None);
            _inputSwitcher.SwitchTo(GameState.GameOver);
        }

        private void OnClimaxTriggered()
        {
            if (_disposed) return;
            _phaseController.SetPhase(GamePhase.Climax);
        }

        private void OnClimaxCompleted()
        {
            if (_disposed) return;
            _phaseController.SetPhase(GamePhase.Ending);
        }

        #endregion

        #region Core Logic

        private async UniTask InitialLoadAsync()
        {
            await UniTask.DelayFrame(1, cancellationToken: _cts.Token);
            if (_disposed) return;

            if (_config.initialState == GameState.MainMenu)
                _navigateToMenuPublisher.Publish(new NavigateToMenu());
        }

        /// <summary>
        /// DRY transition pattern: guard → Loading → await action → success/failure.
        /// On failure, recovers to the last known good state (or MainMenu as fallback).
        /// </summary>
        private async UniTask ExecuteTransition(
            GameState targetState,
            GamePhase targetPhase,
            Func<UniTask> loadAction)
        {
            if (!_stateMachine.CanTransitionTo(GameState.Loading)) return;

            GameState previousState = _stateMachine.CurrentState;
            _stateMachine.TransitionTo(GameState.Loading);

            try
            {
                await loadAction();
                if (_disposed) return;

                _stateMachine.TransitionTo(targetState);
                _phaseController.SetPhase(targetPhase);
                _inputSwitcher.SwitchTo(targetState);
            }
            catch (OperationCanceledException) when (_cts.IsCancellationRequested)
            {
                // Normal shutdown
            }
            catch (Exception e)
            {
                GameState recoveryState = previousState switch
                {
                    GameState.Gameplay => GameState.MainMenu,
                    GameState.Paused   => GameState.MainMenu,
                    GameState.QuitGame => GameState.MainMenu,
                    _                  => previousState
                };

                Debug.LogError(
                    $"[GameFlowRecovery] Transition → {targetState} failed: {e}\n" +
                    $"  Previous state: {previousState}\n" +
                    $"  Current state:  {_stateMachine.CurrentState}\n" +
                    $"  Recovering to:  {recoveryState}");

                _stateMachine.ForceState(recoveryState);
                _phaseController.SetPhase(GamePhase.None);
                _inputSwitcher.SwitchTo(recoveryState);
            }
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var sub in _subscriptions)
                sub?.Dispose();
            _subscriptions.Clear();

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        #endregion
    }
}   