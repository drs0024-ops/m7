
using UnityEngine;

namespace Game.Core.Data
{
    /// <summary>
    /// Reusable dialogue content. Create in Project panel, drag into any NPC.
    /// </summary>
    [CreateAssetMenu(fileName = "Dialogue", menuName = "Dialogue/Text")]
    public class DialogueDataSO : ScriptableObject
    {
        [Tooltip("Name displayed above the dialogue box.")]
        public string speakerName;

        [Tooltip("Each entry is one paragraph. Player advances through them.")]
        public string[] paragraphs;

        [Tooltip("Typing speed for this speaker. Lower = faster.")]
        public float typeSpeed = 10f;

        [Tooltip("Sound to play per character. Null = silent.")]
        public AudioClip typeBlip;

        [Tooltip("Sound on panel open.")]
        public AudioClip openSound;

        [Tooltip("Sound on panel close.")]
        public AudioClip closeSound;
    }
}   