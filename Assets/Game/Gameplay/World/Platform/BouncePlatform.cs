using System;
using System.Collections.Generic;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.World
{
    public class BouncePlatform : MonoBehaviour, IStartable, IDisposable
    {
        [Header("Squish Tween")]
        [SerializeField] private float _squishScaleX = 1.5f;
        [SerializeField] private float _squishScaleY = 0.5f;
        [SerializeField] private float _squishDuration = 0.15f;
        [SerializeField] private float _expandDuration = 0.2f;

        private bool _playerInBounceZone;

        private Vector3 _startScale;
        private bool _isTweening;
        private Animator _animator;

        private readonly IPublisher<BouncePlatformHit> _bounceHitPublisher;
        private readonly List<IDisposable> _subscriptions = new();

        public BouncePlatform()
        {
            _bounceHitPublisher = GlobalMessagePipe.GetPublisher<BouncePlatformHit>();
        }

        private void Start()
        {
            _startScale = transform.localScale;
            _animator = GetComponent<Animator>();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!collision.gameObject.CompareTag("Player")) return;
            if (collision.collider.name != "Feet") return;

            _bounceHitPublisher.Publish(new BouncePlatformHit(collision.transform));

            if (_playerInBounceZone)
                Squish();
        }

        private void Squish()
        {
            if (_isTweening) return;
            _isTweening = true;

            if (_animator != null)
                _animator.SetTrigger("Bounce");

            LeanTween.scale(gameObject, new Vector3(_squishScaleX, _squishScaleY, 1f), _squishDuration)
                .setEase(LeanTweenType.easeInBounce)
                .setOnComplete(() =>
                {
                    LeanTween.scale(gameObject, _startScale, _expandDuration)
                        .setEase(LeanTweenType.easeOutBounce)
                        .setOnComplete(() => _isTweening = false);
                });
        }

        public void OnPlayerEnteredBounceZone()
        {
            _playerInBounceZone = true;
        }

        public void OnPlayerExitedBounceZone()
        {
            _playerInBounceZone = false;
        }

        public void Dispose()
        {
            foreach (var d in _subscriptions)
                d?.Dispose();
            _subscriptions.Clear();
        }

        private void OnDestroy()
        {
            if (_subscriptions.Count != 0)
                Dispose();
        }

        void IStartable.Start()
        {
            Start();
        }
    }
}   