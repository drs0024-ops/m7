using UnityEngine;

namespace Game.Gameplay.Camera
{
    [CreateAssetMenu(fileName = "ScreenShakeProfile", menuName = "Camera/ScreenShakeProfile")]
    public class ScreenShakeProfile : ScriptableObject
    {
        [Min(0.01f)]
        public float impulseDuration = 0.5f;

        public AnimationCurve impulseCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Min(0f)]
        public float amplitudeGain = 1f;

        [Min(0f)]
        public float frequencyGain = 1f;

        public float impactForce = 1f;

        private void OnValidate()
        {
            if (impulseCurve == null || impulseCurve.length == 0)
                impulseCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

            if (impulseDuration < 0.01f) impulseDuration = 0.01f;
            if (amplitudeGain < 0f) amplitudeGain = 0f;
            if (frequencyGain < 0f) frequencyGain = 0f;
        }
    }
}   