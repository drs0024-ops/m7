namespace Game.Core.Enums
{
    /// <summary>
    /// Player behavioral state (bridges HSM to public API).
    /// </summary>
    public enum PlayerState
    {
        Idle,
        Walking,
        Running,
        Jumping,
        Airborne,
        Attacking,
        TakingDamage,
        PlayerDied
    }
}