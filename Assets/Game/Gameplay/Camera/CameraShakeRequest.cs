namespace Game.Gameplay.Camera
{
    public readonly struct CameraShakeRequest
    {
        public ScreenShakeProfile Profile { get; }
        public float ForceMultiplier { get; }

        public CameraShakeRequest(ScreenShakeProfile profile, float forceMultiplier = 1f)
        {
            Profile = profile;
            ForceMultiplier = forceMultiplier;
        }   
    }   
}   