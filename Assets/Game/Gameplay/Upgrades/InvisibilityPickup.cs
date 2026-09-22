using UnityEngine;
using Game.Core.Messages;
using MessagePipe;

namespace Game.Gameplay.Upgrades
{
    public class InvisibilityPickup : MonoBehaviour
    {
        [SerializeField] private string _upgradeID = UpgradeIDs.Invisibility;
        [SerializeField] private bool _destroyOnPickup = true;

        private IPublisher<UpgradePickedUp> _publisher;

        private void Awake()
        {
            _publisher = GlobalMessagePipe.GetPublisher<UpgradePickedUp>();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag("Player")) return;

            _publisher.Publish(new UpgradePickedUp(_upgradeID));

            if (_destroyOnPickup)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }
    }
}   