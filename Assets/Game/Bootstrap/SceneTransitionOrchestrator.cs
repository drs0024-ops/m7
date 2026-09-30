using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Core.Enums;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;

namespace Game.Bootstrap
{
    /// <summary>
    /// Orchestrates the full scene transition sequence:
    /// fade out → load/activate → unload old → cleanup → fade in.
    /// Publishes SceneTransitionStarted / SceneTransitionCompleted.
    /// </summary>
    public class SceneTransitionOrchestrator : IDisposable
    {
        #region Dependencies

        private const int TRANSITION_BUDGET_MS = 2000;

        private readonly IPublisher<SceneTransitionStarted> _startedPublisher;
        private readonly IPublisher<SceneTransitionCompleted> _completedPublisher;
        private readonly SceneLoaderService _loader;
        private readonly SceneFadeManager _fade;
        private readonly SceneCleanupService _cleanup;
        private readonly SceneRegistry _sceneRegistry;

        #endregion

        #region State

        private bool _isTransitioning;
        private bool _disposed;

        #endregion

        #region Construction

        public SceneTransitionOrchestrator(
            SceneLoaderService loader,
            SceneFadeManager fade,
            SceneCleanupService cleanup,
            SceneRegistry sceneRegistry,
            IPublisher<SceneTransitionStarted> startedPublisher,
            IPublisher<SceneTransitionCompleted> completedPublisher)
        {
            _loader = loader;
            _fade = fade;
            _cleanup = cleanup;
            _sceneRegistry = sceneRegistry;
            _startedPublisher = startedPublisher;
            _completedPublisher = completedPublisher;
        }

        #endregion

        #region Lifecycle

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }

        #endregion

        #region Public API

        public async UniTask TransitionAsync(
            string sceneName,
            Action onComplete,
            bool spawnAtDoor,
            DoorToSpawnAt door,
            bool fromRight,
            string sourceSceneToUnload = null,
            LevelStateSnapshot? snapshot = null)
        {
            if (_disposed) return;

            // FIX #52: Log dropped transitions instead of silently ignoring
            if (_isTransitioning)
            {
                Debug.LogWarning($"[Orchestrator] Transition to '{sceneName}' dropped: already transitioning.");
                return;
            }

            _isTransitioning = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string source = string.IsNullOrEmpty(sourceSceneToUnload) ? "(none)" : sourceSceneToUnload;
#endif

            // FIX #49: Per-transition CTS instead of a field-level "poisoned" CTS
            using var cts = new CancellationTokenSource();
            var token = cts.Token;

            _startedPublisher.Publish(new SceneTransitionStarted(sceneName));

            bool fadedOut = false;

            try
            {
                await _fade.FadeOutAsync(token);
                fadedOut = true;
                token.ThrowIfCancellationRequested();

                await _loader.EnsureLoadedAndActivate(sceneName, snapshot, token);

                if (!string.IsNullOrEmpty(sourceSceneToUnload))
                    await _loader.UnloadSceneAsync(sourceSceneToUnload, token);

                token.ThrowIfCancellationRequested();

                // FIX #47: Use registry instead of hardcoded "Bootstrap"
                await _cleanup.UnloadObsoleteScenesAsync(
                    sceneName,
                    persistentSceneName: _sceneRegistry.PersistentScene,
                    unloadMainMenu: false,
                    token);

                token.ThrowIfCancellationRequested();

                // FIX #46: Yield on UnloadUnusedAssets instead of fire-and-forget
                var asyncOp = Resources.UnloadUnusedAssets();
                while (!asyncOp.isDone)
                    await UniTask.Yield();

                token.ThrowIfCancellationRequested();

                await _fade.FadeInAsync(token);
                fadedOut = false;

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
                if (fadedOut)
                {
                    try { await _fade.FadeInAsync(CancellationToken.None); }
                    catch { /* best effort */ }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Orchestrator] Transition to '{sceneName}' failed: {e}");
                if (fadedOut)
                {
                    try { await _fade.FadeInAsync(CancellationToken.None); }
                    catch { /* best effort */ }
                }
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        #endregion
    }
}   