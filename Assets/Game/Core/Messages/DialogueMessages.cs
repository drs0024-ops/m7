
using Game.Core.Data;

namespace Game.Core.Messages
{
    public class DialogueMessages{}
    /// <summary>
    /// Published by any NPC/interactable that wants to start a dialogue.
    /// </summary>
    public readonly struct DialogueRequested
    {
        public readonly DialogueDataSO Dialogue;
        public readonly bool AutoClose;

        public DialogueRequested(DialogueDataSO dialogue, bool autoClose = false)
        {
            Dialogue = dialogue;
            AutoClose = autoClose;
        }
    }

	/// <summary>
    /// Published when a dialogue conversation begins.
    /// </summary>
    public readonly struct DialogueStarted
    {
        public readonly string SpeakerName;
        public readonly string DialogueID;

        public DialogueStarted(string speakerName, string dialogueID)
        {
            SpeakerName = speakerName;
            DialogueID = dialogueID;
        }
    }

	/// <summary>
    /// Published when a dialogue conversation ends.
    /// </summary>
    public readonly struct DialogueEnded
    {
        public static readonly DialogueEnded Default = new();
    }

}   