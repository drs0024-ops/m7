using System;
using System.Collections;
using System.Collections.Generic;
using MessagePipe;
using Game.Core.Messages;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.Player
{
    public class KnockBack : MonoBehaviour, IStartable, IDisposable
    {
        [SerializeField] private float _knockBackTime = 0.2f;
        [SerializeField] private float _hitDirectionForce = 7.5f;
        [SerializeField] private float _constForce = 5f;
        [SerializeField] private float _inputForce = 10f;
        [SerializeField] private AnimationCurve _knockBackForceCurve;
        [SerializeField] private Vector2 _constantForceDirection = Vector2.down;

        public bool IsBeingKnockedBack { get; private set; }

        private Rigidbody2D _rb;
        private Coroutine _knockBackCoroutine;

        private ISubscriber<PlayerDamaged> _damagedSubscriber;
        private IPublisher<KnockBackStarted> _knockBackStartedPublisher;
        private IPublisher<KnockBackEnded> _knockBackEndedPublisher;
        private readonly List<IDisposable> _subscriptions = new();

        private void Awake()
        {
            _rb = GetComponentInParent<Rigidbody2D>();
            if (_rb == null)
                Debug.LogError("[KnockBack] Rigidbody2D not found in parent!");

            _damagedSubscriber = GlobalMessagePipe.GetSubscriber<PlayerDamaged>();
            _knockBackStartedPublisher = GlobalMessagePipe.GetPublisher<KnockBackStarted>();
            _knockBackEndedPublisher = GlobalMessagePipe.GetPublisher<KnockBackEnded>();
        }

        void IStartable.Start()
        {
            _subscriptions.Add(_damagedSubscriber.Subscribe(OnPlayerDamaged));
        }

        private void OnPlayerDamaged(PlayerDamaged message)
        {
            if (_knockBackCoroutine != null)
                StopCoroutine(_knockBackCoroutine);

            _knockBackCoroutine = StartCoroutine(KnockBackAction(
                (Vector2)message.HitDirection,
                _constantForceDirection,
                message.InputAxisX));
        }

        private IEnumerator KnockBackAction(Vector2 hitDirection, Vector2 constantForceDirection, float inputDirection)
        {
            IsBeingKnockedBack = true;
            _knockBackStartedPublisher.Publish(new KnockBackStarted(transform));

            Vector2 constantForce = constantForceDirection * _constForce;
            float elapsedTime = 0f;
            float curveTime = 0f;

            while (elapsedTime < _knockBackTime)
            {
                elapsedTime += Time.fixedDeltaTime;
                curveTime += Time.deltaTime;

                Vector2 hitForce = hitDirection * _hitDirectionForce * _knockBackForceCurve.Evaluate(curveTime);
                Vector2 knockBackForce = hitForce + constantForce;

                Vector2 combinedForce = knockBackForce;
                if (inputDirection != 0f)
                    combinedForce += new Vector2(-inputDirection * _inputForce, 0f);

                _rb.linearVelocity = combinedForce;
                yield return new WaitForFixedUpdate();
            }

            IsBeingKnockedBack = false;
            _knockBackEndedPublisher.Publish(new KnockBackEnded(transform));
        }

        private void OnDestroy()
        {
            if (_knockBackCoroutine != null)
                StopCoroutine(_knockBackCoroutine);
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions)
                sub?.Dispose();
            _subscriptions.Clear();
        }
    }
}   