using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{
    /// <summary>
    /// Passive lookup table for scene names. Does NOT make loading decisions.
    /// The single source of truth for which scene loads first is MainMenuScene.
    /// </summary>
    public class SceneRegistry
    {
        #region Properties

        public string MainMenuScene { get; }
        public string GameLoopScene { get; }
        public string SavedGameScene { get; }
        public string IntroScene { get; }
        public string NewGameScene { get; }

        public string FirstLevelScene => _levels[0];

        #endregion

        #region Fields

        private readonly List<string> _levels;

        #endregion

        #region Construction

        public SceneRegistry(
            string mainMenu = "MainMenu",
            string gameLoop = "GameLoop",
            string savedGame = "SavedGame",
            string intro = "IntroScene",
            string newGame = "Facility",
            IEnumerable<string> levels = null)
        {
            MainMenuScene = mainMenu;
            GameLoopScene = gameLoop;
            SavedGameScene = savedGame;
            IntroScene = intro;
            NewGameScene = newGame;
            _levels = levels?.ToList() ?? new List<string> { newGame };
        }

        #endregion

        #region Public API

        public string GetLevelScene(int index) => _levels[index];

        #endregion
    }
}   