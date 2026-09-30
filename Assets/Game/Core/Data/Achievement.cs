// Serializable achievement definition for objective tracking.
using System;

namespace Game.Core.Data
{
    [Serializable]
    public struct Achievement
    {
        public string AchievementID;
        public string Title;
        public string Description;
        public int RequiredCount;
        public float Duration;
    }
}    