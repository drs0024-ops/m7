using Game.Core.Messages;
using MessagePipe;
using UnityEngine;

namespace Game.Gameplay.Enemies
{
    public class HeadDamageCheck : MonoBehaviour
    {
        [SerializeField] private float _damageAmount = 10f;

        private IPublisher<EntityDamaged> _damagePublisher;
        private Transform _enemyRoot;

        private void Awake()
        {
            _damagePublisher = GlobalMessagePipe.GetPublisher<EntityDamaged>();
            _enemyRoot = transform.root;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!collision.gameObject.CompareTag("Player")) return;
            _damagePublisher.Publish(new EntityDamaged(_enemyRoot, _damageAmount, transform.forward));
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.gameObject.CompareTag("Player")) return;
            _damagePublisher.Publish(new EntityDamaged(_enemyRoot, _damageAmount, transform.forward));
        }
    }
}   