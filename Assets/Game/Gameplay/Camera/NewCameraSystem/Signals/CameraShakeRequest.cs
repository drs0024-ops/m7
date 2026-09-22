using UnityEngine;
using Game.Gameplay.Camera;
/// <summary>
/// Request to trigger a screen shake effect.
/// </summary>
public readonly struct CameraShakeRequest
{
    public readonly float ForceMultiplier;
    public readonly ScreenShakeProfile Profile;

    public CameraShakeRequest(float forceMultiplier, ScreenShakeProfile profile)
    {
        ForceMultiplier = forceMultiplier;
        Profile = profile;
    }
}