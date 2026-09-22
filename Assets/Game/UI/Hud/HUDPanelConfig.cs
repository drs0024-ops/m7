
using System;
using UnityEngine;

namespace Game.UI
{
	[Serializable]
    public class HUDPanelConfig
    {
        [Tooltip("The GameObject of the panel.")]
        public GameObject panel;
        [Tooltip("The CanvasGroup component for fading.")]
        public CanvasGroup canvasGroup;
        [Tooltip("If true, stays visible until explicitly hidden. If false, fades out after delay.")]
        public bool isConstant = true;
        [Tooltip("Seconds to wait before fading out (non-constant only).")]
        public float displayDuration = 2.0f;
    }
	


}