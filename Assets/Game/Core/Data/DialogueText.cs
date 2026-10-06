using UnityEngine;

namespace Game.Core.Data
{
    [CreateAssetMenu(fileName = "DialogueText", menuName = "Game/Dialogue Text")]
    public class DialogueText : ScriptableObject
    {
        public string speakerName;
        public string[] paragraphs;
    }
}    