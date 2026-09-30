using UnityEngine;
using MessagePipe;
using VContainer;
using VContainer.Unity;
using Game.Core.Messages;
using System;
using Game.Core.Interfaces;

namespace Game.Gameplay.World
{
    public class Bomb : MonoBehaviour, IStartable, IDisposable
    {
        [SerializeField] private float _fuseTime = 2f;
        [SerializeField] private float _blastRadius = 1.5f;
        [SerializeField] private float _damage = 25f;
        [SerializeField] private GameObject _explosionVfx;

        private IPublisher<EntityDamaged> _damagedPublisher;
        private float _timer;
        private bool _exploded;

        [Inject]
        private void Inject(IPublisher<EntityDamaged> damagedPublisher)
        {
            _damagedPublisher = damagedPublisher;
        }

        void IStartable.Start()
        {
            // Start fuse
        }

        private void Update()
        {
            if (_exploded) return;
            _timer += Time.deltaTime;
            if (_timer >= _fuseTime)
                Explode();
        }

        private void Explode()
        {
            _exploded = true;

            var colliders = Physics2D.OverlapCircleAll(transform.position, _blastRadius);
            for (int i = 0; i < colliders.Length; i++)
            {
                var target = colliders[i].GetComponentInParent<IDamagable>();
                if (target != null)
                {
                    Vector3 dir = (colliders[i].transform.position - transform.position).normalized;
                    target.Damage(_damage, dir);
                }
            }

            if (_explosionVfx != null)
                Instantiate(_explosionVfx, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }

        public void Dispose() { }
    }
}   