using Game.Gameplay.Camera;

namespace Game.Core.Messages
{
    /// <summary>
    /// Requests a camera shake. Uses a profile for shape/duration, or a flat force multiplier.
    /// </summary>
    public readonly struct CameraShakeRequest
    {
        public readonly ScreenShakeProfile Profile;
        public readonly float ForceMultiplier;

        public CameraShakeRequest(ScreenShakeProfile profile, float forceMultiplier = 1f)
        {
            Profile = profile;
            ForceMultiplier = forceMultiplier;
        }
    }
}   