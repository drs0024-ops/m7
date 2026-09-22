using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using UnityEngine.SceneManagement;
using VContainer;
/// <summary>
/// Unloads all active scenes except the target, persistent, and optionally the main menu.
/// Called after a scene transition to prevent scene accumulation during additive loading.
/// </summary>
namespace Game.Bootstrap
{
    public class SceneCleanupService
    {
        private readonly string _mainMenuSceneName;

        [Inject]
        public SceneCleanupService(SceneRegistry config)
        {
            _mainMenuSceneName = config.MainMenuScene;
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

                var op = SceneManager.UnloadSceneAsync(s);
                await UniTask.WaitUntil(() => op.isDone, cancellationToken: token);
            }
        }   
    }
}   