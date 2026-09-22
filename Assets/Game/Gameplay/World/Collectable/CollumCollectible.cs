using Game.Core.Interfaces;
using UnityEngine;

namespace Game.Gameplay.World
{
    public class CollumCollectible : MonoBehaviour, ITriggerCheckable
    {
        [SerializeField] private GameObject _collume;
        [SerializeField] private AudioClip _soundClip;
        [SerializeField] private Collider2D _trigger;

        [Header("Animation")]
        [SerializeField] private float _heightMultiplier = 2f;
        [SerializeField] private float _duration = 2f;
        [SerializeField] private AnimationCurve _easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private AudioSource _audioSource;
        private Vector2 _startingPosition;
        private float _height;

        public bool IsAggroed { get; private set; }
        public bool IsWithinStrikingDistance { get; private set; }
        bool ITriggerCheckable.IsAggroed { get => IsAggroed; set => IsAggroed = value; }
        public bool IsWithinStrickingDistance { get; set; }

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            _startingPosition = _collume.transform.position;
            _height = _trigger.transform.localScale.y * _heightMultiplier;
        }

        public void SetAggroStatus(bool isAggroed)
        {
            if (IsAggroed == isAggroed) return;
            IsAggroed = isAggroed;

            if (isAggroed)
                ExtendObject();
            else
                RetractObject();
        }

        public void SetStrikingDistance(bool isWithinStrikingDistance)
        {
            IsWithinStrikingDistance = isWithinStrikingDistance;
        }

        #region Tween

        private void ExtendObject()
        {
            PlaySound();
            LeanTween.moveY(_collume, transform.position.y + _height, _duration)
                .setEase(_easeCurve);
        }

        private void RetractObject()
        {
            PlaySound();
            LeanTween.moveY(_collume, _startingPosition.y, _duration)
                .setEase(_easeCurve);
        }

        private void PlaySound()
        {
            if (_audioSource != null && _soundClip != null)
                _audioSource.PlayOneShot(_soundClip);
        }

        public void SetStrikingDistanceBool(bool isWithinStrikingDistance)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}   