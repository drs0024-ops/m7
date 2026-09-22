using System;
using System.Collections.Generic;
using Game.Core.StateMachine;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using System.Threading;

namespace Game.Gameplay.Player
{
    public class PlayerController : IDisposable
    {
        public readonly PlayerContext Ctx;
        public readonly StateMachine Machine;

        private readonly PlayerDependencies _deps;
        private readonly InputManager _input;
        private readonly CancellationTokenSource _cts;
        private readonly List<IDisposable> _subscriptions = new();

        public CancellationToken CancellationToken => _cts.Token;

        public PlayerController(
            PlayerContext ctx,
            PlayerDependencies deps,
            InputManager input,
            IPublisher<PlayerLanded> landedPublisher,
            IPublisher<PlayerJumped> jumpedPublisher,
            IPublisher<PlayerDoubleJumped> doubleJumpedPublisher,
            ISubscriber<BouncePlatformHit> bounceHitSubscriber)
        {
            Ctx = ctx;
            _deps = deps;
            _input = input;
            _cts = new CancellationTokenSource();

            _deps.Rb.linearVelocity = Vector2.zero;
            _deps.Rb.angularVelocity = 0f;

            _subscriptions.Add(bounceHitSubscriber.Subscribe(OnBouncePlatformHit));

            var root = new PlayerRoot(this, input, landedPublisher, jumpedPublisher, doubleJumpedPublisher);
            Machine = new StateMachineBuilder(root).Build();
        }

        public string CurrentStatePath
        {
            get
            {
                if (Machine?.Root == null) return "None";
                var parts = new List<string>(8);
                for (var s = Machine.Root; s != null; s = s.ActiveChild)
                    parts.Add(s.GetType().Name);
                return string.Join(" > ", parts);
            }
        }

        public void Tick(float deltaTime)
        {
            UpdateInputSnapshot();
            Machine.Tick(deltaTime);
        }

        public void TickFixed(float deltaTime)
        {
            Machine.TickFixedUpdate(deltaTime);

            if (Ctx.PlayDust && _deps.Dust != null && !_deps.Dust.isEmitting)
                _deps.Dust.Stop();
        }

        public void SyncFromRigidbody()
        {
            Ctx.Velocity = _deps.Rb.linearVelocity;
            Ctx.RbIsStationary = Ctx.Velocity.sqrMagnitude < 0.01f;
            Ctx.IsFalling = Ctx.Velocity.y < 0f;
        }

        private void UpdateInputSnapshot()
        {
            Ctx.MoveInput = _input.Movement;
            Ctx.JumpPressed = _input.JumpWasPressed;
            Ctx.JumpIsHeld = _input.JumpIsHeld;
            Ctx.RunIsHeld = _input.RunIsHeld;
            Ctx.DownWasPressed = _input.DownWasPressed;
            Ctx.IsBeingKnockedBack = _deps.KnockBack != null && _deps.KnockBack.IsBeingKnockedBack;
        }

        private void OnBouncePlatformHit(BouncePlatformHit message)
        {
            if (message.Player != _deps.Rb.transform) return;

            if (Ctx.IsBouncing)
            {
                Ctx.PowerJump = true;
                Ctx.PowerJumpMultiplier = _deps.Stats.PowerJumpMultiplier;
            }
        }

        public Collider2D GetFeetCollider() => _deps.FeetColl;
        public Collider2D GetBodyCollider() => _deps.BodyColl;
        public ParticleSystem GetDust() => _deps.Dust;
        public int GetPlayerLayer() => _deps.PlayerLayer;
        public int GetOneWayLayer() => _deps.OneWayLayer;
        public bool IsBeingKnockedBack() => _deps.KnockBack != null && _deps.KnockBack.IsBeingKnockedBack;

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();

            foreach (var d in _subscriptions) d?.Dispose();
            _subscriptions.Clear();
        }
    }
}   