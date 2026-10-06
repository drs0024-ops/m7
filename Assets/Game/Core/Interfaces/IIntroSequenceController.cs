using Cysharp.Threading.Tasks;

namespace Game.Core.Interfaces
{
    /// <summary>
    /// Coordinates cutscene playback for intro, new-game, and load sequences.
    /// Decouples cutscene selection and playback from the game flow state machine.
    /// </summary>
    public interface IIntroSequenceController
    {
        /// <summary>
        /// Plays the configured intro cutscene and transitions to the menu phase.
        /// </summary>
        UniTask PlayIntroAsync();

        /// <summary>
        /// Handles video completion by transitioning to the menu phase and loading it.
        /// </summary>
        void OnVideoFinished();

        /// <summary>
        /// Plays the configured new-game cutscene after the level loads.
        /// </summary>
        UniTask PlayNewGameCutsceneAsync();

        /// <summary>
        /// Plays the configured load cutscene for the specified level.
        /// </summary>
        UniTask PlayLoadCutsceneAsync(string levelName);
    }
}