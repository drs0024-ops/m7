using UnityEngine;
using Unity.Cinemachine;

namespace Game.Gameplay.Camera
{
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "Camera/Config")]
    public class CameraConfigSO : ScriptableObject
    {
        [Tooltip("Main camera following player on X and Y.")]
        public CinemachineCamera Center;

        [Tooltip("Camera following player on X only.")]
        public CinemachineCamera NoY;

        [Tooltip("Locked room camera.")]
        public CinemachineCamera Locked;
    }
}   