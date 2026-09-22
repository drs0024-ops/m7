using Game.Core.Data;
using Game.Core.Interfaces;
using UnityEngine;

namespace Game.Gameplay.World
{
    public class RoyalGuard : NPC, ITalkable
    {
        [SerializeField] private DialogueText _dialogueText;
        [SerializeField] private IDialogueController _dialogueController;

        public override void Interact()
        {
            Talk();
        }

        public void Talk()
        {
            _dialogueController.ShowDialogue(_dialogueText, autoClose: false);
        }
    }
}   