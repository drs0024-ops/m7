// ScriptableObject: defines a player upgrade (ID, name, duration).
using UnityEngine;

namespace Game.Gameplay.Upgrades
{
    [CreateAssetMenu(fileName = "New PlayerUpgrade", menuName = "Upgrades/PlayerUpgrade")]
    public class PlayerUpgrade : ScriptableObject
    {
        [SerializeField] private string _upgradeID;
        [SerializeField] private string _upgradeName;
        [SerializeField] private string _description;
        [SerializeField] private Sprite _icon;
        [Tooltip("Duration in seconds. 0 for permanent upgrades.")]
        [SerializeField] private float _duration = 0f;
        [Tooltip("Diegetic string shown as code fragment on pickup (e.g., 'STAMINA +20%').")]
        [SerializeField] private string _codeFragmentText;

        public string UpgradeID => _upgradeID;
        public string UpgradeName => _upgradeName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public float Duration => _duration;
        public string CodeFragmentText => _codeFragmentText;
    }
}   