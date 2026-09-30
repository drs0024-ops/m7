using UnityEngine;
using Game.Core.Enums;


public class CameraMessages {}

namespace Game.Core.Messages
{
    #region System

    /// <summary>
    /// Signals that the camera follow object is ready and positioned.
    /// </summary>
    public readonly struct CameraSystemReady
    {
        public static readonly CameraSystemReady Default = new CameraSystemReady();
    }

    /// <summary>
    /// Requests a switch to a specific camera mode.
    /// </summary>
    public readonly struct CameraSwitchRequest
    {
        public readonly CameraMode Mode;

        public CameraSwitchRequest(CameraMode mode)
        {
            Mode = mode;
        }
    }

    

    /// <summary>
    /// Updates the follow target, boundary, and optional locked room target.
    /// </summary>
    public readonly struct CameraTargetUpdate
    {
        public readonly Transform FollowTarget;
        public readonly Collider2D Boundary;
        public readonly Transform LockedRoomTarget;

        public CameraTargetUpdate(Transform followTarget, Collider2D boundary, Transform lockedRoomTarget = null)
        {
            FollowTarget = followTarget;
            Boundary = boundary;
            LockedRoomTarget = lockedRoomTarget;
        }
    }

    /// <summary>
    /// Published whenever the active camera mode changes.
    /// Subscribe to this instead of holding a CameraSwitcher reference.
    /// </summary>
    public readonly struct CameraModeActive
    {
        public readonly CameraMode Mode;

        public CameraModeActive(CameraMode mode)
        {
            Mode = mode;
        }
    }

    #endregion

    #region Effects

    /// <summary>
    /// Requests Y-axis damping change (fall pan).
    /// </summary>
    public readonly struct CameraDampingRequest
    {
        public readonly bool IsFalling;

        public CameraDampingRequest(bool isFalling)
        {
            IsFalling = isFalling;
        }
    }

    #endregion

    #region Pan / Bounds

    /// <summary>
    /// Requests a camera pan by offset over a duration.
    /// </summary>
    public readonly struct CameraPanRequest
    {
        public readonly Vector2 TargetOffset;
        public readonly float Duration;

        public CameraPanRequest(Vector2 targetOffset, float duration)
        {
            TargetOffset = targetOffset;
            Duration = duration;
        }
    }

    /// <summary>
    /// Request to reset all camera confiner boundaries to null.
    /// </summary>
    public readonly struct CameraBoundsResetRequest
    {
    }

    #endregion

    

    
}