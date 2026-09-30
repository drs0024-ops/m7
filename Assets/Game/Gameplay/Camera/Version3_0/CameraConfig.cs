using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using Game.Core.Enums;

namespace Game.Gameplay.Camera
{
    /// <summary>
    /// Per-scene camera configuration. Lives as a MonoBehaviour in the scene
    /// so it can hold references to scene-specific virtual cameras.
    /// Data-driven: adding a new camera mode requires adding a row — zero code changes.
    /// </summary>
    public class CameraConfig : MonoBehaviour
    {
        [Tooltip("All virtual camera rigs for this scene.")]
        public List<CameraRigEntry> rigs = new List<CameraRigEntry>(3);

        /// <summary>
        /// The composer currently active on the live virtual camera.
        /// Updated by CameraSwitcher on each switch. Read by CameraEffectController and CameraPanMover.
        /// </summary>
        public CinemachinePositionComposer ActiveComposer { get; private set; }

        /// <summary>
        /// Called by CameraSwitcher after switching to update the active composer reference.
        /// </summary>
        public void SetActiveComposer(CinemachinePositionComposer composer)
        {
            ActiveComposer = composer;
        }

        /// <summary>
        /// Returns the rig entry for the given mode, or default if not found.
        /// Only use when you know the mode exists. Use FindRig for null-checking.
        /// </summary>
        public CameraRigEntry GetRig(CameraMode mode)
        {
            for (int i = 0; i < rigs.Count; i++)
            {
                if (rigs[i].mode == mode)
                    return rigs[i];
            }
            return default;
        }

        /// <summary>
        /// Returns the rig entry for the given mode, or null if not found.
        /// </summary>
        public CameraRigEntry? FindRig(CameraMode mode)
        {
            for (int i = 0; i < rigs.Count; i++)
            {
                if (rigs[i].mode == mode)
                    return rigs[i];
            }
            return null;
        }

        /// <summary>
        /// Returns all follow-camera rigs (isFollowCam == true).
        /// </summary>
        public List<CameraRigEntry> GetFollowRigs()
        {
            var result = new List<CameraRigEntry>();
            for (int i = 0; i < rigs.Count; i++)
            {
                if (rigs[i].isFollowCam)
                    result.Add(rigs[i]);
            }
            return result;
        }

        /// <summary>
        /// Returns all non-follow rigs (locked, cutscene, etc.).
        /// </summary>
        public List<CameraRigEntry> GetNonFollowRigs()
        {
            var result = new List<CameraRigEntry>();
            for (int i = 0; i < rigs.Count; i++)
            {
                if (!rigs[i].isFollowCam)
                    result.Add(rigs[i]);
            }
            return result;
        }
    }
}   