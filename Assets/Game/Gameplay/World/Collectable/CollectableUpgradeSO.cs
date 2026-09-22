using Game.Core.Messages;
using Game.Gameplay.Upgrades;
using MessagePipe;
using UnityEngine;

namespace Game.Gameplay.World
{
    [CreateAssetMenu(menuName = "Collectable/Player Upgrade", fileName = "New Player Upgrade")]
    public class CollectableUpgradeSO : CollectableSOBase
    {
        [SerializeField] private PlayerUpgrade _upgradeToGive;

        private readonly IPublisher<UpgradePickedUp> _upgradePublisher;

        public CollectableUpgradeSO()
        {
            _upgradePublisher = GlobalMessagePipe.GetPublisher<UpgradePickedUp>();
        }

        public override void Collect(GameObject objectThatCollected)
        {
            if (_upgradeToGive == null) return;
            _upgradePublisher.Publish(new UpgradePickedUp(_upgradeToGive.UpgradeID));
        }
    }
}   