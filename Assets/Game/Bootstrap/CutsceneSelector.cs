using Game.Core.Data;
using Game.Core.Interfaces;
using UnityEngine.Video;

namespace Game.Core
{
    public class CutsceneSelector : ICutsceneSelector
    {
        private readonly CutscenePoolSO _pool;
        

        public CutsceneSelector(CutscenePoolSO pool)
        {
            _pool = pool;
        }

        public VideoClip GetForIntro() => _pool.GetIntro();
        public VideoClip GetForNewGame() => _pool.GetNewGame();
        public VideoClip GetForLoad(string levelName) => _pool.GetForLevel(levelName);
        public VideoClip GetForLevelComplete(string levelName) => _pool.GetForLevel(levelName);
        public VideoClip GetForGameOver() => null;
        public VideoClip GetForEnding() => null;
    }
}   