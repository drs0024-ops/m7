using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Level
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Game/Configuration/Level Registry")]
    public class LevelConfigSO : ScriptableObject
    {
        [Header("Settings")]
        public int startingLevelIndex;

        [Header("Levels")]
        public List<LevelProgressionData> allLevels = new();
    }

    [System.Serializable]
    public class LevelProgressionData
    {
        public string levelName;
        public List<SceneField> scenes = new();

        public string FirstScene => scenes.Count > 0 ? scenes[0].SceneName : null;
    }
}   