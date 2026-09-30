using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Game.Bootstrap
{
    /// <summary>
    /// Unloads all active scenes except the target, persistent, and optionally the main menu.
    /// Disposes child DI scopes BEFORE scene unload for deterministic teardown.
    /// Called after a scene transition to prevent scene accumulation during additive loading.
    /// </summary>
    public class SceneCleanupService
    {
        private readonly SceneRegistry _registry;
        private readonly IPublisher<SceneUnloading> _unloadingPublisher;
        private readonly string _mainMenuSceneName;

        public SceneCleanupService(
            SceneRegistry registry,
            IPublisher<SceneUnloading> unloadingPublisher)
        {
            _registry = registry;
            _unloadingPublisher = unloadingPublisher;
            _mainMenuSceneName = registry.MainMenuScene;
        }

        public async UniTask UnloadObsoleteScenesAsync(
            string newSceneName,
            string persistentSceneName = null,
            bool unloadMainMenu = true,
            CancellationToken token = default)
        {
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                token.ThrowIfCancellationRequested();

                Scene s = SceneManager.GetSceneAt(i);
                if (!s.IsValid()) continue;
                if (s.name == newSceneName) continue;
                if (!string.IsNullOrEmpty(persistentSceneName) && s.name == persistentSceneName) continue;
                if (!unloadMainMenu && s.name == _mainMenuSceneName) continue;

                // Guard: never unload the active scene
                if (SceneManager.GetActiveScene() == s)
                {
                    Debug.LogError($"[SceneCleanup] Skipping active scene '{s.name}' — cannot unload without re-activating another scene first.");
                    continue;
                }

                Debug.Log($"[SceneCleanup] Unloading: '{s.name}'");

                // Pre-notify root-scope systems
                _unloadingPublisher.Publish(new SceneUnloading(s.name));

                // Deterministic scope disposal before Unity destroys objects
                var scope = _registry.UnregisterScope(s.name);
                scope?.Dispose();   

                var op = SceneManager.UnloadSceneAsync(s);
                await UniTask.WaitUntil(() => op.isDone, cancellationToken: token);
            }
        }
    }
}   