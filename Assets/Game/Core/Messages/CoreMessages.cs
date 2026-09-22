
using UnityEngine;

public class CoreMessages {}

namespace Game.Core.Messages
{
    public readonly struct StartGameRequested
    {
        public readonly string SceneName;
        public readonly string[] AdditiveScenes;

        public StartGameRequested(string sceneName, string[] additiveScenes = null)
        {
            SceneName = sceneName;
            AdditiveScenes = additiveScenes;
        }
    }

    public readonly struct PauseGameRequested
    {
        public static PauseGameRequested Default => new();
    }

    public readonly struct ResumeGameRequested
    {
        public static ResumeGameRequested Default => new();
    }

    public readonly struct GameStarted
    {
        public static GameStarted Default => new();
    }

    public readonly struct GamePaused
    {
        public static GamePaused Default => new();
    }

    public readonly struct GameResumed
    {
        public static GameResumed Default => new();
    }


    #region Input

    public readonly struct InputBackPressed
    {
        public static InputBackPressed Default => new();
    }

    #endregion

    #region User

    public readonly struct UserJoined
    {
        public string Username { get; }
        public UserJoined(string username) => Username = username;
    }

    #endregion

    #region Levels

    public readonly struct LevelCompleteSignal
    {
        public static readonly LevelCompleteSignal Default = new LevelCompleteSignal();
    }

    public readonly struct RestartLevelSignal
    {
        public static readonly RestartLevelSignal Default = new RestartLevelSignal();
    }

    public readonly struct LoadLevelSignal
    {
        public readonly int LevelIndex;
        public LoadLevelSignal(int levelIndex) => LevelIndex = levelIndex;
    }

    public readonly struct GameCompletedSignal
    {
        public static readonly GameCompletedSignal Default = new GameCompletedSignal();
    }

    public readonly struct SceneLoadedSignal
    {
        public readonly string SceneName;
        public SceneLoadedSignal(string sceneName) => SceneName = sceneName;
    }

    public readonly struct LevelProgressionChangedSignal
    {
        public readonly int LevelIndex;
        public readonly int SceneIndex;
        public readonly string SceneName;
        public LevelProgressionChangedSignal(int levelIndex, int sceneIndex, string sceneName)
        {
            LevelIndex = levelIndex;
            SceneIndex = sceneIndex;
            SceneName = sceneName;
        }
    }
    #endregion

    public struct MenuNavDirectionMessage
    {
        public Vector2 Direction;
    }
    
}