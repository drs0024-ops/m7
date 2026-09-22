using UnityEngine;
using Game.Core.Enums;

namespace Game.Gameplay.Camera
{
    [CreateAssetMenu(fileName = "SwitchData", menuName = "Camera/Switch Data")]
    public class CameraSwitchDataSO : ScriptableObject
    {
        [Tooltip("Camera active when entering from the LEFT (moving Right)")]
        public CameraMode cameraOnRight;

        [Tooltip("Camera active when entering from the RIGHT (moving Left)")]
        public CameraMode cameraOnLeft;
    }
}   