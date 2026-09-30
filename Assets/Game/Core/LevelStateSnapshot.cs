namespace Game.Core
{
    /// <summary>
    /// Immutable snapshot of level state injected into scene scopes at load time.
    /// Carried via LifetimeScope.Enqueue — never via MessagePipe.
    /// </summary>
    public readonly struct LevelStateSnapshot
    {
        public readonly int CoinsCollected;
        public readonly float PlayerHealth;

        public LevelStateSnapshot(int coinsCollected, float playerHealth)
        {
            CoinsCollected = coinsCollected;
            PlayerHealth = playerHealth;
        }

        public override string ToString() =>
            $"LevelStateSnapshot(Coins={CoinsCollected}, Health={PlayerHealth})";
    }
}   