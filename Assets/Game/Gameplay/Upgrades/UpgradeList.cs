using UnityEngine;

namespace Game.Gameplay.Upgrades
{
    [CreateAssetMenu(fileName = "UpgradeList", menuName = "Upgrades/UpgradeList")]
    public class UpgradeList : ScriptableObject
    {
        public PlayerUpgrade[] upgrades;

        public PlayerUpgrade GetUpgrade(string id)
        {
            return System.Array.Find(upgrades, u => u.UpgradeID == id);
        }
    }
}   