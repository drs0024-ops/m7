using Game.Core.Enums;

public class SceneSignals
{

}

namespace Game.Core.Messages
{
    #region Load

    public readonly struct SceneLoadRequested
    {
        public string SceneName { get; }
        public bool ActivateOnLoad { get; }
        public SceneLoadRequested(string sceneName, bool activateOnLoad = false)
        {
            SceneName = sceneName;
            ActivateOnLoad = activateOnLoad;
        }
    }

    public readonly struct SceneLoaded
    {
        public string SceneName { get; }
        public int SceneHandle { get; }
        public SceneLoaded(string sceneName, int sceneHandle)
        {
            SceneName = sceneName;
            SceneHandle = sceneHandle;
        }
    }

    public readonly struct SceneActivated
    {
        public string SceneName { get; }
        public SceneActivated(string sceneName) => SceneName = sceneName;
    }

    public readonly struct SceneLoadFailed
    {
        public string SceneName { get; }
        public string Reason { get; }
        public SceneLoadFailed(string sceneName, string reason)
        {
            SceneName = sceneName;
            Reason = reason;
        }
    }

    #endregion

    #region Unload

    public readonly struct SceneUnloadRequested
    {
        public string SceneName { get; }
        public SceneUnloadRequested(string sceneName) => SceneName = sceneName;
    }

    public readonly struct SceneUnloaded
    {
        public string SceneName { get; }
        public SceneUnloaded(string sceneName) => SceneName = sceneName;
    }

    public readonly struct SceneUnloadFailed
    {
        public string SceneName { get; }
        public string Reason { get; }
        public SceneUnloadFailed(string sceneName, string reason)
        {
            SceneName = sceneName;
            Reason = reason;
        }
    }

    #endregion

    #region Transition

    public readonly struct SceneTransitionRequested
    {
        public string TargetSceneName { get; }
        public string SourceSceneName { get; }
        public SceneTransitionRequested(string targetSceneName, string sourceSceneName = "")
        {
            TargetSceneName = targetSceneName;
            SourceSceneName = sourceSceneName;
        }
    }

    public readonly struct SceneTransitionStarted
    {
        public string TargetScene { get; }
        public SceneTransitionStarted(string targetScene) => TargetScene = targetScene;
    }

    public readonly struct SceneTransitionCompleted
    {
        public string TargetScene { get; }
        public SceneTransitionCompleted(string targetScene) => TargetScene = targetScene;
    }

    #endregion

    #region Navigation



    public readonly struct LevelLoaded
    {
        public int LevelIndex { get; }
        public LevelLoaded(int levelIndex) => LevelIndex = levelIndex;
    }

    #endregion

    #region Doors
    public readonly struct DoorActivated
    {
        public readonly SceneField Scene;
        public readonly DoorToSpawnAt SpawnDoor;
        public readonly bool FromRight;

        public DoorActivated(SceneField scene, DoorToSpawnAt spawnDoor, bool fromRight)
        {
            Scene = scene;
            SpawnDoor = spawnDoor;
            FromRight = fromRight;
        }
    }
    #endregion
    
}   