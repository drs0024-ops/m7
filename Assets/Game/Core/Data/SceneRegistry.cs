using System.Collections.Generic;
using System.Linq;
using VContainer.Unity;

namespace Game.Core
{
    /// <summary>
    /// Passive lookup table for scene names AND their associated child DI scopes.
    /// The single source of truth for scene identity and scope lifecycle tracking.
    /// </summary>
    public class SceneRegistry
    {
        #region Properties

        public string MainMenuScene { get; }
        public string GameLoopScene { get; }
        public string SavedGameScene { get; }
        public string IntroScene { get; }
        public string NewGameScene { get; }
        public string PersistentScene { get; }
        public string FirstLevelScene => _levels[0];

        #endregion

        #region Fields

        private readonly List<string> _levels;
        private readonly Dictionary<string, LifetimeScope> _sceneScopes = new(4);

        #endregion

        #region Construction

        public SceneRegistry(
            string mainMenu = "MainMenu",
            string gameLoop = "GameLoop",
            string savedGame = "SavedGame",
            string intro = "IntroScene",
            string newGame = "Facility",
            string persistent = "Bootstrap",
            IEnumerable<string> levels = null)
        {
            MainMenuScene = mainMenu;
            GameLoopScene = gameLoop;
            SavedGameScene = savedGame;
            IntroScene = intro;
            NewGameScene = newGame;
            PersistentScene = persistent;
            _levels = levels?.ToList() ?? new List<string> { newGame };
        }

        #endregion

        #region Public API — Scene Names

        public string GetLevelScene(int index)
        {
            if (index < 0 || index >= _levels.Count)
            {
                UnityEngine.Debug.LogError($"[SceneRegistry] Invalid level index {index} (count: {_levels.Count}).");
                return null;
            }
            return _levels[index];
        }

        #endregion

        #region Public API — Scope Tracking

        public void RegisterScope(string sceneName, LifetimeScope scope)
        {
            if (string.IsNullOrEmpty(sceneName) || scope == null) return;
            _sceneScopes[sceneName] = scope;
        }

        public LifetimeScope GetScope(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return null;
            return _sceneScopes.TryGetValue(sceneName, out var scope) ? scope : null;
        }

        public LifetimeScope UnregisterScope(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return null;
            if (_sceneScopes.TryGetValue(sceneName, out var scope))
            {
                _sceneScopes.Remove(sceneName);
                return scope;
            }
            return null;
        }

        #endregion
    }
}   