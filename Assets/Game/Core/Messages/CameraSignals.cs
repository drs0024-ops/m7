using UnityEngine;
using Game.Core.Enums;


public class CameraSignals {}

namespace Game.Core.Messages
{
    #region System

    public readonly struct CameraSystemReady
    {
        public static CameraSystemReady Default => new();
    }

    public readonly struct CameraSwitchRequest
    {
        public readonly CameraMode Mode { get; }
        public readonly Vector2 ExitDirection { get; }

        public CameraSwitchRequest(CameraMode mode, Vector2 exitDirection)
        {
            Mode = mode;
            ExitDirection = exitDirection;
        }
    }
    public readonly struct CameraTargetUpdate
    {
        public Transform FollowTarget { get; }
        public Collider2D Boundary { get; }
        public CameraTargetUpdate(Transform followTarget, Collider2D boundary)
        {
            FollowTarget = followTarget;
            Boundary = boundary;
        }
    }

    #endregion

    #region Effects

    

    public readonly struct CameraDampingRequest
    {
        public bool IsFalling { get; }
        public CameraDampingRequest(bool isFalling) => IsFalling = isFalling;
    }

    #endregion

    #region Pan / Bounds

    public readonly struct CameraPanRequest
    {
        public Vector2 TargetOffset { get; }
        public float Duration { get; }
        public CameraPanRequest(Vector2 targetOffset, float duration)
        {
            TargetOffset = targetOffset;
            Duration = duration;
        }
    }

    public readonly struct CameraBoundsResetRequest
    {
        public static CameraBoundsResetRequest Default => new();
    }

    #endregion
}