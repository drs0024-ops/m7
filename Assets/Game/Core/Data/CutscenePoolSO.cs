using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace Game.Core.Data
{
    /// <summary>
    /// Holds pools of cutscene videos, selectable by context.
    /// </summary>
    [CreateAssetMenu(fileName = "CutscenePool", menuName = "Game/Cutscene Pool")]
    public class CutscenePoolSO : ScriptableObject
    {
        [Header("New Game Pool")]
        [Tooltip("Randomly selected when starting a new game.")]
        public VideoClip[] newGamePool;

        [Header("Per-Level Pools (optional)")]
        [Tooltip("Matched by level scene name. Falls back to null if no match.")]
        public List<LevelCutscene> perLevel;

        [Serializable]
        public class LevelCutscene
        {
            public string levelName;
            public VideoClip[] pool;
        }

        public VideoClip GetNewGame()
        {
            if (newGamePool == null || newGamePool.Length == 0) return null;
            return newGamePool[UnityEngine.Random.Range(0, newGamePool.Length)];
        }

        public VideoClip GetForLevel(string levelName)
        {
            if (perLevel == null) return null;
            for (int i = 0; i < perLevel.Count; i++)
            {
                if (perLevel[i] != null
                    && perLevel[i].levelName == levelName
                    && perLevel[i].pool != null
                    && perLevel[i].pool.Length > 0)
                {
                    return perLevel[i].pool[UnityEngine.Random.Range(0, perLevel[i].pool.Length)];
                }
            }
            return null;
        }   
    }
}   