using System;
using Game.Core.Interfaces;
using Game.Core.Messages;
using MessagePipe;
using TMPro;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Player
{
    public class PlayerStateDriverShell : MonoBehaviour, IDisposable, IPlayerStateDriver
    {
        #region Serialized Fields

        [SerializeField] private Collider2D _feetColl;
        [SerializeField] private Collider2D _bodyColl;
        [SerializeField] private ParticleSystem _dust;
        [SerializeField] private TextMeshProUGUI _debugText;
        [SerializeField] private bool _logStateChanges = true;
        [SerializeField] private bool _logIsGroundedChanges = true;

        [Header("Layers")]
        [SerializeField] private int _playerLayer;
        [SerializeField] private int _oneWayLayer;

        #endregion

        #region Dependencies

        private InputManager _inputManager;
        private PlayerMovementStats _stats;
        private IPublisher<PlayerFacingChanged> _facingChangedPublisher;
        private ISubscriber<BouncePlatformHit> _bounceHitSubscriber;
        private IPublisher<EntityHealthChanged> _healthPublisher;
        private IPublisher<PlayerDamaged> _damagedPublisher;
        private IPublisher<PlayerLanded> _landedPublisher;
        private IPublisher<PlayerJumped> _jumpedPublisher;
        private IPublisher<PlayerDoubleJumped> _doubleJumpedPublisher;
        private PlayerDependencies _deps;
        private PlayerContext _context;
        private PlayerHSMBuilder _hsmBuilder;

        #endregion

        #region State

        private PlayerController _controller;
        public PlayerController Controller => _controller;

        private string _lastStatePath;
        private Rigidbody2D _rb;
        private Animator _anim;
        private KnockBack _knockBack;
        private bool _disposed;

        #endregion

        #region IPlayerStateDriver

        Transform IPlayerStateDriver.Transform => transform;
        public bool IsDead => false;
        public float CurrentHealth => 1f;

        #endregion

        #region Construction

        [Inject]
        private void Initialize(
            InputManager inputManager,
            PlayerMovementStats stats,
            IPublisher<PlayerFacingChanged> facingChangedPublisher,
            ISubscriber<BouncePlatformHit> bounceHitSubscriber,
            IPublisher<EntityHealthChanged> healthPublisher,
            IPublisher<PlayerDamaged> damagedPublisher,
            IPublisher<PlayerLanded> landedPublisher,
            IPublisher<PlayerJumped> jumpedPublisher,
            IPublisher<PlayerDoubleJumped> doubleJumpedPublisher,
            PlayerDependencies deps,
            PlayerContext context,
            PlayerHSMBuilder hsmBuilder)
        {
            if (_controller != null) return;

            _inputManager = inputManager;
            _stats = stats;
            _facingChangedPublisher = facingChangedPublisher;
            _bounceHitSubscriber = bounceHitSubscriber;
            _healthPublisher = healthPublisher;
            _damagedPublisher = damagedPublisher;
            _landedPublisher = landedPublisher;
            _jumpedPublisher = jumpedPublisher;
            _doubleJumpedPublisher = doubleJumpedPublisher;
            _deps = deps;
            _context = context;
            _hsmBuilder = hsmBuilder;

            // --- Populate Rigidbody ---
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null)
                _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (_anim == null) _anim = GetComponentInChildren<Animator>();
            if (_knockBack == null) _knockBack = GetComponent<KnockBack>();

            // --- Populate scoped PlayerDependencies ---
            _deps.Rb = _rb;
            _deps.Anim = _anim;
            _deps.KnockBack = _knockBack;
            _deps.Dust = _dust;
            _deps.FeetColl = _feetColl;
            _deps.BodyColl = _bodyColl;
            _deps.Stats = _stats;
            _deps.PlayerLayer = _playerLayer;
            _deps.OneWayLayer = _oneWayLayer;

            // --- Populate scoped PlayerContext ---
            _context.MoveStats = _stats;
            _context.Rb = _rb;
            _context.Anim = _anim;
            _context.IsFacingRight = true;

            // --- Build HSM (controller has no side effects in ctor) ---
            var (controller, machine) = _hsmBuilder.Build(
                _context, _deps, _inputManager,
                _landedPublisher, _jumpedPublisher, _doubleJumpedPublisher,
                _bounceHitSubscriber);

            _controller = controller;
            machine.Start();

            if (machine.Root is PlayerRoot playerRoot)
                playerRoot.EvaluateInitial();

            // --- Reset physics NOW (deps fully populated) ---
            _controller.ResetPhysics();
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_bodyColl != null && _feetColl != null)
                Physics2D.IgnoreCollision(_bodyColl, _feetColl, true);
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

        private void OnDestroy()
        {
            if (!_disposed) Dispose();
        }

        #endregion

        #region Internal

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

            _facingChangedPublisher.Publish(new PlayerFacingChanged(_controller.Ctx.IsFacingRight));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _controller?.Dispose();
            _controller = null;
        }

        #endregion
    }
}