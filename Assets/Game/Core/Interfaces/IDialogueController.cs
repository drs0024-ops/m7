using Game.Core.Data;

namespace Game.Core.Interfaces
{
    public interface IDialogueController
    {
        void ShowDialogue(DialogueText dialogue, bool autoClose);
        void DisplayNextParagraph(DialogueText dialogue);
        void StopTyping();
    }
}   