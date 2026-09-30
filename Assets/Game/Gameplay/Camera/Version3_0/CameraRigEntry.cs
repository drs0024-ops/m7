using System;
using UnityEngine;
using Unity.Cinemachine;
using Game.Core.Enums;

namespace Game.Gameplay.Camera
{
    /// <summary>
    /// A single virtual camera rig entry. Adding a new camera mode
    /// requires adding a row in the CameraConfig inspector — zero code changes.
    /// </summary>
    [Serializable]
    public struct CameraRigEntry
    {
        [Tooltip("The camera mode this rig represents.")]
        public CameraMode mode;

        [Tooltip("The virtual camera component.")]
        public CinemachineCamera camera;

        [Tooltip("The position composer on the virtual camera.")]
        public CinemachinePositionComposer composer;

        [Tooltip("The 2D confiner on the virtual camera.")]
        public CinemachineConfiner2D confiner;

        [Tooltip("If true, this camera receives the player follow target. If false, it keeps its scene-set Follow.")]
        public bool isFollowCam;
    }
}   