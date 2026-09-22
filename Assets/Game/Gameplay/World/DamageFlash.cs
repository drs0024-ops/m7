using System.Collections;
using MessagePipe;
using Game.Core.Messages;
using UnityEngine;
using VContainer.Unity;
using System;
using System.Collections.Generic;
using VContainer;

namespace Game.Gameplay.World
{
    public class DamageFlash : MonoBehaviour, IStartable, IDisposable
    {
        [ColorUsage(true, true)]
        [SerializeField] private Color _flashColor = Color.white;
        [SerializeField] private float _flashTime = 0.25f;
        [SerializeField] private AnimationCurve _flashSpeedCurve;

        private SpriteRenderer[] _spriteRenderers;
        private Material[] _materials;
        private Coroutine _damageFlashCoroutine;

        private ISubscriber<EntityDamaged> _entityDamagedSubscriber;
        private ISubscriber<PlayerDamaged> _playerDamagedSubscriber;
        private readonly List<IDisposable> _subscriptions = new();

        [Inject]
        public DamageFlash()
        {
            _entityDamagedSubscriber = GlobalMessagePipe.GetSubscriber<EntityDamaged>();
            _playerDamagedSubscriber = GlobalMessagePipe.GetSubscriber<PlayerDamaged>();
        }

        private void Awake()
        {
            _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            _materials = new Material[_spriteRenderers.Length];
            for (int i = 0; i < _spriteRenderers.Length; i++)
                _materials[i] = _spriteRenderers[i].material;
        }

        void IStartable.Start()
        {
            _subscriptions.Add(_entityDamagedSubscriber.Subscribe(OnEntityDamaged));
            _subscriptions.Add(_playerDamagedSubscriber.Subscribe(OnPlayerDamaged));
        }

        private void OnEntityDamaged(EntityDamaged message)
        {
            if (message.Target != transform) return;
            TriggerFlash();
        }

        private void OnPlayerDamaged(PlayerDamaged message)
        {
            TriggerFlash();
        }

        private void TriggerFlash()
        {
            if (_damageFlashCoroutine != null)
                StopCoroutine(_damageFlashCoroutine);
            _damageFlashCoroutine = StartCoroutine(DamageFlasher());
        }

        private IEnumerator DamageFlasher()
        {
            SetFlashColor();
            float elapsedTime = 0f;
            while (elapsedTime < _flashTime)
            {
                elapsedTime += Time.deltaTime;
                SetFlashAmount(_flashSpeedCurve.Evaluate(elapsedTime / _flashTime));
                yield return null;
            }
        }

        private void SetFlashColor()
        {
            for (int i = 0; i < _materials.Length; i++)
                _materials[i].SetColor("FlashColor", _flashColor);
        }

        private void SetFlashAmount(float amount)
        {
            for (int i = 0; i < _materials.Length; i++)
                _materials[i].SetFloat("_FlashAmount", amount);
        }

        private void OnDestroy()
        {
            if (_damageFlashCoroutine != null)
                StopCoroutine(_damageFlashCoroutine);
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions)
                sub?.Dispose();
            _subscriptions.Clear();
        }
    }
}   