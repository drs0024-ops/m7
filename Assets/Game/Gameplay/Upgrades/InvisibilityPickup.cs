using UnityEngine;
using Game.Core.Messages;
using Game.Core.Interfaces;
using MessagePipe;
using VContainer;

namespace Game.Gameplay.Upgrades
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class InvisibilityPickup : MonoBehaviour
    {
        [SerializeField] private string _upgradeID = UpgradeIDs.Invisibility;
        [SerializeField] private bool _destroyOnPickup = true;

        private IPublisher<UpgradePickedUp> _publisher;
        private IUpgradeState _upgradeState;
        private bool _collected;

        [Inject]
        private void Inject(IPublisher<UpgradePickedUp> publisher, IUpgradeState upgradeState)
        {
            _publisher = publisher;
            _upgradeState = upgradeState;
        }

        private void Awake()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void Start()
        {
            if (_upgradeState.IsUnlocked(_upgradeID))
            {
                _collected = true;
                gameObject.SetActive(false);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_collected || !collision.transform.root.CompareTag("Player")) return;

            _collected = true;
            _publisher.Publish(new UpgradePickedUp(_upgradeID));

            if (_destroyOnPickup)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }
    }
}   