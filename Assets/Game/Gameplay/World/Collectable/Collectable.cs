using UnityEngine;

namespace Game.Gameplay.World
{
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(CollectableTriggerHandler))]
    public class Collectable : MonoBehaviour
    {
        [SerializeField] private CollectableSOBase _collectable;

        [Header("Scale Pulse")]
        [SerializeField] private bool _pulseScale;
        [SerializeField] private float _pulseScaleSize = 1.1f;
        [SerializeField] private float _pulseDuration = 0.8f;

        [Header("Float")]
        [SerializeField] private bool _floatEnabled;
        [SerializeField] private float _floatYOffset = 0.5f;
        [SerializeField] private float _floatDuration = 1.0f;

        private Vector3 _startScale;
        private bool _scaleTweenStarted;
        private bool _floatTweenStarted;

        private void Reset()
        {
            GetComponent<CircleCollider2D>().isTrigger = true;
        }

        private void Start()
        {
            _startScale = transform.localScale;
        }

        private void Update()
        {
            if (_pulseScale && !_scaleTweenStarted)
                StartScalePulse();

            if (_floatEnabled && !_floatTweenStarted)
                StartFloat();
        }

        public void Collect(GameObject collector)
        {
            _collectable.Collect(collector);
        }

        #region Scale Pulse

        private void StartScalePulse()
        {
            _scaleTweenStarted = true;
            TweenScaleTo(_pulseScaleSize);
        }

        private void TweenScaleTo(float targetScale)
        {
            LeanTween.scale(gameObject, Vector3.one * targetScale, _pulseDuration)
                .setEase(LeanTweenType.easeInOutSine)
                .setOnComplete(() => TweenScaleTo(1f));
        }

        #endregion

        #region Float

        private void StartFloat()
        {
            _floatTweenStarted = true;
            LeanTween.moveLocalY(gameObject, transform.localPosition.y + _floatYOffset, _floatDuration)
                .setEase(LeanTweenType.easeOutBounce)
                .setOnComplete(FloatUp);
        }

        private void FloatUp()
        {
            float y = transform.localPosition.y;
            LeanTween.moveLocalY(gameObject, y + 0.2f, 1.0f)
                .setEase(LeanTweenType.easeInOutSine)
                .setOnComplete(FloatDown);
        }

        private void FloatDown()
        {
            float y = transform.localPosition.y;
            LeanTween.moveLocalY(gameObject, y - 0.2f, 1.0f)
                .setEase(LeanTweenType.easeInOutSine)
                .setOnComplete(FloatUp);
        }

        #endregion
    }
}   