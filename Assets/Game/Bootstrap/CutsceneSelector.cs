using Game.Core.Data;
using UnityEngine.Video;

namespace Game.Bootstrap
{
    /// <summary>
    /// Maps game context to a VideoClip from the CutscenePoolSO.
    /// Plain C#, VContainer-registered.
    /// </summary>
    public class CutsceneSelector
    {
        private readonly CutscenePoolSO _pool;

        public CutsceneSelector(CutscenePoolSO pool)
        {
            _pool = pool;
        }

        public VideoClip GetForNewGame() => _pool.GetNewGame();

        public VideoClip GetForLoad(string levelName) => _pool.GetForLevel(levelName);
    }
}   