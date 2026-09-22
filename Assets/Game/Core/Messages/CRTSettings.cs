using UnityEngine;
using Game.Core.Enums;

namespace Game.UI
{
    [System.Serializable]
    public struct CRTSettings
    {
        public float transitionDuration;

        public Color tearColor;
        public float tearSpeed;
        public float tearPause;
        public CRTScrollDirection tearDirection;
        public float idleTearOpacity;
        public float transitionTearOpacity;
        public float tearFontSize;
        public float tearLeadingOpacity;
        public float tearTrailingOpacity;

        public float retraceSpeed;
        public float retraceSpeed2;
        public float retraceHeight;
        public float retraceOpacity;
        public CRTScrollDirection retraceDirection;
        public float idleRetraceIntensity;
        public float transitionRetraceIntensity;

        public float idleIntensity;
        public float transitionIntensity;
        public float transitionOpacity;

        public float rollBarIdleIntensity;
        public float rollBarTransitionIntensity;
    }
}   