using UnityEngine;

namespace Game.Gameplay.Camera
{
    [CreateAssetMenu(fileName = "CameraSettings", menuName = "Camera/Settings")]
    public class CameraSettingsSO : ScriptableObject
    {
        public float shakeForce = 1f;
        public float fallDamping = 0.25f;
        public float panDuration = 0.5f;
        public float fallPanTime = 0.35f;
    }
}   