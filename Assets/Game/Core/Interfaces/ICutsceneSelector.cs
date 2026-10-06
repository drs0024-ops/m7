using UnityEngine.Video;

namespace Game.Core.Interfaces
{
    public interface ICutsceneSelector
    {
        VideoClip GetForIntro();
        VideoClip GetForNewGame();
        VideoClip GetForLoad(string levelName);
        VideoClip GetForLevelComplete(string levelName);
        VideoClip GetForGameOver();
        VideoClip GetForEnding();
    }
}   