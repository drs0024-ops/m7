using UnityEngine;
using Game.Core.Interfaces;
using Game.Core.Data;

namespace Game.Gameplay.World
{
    public class PopUpIngameText : MonoBehaviour, ITalkable
    {
        [SerializeField] private DialogueText _dialogueText;
        [SerializeField] private IDialogueController _dialogueController;
        [SerializeField] private bool _autoClose = true;

        public void Talk()
        {
            _dialogueController.ShowDialogue(_dialogueText, _autoClose);
        }
    }
}   