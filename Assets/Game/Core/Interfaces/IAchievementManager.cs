using System.Collections.Generic;
using Game.Core.Data;

namespace Game.Core.Interfaces
{
    public interface IAchievementManager : ISaveable
    {
        void UnlockAchievement(string achievementId);
        bool IsAchievementUnlocked(string achievementId);
        Achievement GetAchievement(string id);
        IReadOnlyList<string> UnlockedAchievements { get; }
    }
}