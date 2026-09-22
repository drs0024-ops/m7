using UnityEngine;

namespace Game.Gameplay.Achievements
{
    [CreateAssetMenu(fileName = "AchievementConfig", menuName = "Game/Achievements/Config")]
    public class AchievementConfigSO : ScriptableObject
    {
        public AchievementList achievementList;
    }
}   