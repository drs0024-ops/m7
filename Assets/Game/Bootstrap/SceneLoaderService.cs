using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;
using Game.Core;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine.SceneManagement;
using VContainer;

namespace Game.Bootstrap
{
    /// <summary>
    /// Manages additive scene load/unload/activate with per-scene DI child scopes.
    /// Publishes SceneLoaded, SceneUnloaded, SceneActivated, SceneLoadProgress, SceneUnloading.
    /// </summary>
    public class SceneLoaderService : IStartable, IDisposable
    {
        #region Dependencies

        private const int SCENE_LOAD_BUDGET_MS = 300;

        private readonly SceneRegistry _sceneRegistry;
        private readonly LifetimeScope _currentScope;
        private readonly IPublisher<SceneLoaded> _loadedPublisher;
        private readonly IPublisher<SceneUnloaded> _unloadedPublisher;
        private readonly IPublisher<SceneUnloading> _unloadingPublisher;
        private readonly IPublisher<SceneActivated> _activatedPublisher;
        private readonly IPublisher<SceneLoadProgress> _progressPublisher;

        #endregion

        #region State

        private readonly Dictionary<string, Scene> _loadedScenes = new(8);
        private bool _disposed;

        #endregion

        #region Construction

        public SceneLoaderService(
            SceneRegistry sceneRegistry,
            LifetimeScope currentScope,
            IPublisher<SceneLoaded> loadedPublisher,
            IPublisher<SceneUnloaded> unloadedPublisher,
            IPublisher<SceneUnloading> unloadingPublisher,
            IPublisher<SceneActivated> activatedPublisher,
            IPublisher<SceneLoadProgress> progressPublisher)
        {
            _sceneRegistry = sceneRegistry;
            _currentScope = currentScope;
            _loadedPublisher = loadedPublisher;
            _unloadedPublisher = unloadedPublisher;
            _unloadingPublisher = unloadingPublisher;
            _activatedPublisher = activatedPublisher;
            _progressPublisher = progressPublisher;
        }

        #endregion

        #region Lifecycle

        void IStartable.Start()
        {
            // No subscriptions needed — all interaction is via direct method calls.
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var kvp in _loadedScenes)
            {
                if (kvp.Value.isLoaded)
                {
                    DisposeScopeForScene(kvp.Key);
                    SceneManager.UnloadSceneAsync(kvp.Value);
                }
            }
            _loadedScenes.Clear();
        }

        #endregion

        #region Public API

        public async UniTask EnsureLoadedAndActivate(string sceneName, LevelStateSnapshot? snapshot = null, CancellationToken token = default)
        {
            if (!IsSceneLoaded(sceneName))
                await LoadSceneAdditiveInternal(sceneName, snapshot, token);
            ActivateScene(sceneName);
        }

        public async UniTask NavigateToMenuAsync(string sourceSceneName, CancellationToken token = default)
        {
            if (_disposed) return;

            await EnsureLoadedAndActivate(_sceneRegistry.MainMenuScene, null, token);

            if (!string.IsNullOrEmpty(sourceSceneName))
                await UnloadSceneAsync(sourceSceneName, token);
        }

        public async UniTask SwapScenesAsync(string targetSceneName, string sourceSceneName, CancellationToken token = default)
        {
            if (_disposed) return;

            if (!IsSceneLoaded(targetSceneName))
                await LoadSceneAdditiveInternal(targetSceneName, null, token);

            var target = SceneManager.GetSceneByName(targetSceneName);
            if (!target.IsValid() || !target.isLoaded)
            {
                Debug.LogError($"[SceneLoader] Failed to activate target: '{targetSceneName}'");
                return;
            }

            SceneManager.SetActiveScene(target);

            if (!string.IsNullOrEmpty(sourceSceneName))
                await UnloadSceneAsync(sourceSceneName, token);
        }

        public bool IsSceneLoaded(string sceneName)
        {
            if (_loadedScenes.TryGetValue(sceneName, out Scene s) && s.isLoaded)
                return true;

            var scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid() && scene.isLoaded)
            {
                _loadedScenes[sceneName] = scene;
                return true;
            }

            return false;
        }

        public bool ActivateScene(string sceneName)
        {
            if (_loadedScenes.TryGetValue(sceneName, out Scene target) && target.isLoaded)
            {
                SceneManager.SetActiveScene(target);
                _activatedPublisher.Publish(new SceneActivated(sceneName));
                return true;
            }

            Debug.LogError($"[SceneLoader] Cannot activate '{sceneName}' (not loaded).");
            return false;
        }

        public async UniTask UnloadSceneAsync(string sceneName, CancellationToken token = default)
        {
            if (_disposed) return;

            Scene sceneToUnload;

            if (_loadedScenes.TryGetValue(sceneName, out Scene tracked) && tracked.isLoaded)
            {
                sceneToUnload = tracked;
            }
            else
            {
                sceneToUnload = SceneManager.GetSceneByName(sceneName);
                if (!sceneToUnload.IsValid() || !sceneToUnload.isLoaded) return;
                _loadedScenes[sceneName] = sceneToUnload;
            }

            // Guard: cannot unload the only loaded scene
            if (SceneManager.sceneCount <= 1)
            {
                Debug.LogError($"[SceneLoader] Cannot unload '{sceneName}': it is the only loaded scene.");
                return;
            }

            // Guard: cannot unload the active scene without re-activating first
            if (SceneManager.GetActiveScene() == sceneToUnload)
            {
                bool reactivated = false;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene s = SceneManager.GetSceneAt(i);
                    if (s != sceneToUnload && s.IsValid() && s.isLoaded)
                    {
                        SceneManager.SetActiveScene(s);
                        reactivated = true;
                        break;
                    }
                }
                if (!reactivated)
                {
                    Debug.LogError($"[SceneLoader] Cannot unload active scene '{sceneName}': no alternative scene to activate.");
                    return;
                }
            }

            // Pre-notify: give root-scope systems a chance to release scene-scoped references
            _unloadingPublisher.Publish(new SceneUnloading(sceneName));

            // Deterministic scope disposal BEFORE Unity destroys objects
            DisposeScopeForScene(sceneName);

            // Now unload the scene (scope is already disposed, objects will be destroyed by Unity)
            var op = SceneManager.UnloadSceneAsync(sceneToUnload);
            while (!op.isDone)
            {
                token.ThrowIfCancellationRequested();
                await UniTask.Yield();
            }

            _loadedScenes.Remove(sceneName);
            _unloadedPublisher.Publish(new SceneUnloaded(sceneName));
        }

        #endregion

        #region Internal

        private void DisposeScopeForScene(string sceneName)
        {
            var scope = _sceneRegistry.UnregisterScope(sceneName);
            scope?.Dispose();
        }

        private static LifetimeScope FindScopeInScene(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var scope = roots[i].GetComponent<LifetimeScope>();
                if (scope != null) return scope;
            }
            return null;
        }

        private async UniTask LoadSceneAdditiveInternal(string sceneName, LevelStateSnapshot? snapshot, CancellationToken token)
        {
            if (_disposed) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var sw = System.Diagnostics.Stopwatch.StartNew();
#endif

            using (LifetimeScope.EnqueueParent(_currentScope))
            using (LifetimeScope.Enqueue(builder =>
            {
                if (snapshot.HasValue)
                    builder.RegisterInstance(snapshot.Value);
            }))
            {
                var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (op == null)
                {
                    Debug.LogError($"[SceneLoader] Scene '{sceneName}' not found in Build Settings.");
                    return;
                }

                op.allowSceneActivation = false;

                while (op.progress < 0.9f)
                {
                    token.ThrowIfCancellationRequested();
                    _progressPublisher.Publish(new SceneLoadProgress(op.progress / 0.9f));
                    await UniTask.Yield();
                }

                op.allowSceneActivation = true;
                while (!op.isDone)
                {
                    token.ThrowIfCancellationRequested();
                    await UniTask.Yield();
                }
            }

            if (_disposed) return;

            Scene loaded = SceneManager.GetSceneByName(sceneName);
            if (!loaded.IsValid() || !loaded.isLoaded)
            {
                Debug.LogError($"[SceneLoader] Scene '{sceneName}' not found after load completed.");
                return;
            }

            _loadedScenes[sceneName] = loaded;

            // Register the child scope for deterministic disposal on unload
            var childScope = FindScopeInScene(loaded);
            if (childScope != null)
            {
                _sceneRegistry.RegisterScope(sceneName, childScope);
            }
            else
            {
                Debug.LogWarning($"[SceneLoader] No LifetimeScope found in scene '{sceneName}'. Scope tracking unavailable for this scene.");
            }

            _loadedPublisher.Publish(new SceneLoaded(sceneName, loaded.handle));

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            sw.Stop();
            long ms = sw.ElapsedMilliseconds;
            Debug.Log($"[Perf] SceneLoad \"{sceneName}\" took {ms}ms");
            if (ms > SCENE_LOAD_BUDGET_MS)
                Debug.LogWarning($"[Perf][WARN] SceneLoad \"{sceneName}\" took {ms}ms (budget: {SCENE_LOAD_BUDGET_MS}ms)");
#endif
        }

        #endregion
    }
}   