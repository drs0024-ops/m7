using Game.Core.Interfaces;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;

namespace Game.Gameplay.World
{
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(DestructibleTriggerHandler))]
    public class Destructible : MonoBehaviour, IDamagable
    {
        [SerializeField] private GameObject _collectable;
        [SerializeField] private float _maxHealth = 3f;

        [Header("Hit Tween")]
        [SerializeField] private float _hitScale = 1.2f;
        [SerializeField] private float _hitDuration = 0.15f;

        [Header("Depleted Tween")]
        [SerializeField] private float _depletedScale = 0.5f;
        [SerializeField] private float _depletedDuration = 1.5f;

        [Header("Hit Audio")]
        [SerializeField] private AudioClip[] _audioClips;

        private float _currentHealth;
        private Vector3 _startScale;
        private bool _depleted;
        private bool _isTweening;
        private AudioSource _audioSource;

        private IPublisher<EntityDamaged> _damagePublisher;

        public bool IsDead => _currentHealth <= 0f;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;

        private void Awake()
        {
            if (_collectable != null)
                _collectable.SetActive(false);

            _startScale = transform.localScale;
            _audioSource = GetComponent<AudioSource>();
            _damagePublisher = GlobalMessagePipe.GetPublisher<EntityDamaged>();
        }

        private void Start()
        {
            _currentHealth = _maxHealth;
        }

        private void Reset()
        {
            GetComponent<CircleCollider2D>().isTrigger = true;
        }

        public void Damage(float amount, Vector3 knockbackDirection)
        {
            if (IsDead) return;

            _currentHealth -= amount;
            _damagePublisher.Publish(new EntityDamaged(transform, amount, knockbackDirection));

            if (_audioClips.Length > 0 && _audioSource != null)
                _audioSource.PlayOneShot(_audioClips[Random.Range(0, _audioClips.Length)]);

            if (IsDead)
                Deplete();
            else
                TweenHit();
        }

        void IDamagable.Die() => Deplete();

        private void Deplete()
        {
            if (_depleted) return;
            _depleted = true;

            if (_collectable != null)
                _collectable.SetActive(true);

            LeanTween.scale(gameObject, Vector3.one * _depletedScale, _depletedDuration)
                .setEase(LeanTweenType.easeOutSine);
        }

        public void TweenHit()
        {
            if (_isTweening) return;
            _isTweening = true;

            LeanTween.scale(gameObject, Vector3.one * _hitScale, _hitDuration)
                .setEase(LeanTweenType.easeInOutCirc)
                .setOnComplete(() =>
                {
                    LeanTween.scale(gameObject, _startScale, _hitDuration)
                        .setEase(LeanTweenType.easeInOutCirc)
                        .setOnComplete(() => _isTweening = false);
                });
        }
    }
}   