using UnityEngine;
using VContainer;
using MessagePipe;
using System;
using System.Collections.Generic;
using Game.Core.Messages;
using UnityEngine.UI;

namespace Game.UI
{
    public class CrtWorldOverlay : MonoBehaviour, IDisposable
    {
        #region Fields

        [Header("Display")]
        [Range(0f, 1f)] [SerializeField] private float _opacity = 1f;
        [Range(0.1f, 1f)] [SerializeField] private float _zoom = 1f;
        [SerializeField] private float _panSpeed = 0.5f;
        [SerializeField] private float _lerpSpeed = 8f;

        [Header("References")]
        [SerializeField] private RawImage _worldImage;
        [SerializeField] private Image _solidBackground;

        private ISubscriber<MenuNavDirectionMessage> _navSub;
        private readonly List<IDisposable> _subscriptions = new(3);
        private bool _disposed;

        private Vector2 _currentOffset;
        private Vector2 _targetOffset;
        private Rect _uvRect;

        #endregion

        #region Injection

        [Inject]
        public void Construct(ISubscriber<MenuNavDirectionMessage> navSub)
        {
            _navSub = navSub;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_worldImage == null)
            {
                Debug.LogError("[CrtWorld] WorldImage not assigned!", this);
                return;
            }

            var rt = _worldImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _worldImage.raycastTarget = false;
            _worldImage.color = new Color(1f, 1f, 1f, _opacity);

            if (_solidBackground != null)
                _solidBackground.color = new Color(0f, 0f, 0f, _opacity);

            CenterUV();
        }

        private void Start()
        {
            _subscriptions.Add(_navSub.Subscribe(OnNavDirection));
            _subscriptions.Add(GlobalMessagePipe.GetSubscriber<CRTWorldZoomChanged>()
                .Subscribe(OnZoomChanged));
            _subscriptions.Add(GlobalMessagePipe.GetSubscriber<CRTWorldOpacityChanged>()
                .Subscribe(OnOpacityChanged));
        }

        private void OnDestroy()
        {
            if (!_disposed) Dispose();
        }

        #endregion

        #region Update

        private void Update()
        {
            if (_disposed) return;

            _currentOffset = Vector2.Lerp(_currentOffset, _targetOffset, _lerpSpeed * Time.deltaTime);

            float maxOffset = 1f - _zoom;
            _uvRect.x = Mathf.Clamp(_currentOffset.x, 0f, maxOffset);
            _uvRect.y = Mathf.Clamp(_currentOffset.y, 0f, maxOffset);

            _worldImage.uvRect = _uvRect;
        }

        private void OnValidate()
        {
            _zoom = Mathf.Clamp(_zoom, 0.1f, 1f);
            _opacity = Mathf.Clamp(_opacity, 0f, 1f);

            if (_worldImage != null)
            {
                _worldImage.color = new Color(1f, 1f, 1f, _opacity);
                _uvRect = new Rect((1f - _zoom) * 0.5f, (1f - _zoom) * 0.5f, _zoom, _zoom);
                _worldImage.uvRect = _uvRect;
            }

            if (_solidBackground != null)
                _solidBackground.color = new Color(0f, 0f, 0f, _opacity);
        }



        #endregion

        #region Message Handlers

        private void OnNavDirection(MenuNavDirectionMessage msg)
        {
            if (_disposed) return;
            _targetOffset += msg.Direction * _panSpeed * Time.deltaTime;
        }

        private void OnZoomChanged(CRTWorldZoomChanged msg)
        {
            if (_disposed) return;
            _zoom = Mathf.Clamp(msg.Zoom, 0.1f, 1f);
            RecalcUV();
        }

        private void OnOpacityChanged(CRTWorldOpacityChanged msg)
        {
            if (_disposed) return;
            _opacity = Mathf.Clamp(msg.Opacity, 0f, 1f);
            _worldImage.color = new Color(1f, 1f, 1f, _opacity);
            //if (_solidBackground != null);
               // comment in for the object to fade _solidBackground.color = new Color(0f, 0f, 0f, _opacity);
        }

        #endregion

        #region UV

        private void CenterUV()
        {
            _currentOffset = new Vector2((1f - _zoom) * 0.5f, (1f - _zoom) * 0.5f);
            _targetOffset = _currentOffset;
            RecalcUV();
        }

        private void RecalcUV()
        {
            float maxOffset = 1f - _zoom;
            _currentOffset.x = Mathf.Clamp(_currentOffset.x, 0f, maxOffset);
            _currentOffset.y = Mathf.Clamp(_currentOffset.y, 0f, maxOffset);
            _targetOffset.x = Mathf.Clamp(_targetOffset.x, 0f, maxOffset);
            _targetOffset.y = Mathf.Clamp(_targetOffset.y, 0f, maxOffset);

            _uvRect = new Rect(_currentOffset.x, _currentOffset.y, _zoom, _zoom);
            _worldImage.uvRect = _uvRect;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var sub in _subscriptions)
                sub?.Dispose();
            _subscriptions.Clear();
        }

        #endregion
    }
}   