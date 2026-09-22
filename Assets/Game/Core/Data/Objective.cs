using System;
using System.Collections.Generic;

namespace Game.Core.Data
{
    [Serializable]
    public class Objective
    {
        public List<Achievement> achievements;
        public int currentAchievementIndex;

        public bool IsComplete => currentAchievementIndex >= achievements.Count;
        public Achievement Current => achievements[currentAchievementIndex];
    }
}   