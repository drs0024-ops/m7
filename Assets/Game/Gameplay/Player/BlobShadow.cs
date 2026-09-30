using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Game.Core.Messages;
using MessagePipe;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Soft ellipse shadow that tracks the player's ground contact.
    /// Tilts to match ground angle. Clips at walls. Shrinks/fades on jump.
    /// Squashes on landing. Stays visible when player is invisible.
    /// </summary>
    public class BlobShadow : MonoBehaviour, IStartable, IDisposable
    {
        #region Constants

        private const float SQUASH_DURATION = 0.12f;
        private const float SQUASH_X = 1.4f;
        private const float SQUASH_Y = 0.5f;
        private static readonly int ShadowOpacityId = Shader.PropertyToID("_ShadowOpacity");

        #endregion

        #region Serialized

        [Header("Opacity")]
        [Tooltip("Opacity when player is fully visible.")]
        [SerializeField, Range(0f, 1f)] private float _visibleOpacity = 0.4f;
        [Tooltip("Opacity when player is fully invisible.")]
        [SerializeField, Range(0f, 1f)] private float _invisibleOpacity = 0.15f;

        [Header("Height")]
        [Tooltip("Shadow starts fading at this height above ground.")]
        [SerializeField] private float _fadeStartHeight = 1.0f;
        [Tooltip("Shadow fully invisible at this height.")]
        [SerializeField] private float _maxHeight = 3.0f;
        [Tooltip("Minimum scale at max height.")]
        [SerializeField, Range(0f, 1f)] private float _minScale = 0.3f;

        [Header("Raycast")]
        [Tooltip("Downward raycast length to find ground.")]
        [SerializeField] private float _raycastLength = 4.0f;
        [Tooltip("Half-width of shadow for wall clipping.")]
        [SerializeField] private float _shadowHalfWidth = 0.8f;

        [Header("Layers")]
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private LayerMask _wallLayer;

        [Header("Squash")]
        [SerializeField, Range(0f, 1f)] private float _squashAmount = 0.4f;

        #endregion

        #region State

        private SpriteRenderer _shadowRenderer;
        private Material _shadowMaterial;
        private ISubscriber<InvisibilityStateChanged> _invisibilitySub;
        private ISubscriber<PlayerLanded> _landedSub;
        private ISubscriber<PlayerSpawned> _spawnedSub;
        private readonly List<IDisposable> _subscriptions = new(3);

        private Transform _playerTransform;
        private float _currentCloakFactor;
        private float _squashTimer;
        private Vector3 _lastShadowScale = Vector3.one;

        private bool _disposed;

        #endregion

        #region DI

        [Inject]
        private void Inject(
            ISubscriber<InvisibilityStateChanged> invisibilitySub,
            ISubscriber<PlayerLanded> landedSub,
            ISubscriber<PlayerSpawned> spawnedSub)
        {
            _invisibilitySub = invisibilitySub;
            _landedSub = landedSub;
            _spawnedSub = spawnedSub;
        }

        #endregion

        #region IStartable

        void IStartable.Start()
        {
            _shadowRenderer = GetComponent<SpriteRenderer>();
            if (_shadowRenderer == null)
            {
                Debug.LogError("[BlobShadow] No SpriteRenderer found on BlobShadow GO.");
                return;
            }

            _shadowMaterial = new Material(_shadowRenderer.sharedMaterial);
            _shadowRenderer.material = _shadowMaterial;

            _subscriptions.Add(_invisibilitySub.Subscribe(OnInvisibilityChanged));
            _subscriptions.Add(_landedSub.Subscribe(OnLanded));
            _subscriptions.Add(_spawnedSub.Subscribe(OnPlayerSpawned));
        }

        #endregion

        #region Message Handlers

        private void OnPlayerSpawned(PlayerSpawned message)
        {
            if (message.Player != null)
                _playerTransform = message.Player;
        }

        private void OnInvisibilityChanged(InvisibilityStateChanged state)
        {
            _currentCloakFactor = state.TargetFactor;
        }

        private void OnLanded(PlayerLanded _)
        {
            _squashTimer = SQUASH_DURATION;
        }

        #endregion

        #region Update

        private void Update()
        {
            if (_shadowRenderer == null || _shadowMaterial == null) return;
            if (_playerTransform == null)
            {
                _shadowRenderer.enabled = false;
                return;
            }

            // 1. Find ground
            var groundHit = Physics2D.Raycast(
                _playerTransform.position, Vector2.down, _raycastLength, _groundLayer);

            if (!groundHit)
            {
                _shadowRenderer.enabled = false;
                return;
            }

            _shadowRenderer.enabled = true;

            float heightAboveGround = groundHit.distance;
            Vector2 groundNormal = groundHit.normal;

            // 2. Position shadow at ground hit
            transform.position = (Vector3)groundHit.point + (Vector3)groundNormal * 0.02f;   

            // 3. Tilt to ground angle
            float angle = Mathf.Atan2(groundNormal.x, groundNormal.y) * Mathf.Rad2Deg;
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            // 4. Height scale + fade
            float heightT = Mathf.Clamp01(
                (heightAboveGround - _fadeStartHeight) / (_maxHeight - _fadeStartHeight));
            float heightScale = Mathf.Lerp(1f, _minScale, heightT);
            float heightFade = 1f - heightT;

            // 5. Wall clipping
            float clipScale = ComputeWallClip(groundNormal);

            // 6. Squash
            float squashX = 1f, squashY = 1f;
            if (_squashTimer > 0f)
            {
                _squashTimer -= Time.deltaTime;
                float t = 1f - Mathf.Max(0f, _squashTimer / SQUASH_DURATION);
                squashX = Mathf.Lerp(1f + _squashAmount, 1f, t);
                squashY = Mathf.Lerp(1f - _squashAmount, 1f, t);
            }

            // 7. Final scale
            float finalScaleX = heightScale * clipScale * squashX;
            float finalScaleY = heightScale * squashY;
            transform.localScale = new Vector3(finalScaleX, finalScaleY, 1f);

            // 8. Opacity
            float cloakOpacity = Mathf.Lerp(_visibleOpacity, _invisibleOpacity, _currentCloakFactor);
            float finalOpacity = cloakOpacity * heightFade;
            _shadowMaterial.SetFloat(ShadowOpacityId, finalOpacity);
        }

        #endregion

        #region Wall Clipping

        private float ComputeWallClip(Vector2 groundNormal)
        {
            // Cast left and right from shadow position
            Vector2 shadowPos = transform.position;
            float leftDist = _shadowHalfWidth;
            float rightDist = _shadowHalfWidth;

            var leftHit = Physics2D.Raycast(shadowPos, Vector2.left, _shadowHalfWidth, _wallLayer);
            if (leftHit)
                leftDist = leftHit.distance;

            var rightHit = Physics2D.Raycast(shadowPos, Vector2.right, _shadowHalfWidth, _wallLayer);
            if (rightHit)
                rightDist = rightHit.distance;

            float totalWidth = leftDist + rightDist;
            float fullWidth = _shadowHalfWidth * 2f;

            return Mathf.Clamp01(totalWidth / fullWidth);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var s in _subscriptions) s?.Dispose();
            _subscriptions.Clear();
        }

        private void OnDestroy()
        {
            Dispose();
            if (_shadowMaterial != null)
                Destroy(_shadowMaterial);
        }

        #endregion
    }
}   