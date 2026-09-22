using System;
using System.Collections.Generic;
using Game.Core.Interfaces;
using Game.Core.Messages;
using MessagePipe;
using TMPro;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Player
{
    public class PlayerStateDriverShell : MonoBehaviour, IStartable, IDisposable, IPlayerStateDriver
    {
        [Inject] private InputManager _inputManager;
        [Inject] private PlayerMovementStats _stats;

        Transform IPlayerStateDriver.Transform => transform;
        public bool IsDead => false;
        public float CurrentHealth => 1f;

        // NOT readonly — cached in Initialize()
        private IPublisher<PlayerFacingChanged> _facingChangedPublisher;
        private ISubscriber<BouncePlatformHit> _bounceHitSubscriber;
        private IPublisher<EntityHealthChanged> _healthPublisher;
        private IPublisher<PlayerDamaged> _damagedPublisher;
        private IPublisher<PlayerLanded> _landedPublisher;
        private IPublisher<PlayerJumped> _jumpedPublisher;
        private IPublisher<PlayerDoubleJumped> _doubleJumpedPublisher;

        private readonly List<IDisposable> _disposables = new();

        private PlayerController _controller;
        public PlayerController Controller => _controller;

        [SerializeField] private Collider2D _feetColl;
        [SerializeField] private Collider2D _bodyColl;
        [SerializeField] private ParticleSystem _dust;
        [SerializeField] private TextMeshProUGUI _debugText;
        [SerializeField] private bool _logStateChanges = true;
        [SerializeField] private bool _logIsGroundedChanges = true;

        [Header("Layers")]
        [SerializeField] private int _playerLayer;
        [SerializeField] private int _oneWayLayer;

        private string _lastStatePath;
        private Rigidbody2D _rb;
        private Animator _anim;
        private KnockBack _knockBack;
        private bool _disposed;

        public PlayerStateDriverShell() { }

        private void Awake()
        {
            if (_bodyColl != null && _feetColl != null)
                Physics2D.IgnoreCollision(_bodyColl, _feetColl, true);
        }

        [VContainer.Inject]
        private void Initialize()
        {
            if (_controller != null) return;

            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null)
                _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (_anim == null) _anim = GetComponentInChildren<Animator>();
            if (_knockBack == null) _knockBack = GetComponent<KnockBack>();

            // Cache publishers/subscribers HERE (SetProvider is done —
            // InjectGameObject is called by LevelLoader after scope build)
            _facingChangedPublisher = GlobalMessagePipe.GetPublisher<PlayerFacingChanged>();
            _bounceHitSubscriber = GlobalMessagePipe.GetSubscriber<BouncePlatformHit>();
            _healthPublisher = GlobalMessagePipe.GetPublisher<EntityHealthChanged>();
            _damagedPublisher = GlobalMessagePipe.GetPublisher<PlayerDamaged>();
            _landedPublisher = GlobalMessagePipe.GetPublisher<PlayerLanded>();
            _jumpedPublisher = GlobalMessagePipe.GetPublisher<PlayerJumped>();
            _doubleJumpedPublisher = GlobalMessagePipe.GetPublisher<PlayerDoubleJumped>();

            var deps = new PlayerDependencies
            {
                Rb = _rb,
                Anim = _anim,
                KnockBack = _knockBack,
                Dust = _dust,
                FeetColl = _feetColl,
                BodyColl = _bodyColl,
                Stats = _stats,
                PlayerLayer = _playerLayer,
                OneWayLayer = _oneWayLayer
            };

            var context = new PlayerContext
            {
                MoveStats = _stats,
                Rb = _rb,
                Anim = _anim,
                IsFacingRight = true
            };

            var (controller, machine) = new PlayerHSMBuilder().Build(
                context, deps, _inputManager,
                _landedPublisher, _jumpedPublisher, _doubleJumpedPublisher,
                _bounceHitSubscriber);

            _controller = controller;
            machine.Start();

            if (machine.Root is PlayerRoot playerRoot)
                playerRoot.EvaluateInitial();
        }

        void IStartable.Start()
        {
            // No subscriptions needed here — BouncePlatformHit is handled
            // inside PlayerController (which owns Ctx)
        }

        private void Update()
        {
            if (_controller == null) return;
            _controller.Tick(Time.deltaTime);
            TurnCheck(_controller.Ctx.MoveInput);

            string path = _controller.CurrentStatePath;
            if (_debugText != null && _debugText.text != path)
                _debugText.text = path;

            if (_logStateChanges && path != _lastStatePath)
            {
                _lastStatePath = path;
                Debug.Log($"[HSM] {path}", this);
            }

            bool grounded = _controller.Ctx.IsGrounded;
            if (_logIsGroundedChanges != grounded)
            {
                _logIsGroundedChanges = grounded;
                Debug.Log("[Shell] IsGrounded: " + _logIsGroundedChanges);
            }
        }

        private void FixedUpdate()
        {
            if (_controller == null) return;
            _controller.TickFixed(Time.fixedDeltaTime);
            _rb.linearVelocity = _controller.Ctx.Velocity;
            _controller.SyncFromRigidbody();
        }

        private void TurnCheck(Vector2 moveInput)
        {
            if (moveInput.x > 0f && !_controller.Ctx.IsFacingRight)
                Turn();
            else if (moveInput.x < 0f && _controller.Ctx.IsFacingRight)
                Turn();
        }

        private void Turn()
        {
            bool facingRight = _controller.Ctx.IsFacingRight;
            float targetYRotation = facingRight ? 180f : 0f;

            transform.rotation = Quaternion.Euler(0f, targetYRotation, 0f);
            _controller.Ctx.IsFacingRight = !facingRight;

            if (_facingChangedPublisher != null)
                _facingChangedPublisher.Publish(new PlayerFacingChanged(_controller.Ctx.IsFacingRight));
        }

        private void OnDestroy()
        {
            if (!_disposed) Dispose();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _controller?.Dispose();
            _controller = null;

            foreach (var d in _disposables) d?.Dispose();
            _disposables.Clear();
        }
    }
}   