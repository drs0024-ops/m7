using System;
using System.Collections.Generic;
using Game.Core.Interfaces;

namespace Game.Core.Data
{
    [Serializable]
    public struct LevelProgressionSaveData : ISaveData
    {
        public string levelName;
        public List<SceneField> scenes;

        public SceneField FirstScene => scenes != null && scenes.Count > 0 ? scenes[0] : default;

        public string SaveId => "LevelProgression";
    }
}   