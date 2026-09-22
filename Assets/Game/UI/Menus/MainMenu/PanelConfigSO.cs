using UnityEngine;

namespace Game.UI
{
    [CreateAssetMenu(fileName = "NewPanelConfig", menuName = "UI/Panel Config")]
    public class PanelConfigSO : ScriptableObject
    {
        [Tooltip("Unique identifier for this panel (used in code)")]
        public string panelID; 

        [Tooltip("Optional: Override default fade duration for this specific panel")]
        public bool useCustomDuration = false;
        public float customDuration = 0.5f;
    }   
}
