namespace Game.Core.Interfaces
{
    public interface ILevelProgression
    {
        int CoinsCollected { get; }
        float CurrentHealth { get; }
        void UpdateState(int coinsCollected, float currentHealth);
        void CompleteLevel();
    }
}   