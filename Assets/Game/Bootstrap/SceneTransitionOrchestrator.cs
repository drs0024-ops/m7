using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Enums;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap
{
    /// <summary>
    /// Orchestrates the full scene transition sequence: fade out → load/activate → unload source → cleanup → fade in.
    /// Publishes SceneTransitionStarted/Completed for UI and audio to react to.
    /// Does NOT decide WHEN to transition — callers (GameStateSceneRouter) own that decision.
    /// </summary>
    public class SceneTransitionOrchestrator : IStartable, IDisposable
    {
        #region Dependencies
        private const int TRANSITION_BUDGET_MS = 500;
        private IPublisher<SceneTransitionStarted> _startedPublisher;
        private IPublisher<SceneTransitionCompleted> _completedPublisher;

        private readonly SceneLoaderService _loader;
        private readonly SceneFadeManager _fade;
        private readonly SceneCleanupService _cleanup;
        private bool _isTransitioning;

        private CancellationTokenSource _cts;
        private bool _disposed;

        #endregion

        #region Construction

        [Inject]
        public SceneTransitionOrchestrator(
            SceneLoaderService loader,
            SceneFadeManager fade,
            SceneCleanupService cleanup)
        {
            _loader = loader;
            _fade = fade;
            _cleanup = cleanup;
        }

        #endregion

        #region Lifecycle

        void IStartable.Start()
        {
            _cts = new CancellationTokenSource();

            _startedPublisher = GlobalMessagePipe.GetPublisher<SceneTransitionStarted>();
            _completedPublisher = GlobalMessagePipe.GetPublisher<SceneTransitionCompleted>();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        #endregion

        #region Public API

        public async UniTask TransitionAsync(
            string sceneName,
            Action onComplete,
            bool spawnAtDoor,
            DoorToSpawnAt door,
            bool fromRight,
            string sourceSceneToUnload = null)
        {
            if (_disposed || _isTransitioning) return;
            _isTransitioning = true;

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string source = string.IsNullOrEmpty(sourceSceneToUnload) ? "(none)" : sourceSceneToUnload;
        #endif

            var token = _cts.Token;
            _startedPublisher.Publish(new SceneTransitionStarted(sceneName));

            try
            {
                await _fade.FadeOutAsync();
                token.ThrowIfCancellationRequested();

                await _loader.EnsureLoadedAndActivate(sceneName);

                if (!string.IsNullOrEmpty(sourceSceneToUnload))
                    await _loader.UnloadSceneAsync(sourceSceneToUnload);

                token.ThrowIfCancellationRequested();

                await _cleanup.UnloadObsoleteScenesAsync(
                    sceneName, persistentSceneName: "Bootstrap", unloadMainMenu: false);

                token.ThrowIfCancellationRequested();

                _ = Resources.UnloadUnusedAssets();

                token.ThrowIfCancellationRequested();

                await _fade.FadeInAsync();
                onComplete?.Invoke();
                _completedPublisher.Publish(new SceneTransitionCompleted(sceneName));

                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    sw.Stop();
                    long ms = sw.ElapsedMilliseconds;
                    Debug.Log($"[Perf] Transition \"{source}\" → \"{sceneName}\" took {ms}ms");
                    if (ms > TRANSITION_BUDGET_MS)
                        Debug.LogWarning($"[Perf][WARN] Transition \"{source}\" → \"{sceneName}\" took {ms}ms (budget: {TRANSITION_BUDGET_MS}ms)");
                #endif   
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogError($"[Orchestrator] Transition to '{sceneName}' failed: {e.Message}");
            }
            finally
            {
                _isTransitioning = false;
            }
        }   

        #endregion
    }
}   