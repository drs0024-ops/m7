using System;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace Game.Bootstrap
{
    public class OverlayManager : IDisposable
    {
        private readonly SceneLoaderService _loader;
        private readonly string _mainMenuSceneName;
        private readonly IPublisher<SceneLoadRequested> _loadPublisher;
        private readonly IPublisher<SceneUnloadRequested> _unloadPublisher;
        //private readonly IPublisher<OptionsClosed> _optionsClosedPublisher;
        private const int TIMEOUT_MS = 10_000;

        private Scene _previousScene;
        private bool _disposed;

        [Inject]
        public OverlayManager(SceneLoaderService loader, SceneRegistry sceneRegistry)
        {
            _loader = loader;
            _mainMenuSceneName = sceneRegistry.MainMenuScene;
            _loadPublisher = GlobalMessagePipe.GetPublisher<SceneLoadRequested>();
            _unloadPublisher = GlobalMessagePipe.GetPublisher<SceneUnloadRequested>();
            //_optionsClosedPublisher = GlobalMessagePipe.GetPublisher<OptionsClosed>();
        }

        public async UniTask OpenOptionsAsync(string optionsSceneName)
        {
            _previousScene = SceneManager.GetActiveScene();

            var existing = SceneManager.GetSceneByName(optionsSceneName);
            if (existing.IsValid() && existing.isLoaded)
            {
                SceneManager.SetActiveScene(existing);
                return;
            }

            _loadPublisher.Publish(new SceneLoadRequested(optionsSceneName, activateOnLoad: true));

            using var cts = new System.Threading.CancellationTokenSource(10000);
            try
            {
                await UniTask.WaitUntil(
                    () => IsSceneLoaded(optionsSceneName),
                    cancellationToken: cts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.LogError($"[OverlayManager] Timeout loading '{optionsSceneName}'.");
            }
        }

        public async UniTask CloseOptionsAsync(string optionsSceneName, string fallbackSceneName)
        {
            var optionsScene = SceneManager.GetSceneByName(optionsSceneName);
            if (optionsScene.IsValid() && optionsScene.isLoaded)
            {
                _unloadPublisher.Publish(new SceneUnloadRequested(optionsSceneName));

                using var cts = new System.Threading.CancellationTokenSource(TIMEOUT_MS);
                try
                {
                    await UniTask.WaitUntil(
                        () => !IsSceneLoaded(optionsSceneName),
                        cancellationToken: cts.Token);
                }
                catch (OperationCanceledException)
                {
                    Debug.LogError($"[OverlayManager] Timeout unloading '{optionsSceneName}'.");
                }
            }

            var target = _previousScene;
            if (!target.IsValid() || !target.isLoaded)
                target = SceneManager.GetSceneByName(fallbackSceneName);

            if (target.IsValid() && target.isLoaded)
            {
                SceneManager.SetActiveScene(target);

               // if (target.name == _mainMenuSceneName)
                   // _optionsClosedPublisher.Publish(new OptionsClosed(target.name));
            }
            else
            {
                Debug.LogError($"[OverlayManager] Failed to restore scene. Target invalid.");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }

        private static bool IsSceneLoaded(string name)
        {
            var s = SceneManager.GetSceneByName(name);
            return s.IsValid() && s.isLoaded;
        }
    }
}   