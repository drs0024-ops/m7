using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Game.Core;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine.SceneManagement;

namespace Game.Bootstrap
{
    /// <summary>
    /// Central scene loading/unloading/activation service.
    /// Owns the scene lifecycle: additive load, activate, unload, dedup.
    /// Does NOT decide WHICH scene to load — callers pass the scene name.
    /// </summary>
    public class SceneLoaderService : IStartable, IDisposable
    {
        #region Dependencies
        private const int SCENE_LOAD_BUDGET_MS = 300;

        private readonly SceneRegistry _sceneRegistry;
        private readonly LifetimeScope _currentScope;

        private IPublisher<SceneLoaded> _loadedPublisher;
        private IPublisher<SceneUnloaded> _unloadedPublisher;
        private IPublisher<SceneUnloadFailed> _unloadFailedPublisher;
        private IPublisher<SceneActivated> _activatedPublisher;
        private IPublisher<SceneLoadProgress> _progressPublisher;   

        private readonly Dictionary<string, Scene> _loadedScenes = new(8);
        private readonly List<IDisposable> _subscriptions = new();
        private bool _disposed;

        #endregion

        #region Construction

        [Inject]
        public SceneLoaderService(SceneRegistry sceneRegistry, LifetimeScope currentScope)
        {
            _sceneRegistry = sceneRegistry;
            _currentScope = currentScope;
        }

        #endregion

        #region Lifecycle

        void IStartable.Start()
        {
            _loadedPublisher = GlobalMessagePipe.GetPublisher<SceneLoaded>();
            _unloadedPublisher = GlobalMessagePipe.GetPublisher<SceneUnloaded>();
            _unloadFailedPublisher = GlobalMessagePipe.GetPublisher<SceneUnloadFailed>();
            _activatedPublisher = GlobalMessagePipe.GetPublisher<SceneActivated>();
            _progressPublisher = GlobalMessagePipe.GetPublisher<SceneLoadProgress>();   

        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var sub in _subscriptions)
                sub?.Dispose();
            _subscriptions.Clear();

            foreach (var scene in _loadedScenes.Values)
                if (scene.isLoaded)
                    SceneManager.UnloadSceneAsync(scene);
            _loadedScenes.Clear();
        }

        #endregion

        #region Public API

        public async UniTask EnsureLoadedAndActivate(string sceneName)
        {
            if (!IsSceneLoaded(sceneName))
                await LoadSceneAdditiveInternal(sceneName);
            ActivateScene(sceneName);
        }

        /// <summary>
        /// Loads and activates the main menu, then unloads the source scene if provided.
        /// </summary>
        public async UniTask NavigateToMenuAsync(string sourceSceneName)
        {
            if (_disposed) return;

            await EnsureLoadedAndActivate(_sceneRegistry.MainMenuScene);

            if (!string.IsNullOrEmpty(sourceSceneName))
                await UnloadSceneAsync(sourceSceneName);
        }

        public async UniTask SwapScenesAsync(string targetSceneName, string sourceSceneName)
        {
            if (_disposed) return;

            if (!IsSceneLoaded(targetSceneName))
                await LoadSceneAdditiveInternal(targetSceneName);

            var target = SceneManager.GetSceneByName(targetSceneName);
            if (!target.IsValid() || !target.isLoaded)
            {
                Debug.LogError($"[SceneLoader] Failed to activate target: '{targetSceneName}'");
                return;
            }

            SceneManager.SetActiveScene(target);

            if (!string.IsNullOrEmpty(sourceSceneName))
                await UnloadSceneAsync(sourceSceneName);
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

        public async UniTask UnloadSceneAsync(string sceneName)
        {
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

            if (SceneManager.GetActiveScene() == sceneToUnload)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene s = SceneManager.GetSceneAt(i);
                    if (s != sceneToUnload && s.IsValid() && s.isLoaded)
                    {
                        SceneManager.SetActiveScene(s);
                        break;
                    }
                }
            }

            var op = SceneManager.UnloadSceneAsync(sceneToUnload);
            while (!op.isDone) await UniTask.Yield();

            _loadedScenes.Remove(sceneName);

            if (sceneToUnload.isLoaded)
            {
                var roots = sceneToUnload.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                    if (roots[i] != null) UnityEngine.Object.Destroy(roots[i]);

                _unloadFailedPublisher.Publish(new SceneUnloadFailed(sceneName, "Still loaded after async op"));
            }
            else
            {
                _unloadedPublisher.Publish(new SceneUnloaded(sceneName));
            }
        }

        #endregion

        #region Internal

        private async UniTask LoadSceneAdditiveInternal(string sceneName)
        {
            if (_disposed) return;

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
            var sw = System.Diagnostics.Stopwatch.StartNew();
        #endif

            int indexBefore = SceneManager.sceneCount;

            using (LifetimeScope.EnqueueParent(_currentScope))
            {
                var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (op == null) throw new Exception($"Scene '{sceneName}' not found in Build Settings.");

                op.allowSceneActivation = false;

                while (op.progress < 0.9f)
                {
                    _progressPublisher.Publish(new SceneLoadProgress(op.progress / 0.9f));
                    await UniTask.Yield();
                }

                op.allowSceneActivation = true;
                while (!op.isDone)
                    await UniTask.Yield();
            }

            if (_disposed) return;

            Scene loaded = SceneManager.GetSceneAt(indexBefore);
            _loadedScenes[sceneName] = loaded;
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