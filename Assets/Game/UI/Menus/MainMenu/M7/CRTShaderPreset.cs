using UnityEngine;
using Game.Core.Enums;

namespace Game.UI
{
    [CreateAssetMenu(fileName = "CRT_Preset", menuName = "UI/CRT Shader Preset")]
    public class CRTShaderPreset : ScriptableObject
    {
        [Header("Failure")]
        [Range(0f, 2f)] public float failureAmount = 0.15f;
        [Range(0f, 1f)] public float transitionOpacity = 1f;

        [Header("Phosphor")]
        public Color phosphorColor = new Color(0f, 0.8f, 0.3f, 1f);

        [Header("Tear")]
        public Color tearColor = new Color(0f, 0.8f, 0.3f, 1f);
        [Range(0f, 2f)] public float tearSpeed = 0.25f;
        [Range(0f, 20f)] public float tearPauseMin = 1f;
        [Range(0f, 20f)] public float tearPauseMax = 5f;
        public CRTScrollDirection tearDirection = CRTScrollDirection.Down;
        [Range(0f, 1f)] public float tearOpacity = 1f;
        [Range(1f, 128f)] public float tearFontSize = 24f;
        [Range(0f, 1f)] public float tearLeadingOpacity = 1f;
        [Range(0f, 1f)] public float tearTrailingOpacity = 0f;

        [Header("Retrace")]
        [Range(0f, 1f)] public float retraceSpeed = 0.05f;
        [Range(0f, 1f)] public float retraceSpeed2 = 0.03f;
        [Range(0.001f, 0.1f)] public float retraceHeight = 0.001f;
        [Range(0f, 1f)] public float retraceOpacity = 1f;
        [Range(0f, 2f)] public float retraceIntensity = 0.6f;
        public CRTScrollDirection retraceDirection = CRTScrollDirection.Up;

        [Header("Roll Bar")]
        [Range(0f, 2f)] public float rollBarIdleIntensity = 0.15f;
        [Range(0f, 2f)] public float rollBarTransitionIntensity = 1f;

        [Header("Transition")]
        [Range(0.01f, 1f)] public float transitionDuration = 0.066f;

        public void ApplyTo(Material overlayMat, Material rollBarMat)
        {
            overlayMat.SetFloat("_FailureAmount", failureAmount);
            overlayMat.SetFloat("_TransitionOpacity", transitionOpacity);
            overlayMat.SetColor("_PhosphorColor", phosphorColor);

            overlayMat.SetColor("_TearColor", tearColor);
            overlayMat.SetFloat("_TearDirection", tearDirection == CRTScrollDirection.Up ? 1f : -1f);
            overlayMat.SetFloat("_TearOpacity", tearOpacity);
            overlayMat.SetFloat("_TearLeadingOpacity", tearLeadingOpacity);
            overlayMat.SetFloat("_TearTrailingOpacity", tearTrailingOpacity);

            overlayMat.SetFloat("_RetraceIntensity", retraceIntensity);
            overlayMat.SetFloat("_RetraceOpacity", retraceOpacity);
            overlayMat.SetFloat("_RetraceWidth", retraceHeight);

            rollBarMat.SetFloat("_Intensity", rollBarIdleIntensity);
        }
    }
}   