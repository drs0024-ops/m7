using UnityEngine;
using Game.Core.Data;

namespace Game.Gameplay.Achievements
{
    [CreateAssetMenu(fileName = "AchievementList", menuName = "Achievement/Achievement List")]
    public class AchievementList : ScriptableObject
    {
        public Achievement[] achievements;

        public Achievement GetAchievement(string id)
        {
            for (int i = 0; i < achievements.Length; i++)
                if (achievements[i].AchievementID == id)
                    return achievements[i];

            return default;
        }
    }
}   