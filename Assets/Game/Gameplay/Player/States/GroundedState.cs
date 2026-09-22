// Gameplay/Player/States/GroundedState.cs
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using Game.Core.StateMachine;

namespace Game.Gameplay.Player
{
    public class GroundedState : PlayerBaseState
    {
        public readonly IdleState Idle;
        public readonly WalkState Walk;
        public readonly RunState Run;

        private readonly IPublisher<PlayerLanded> _landedPublisher;

        public GroundedState(State parent, PlayerController driver, InputManager input,
            IPublisher<PlayerLanded> landedPublisher)
            : base(parent, driver, input)
        {
            _landedPublisher = landedPublisher;
            Idle = new IdleState(this, driver, input);
            Walk = new WalkState(this, driver, input);
            Run = new RunState(this, driver, input);
        }

        protected override State GetInitialState() => Idle;

        protected override void OnEnter()
        {
            if (Ctx.Velocity.y < -0.5f)
            {
                Ctx.Velocity = new Vector2(Ctx.Velocity.x, 0f);
                _landedPublisher.Publish(PlayerLanded.Default);
            }

            Ctx.IsGrounded = true;
            Ctx.JumpsUsed = 0;
            Ctx.IsFalling = false;
            Ctx.IsJumping = false;
            Ctx.IsFastFalling = false;
            Ctx.JumpBufferTimer = 0f;
            Ctx.CoyoteTimer = 0f;
        }

        protected override void OnFixedUpdate(float deltaTime)
        {
            if (Pc.IsBeingKnockedBack()) return;

            Ctx.Velocity.y = Ctx.MoveStats.GroundSnapVelocity;

            bool isGroundedNow = CheckGrounded();
            bool shouldIgnore = Ctx.MoveInput.y < 0f || Ctx.Velocity.y > 0.1f;
            IgnoreOneWay(shouldIgnore);

            if (isGroundedNow)
            {
                Ctx.Move(Ctx.MoveStats.GroundAcceleration, Ctx.MoveStats.GroundDeceleration, Ctx.MoveInput, deltaTime);
                Ctx.PlayDust = !Ctx.RbIsStationary;
            }
            else
            {
                Ctx.PlayDust = false;
            }
        }

        protected override State GetTransition()
        {
            if (!CheckGrounded())
            {
                Ctx.CoyoteTimer = Ctx.MoveStats.JumpCoyoteTime;
                return ((PlayerRoot)Parent).Airborne;
            }

            if (Ctx.JumpPressed)
            {
                Ctx.JumpBufferTimer = Ctx.MoveStats.JumpBufferTime;
                Ctx.CoyoteTimer = Ctx.MoveStats.JumpCoyoteTime;
                return ((PlayerRoot)Parent).Airborne;
            }

            return null;
        }
    }
}

// --- Sub-States for Grounded ---

/// <summary>
/// Idle sub-state.
/// </summary>
namespace Game.Gameplay.Player
{
    public class IdleState : PlayerBaseState
    {
        private const float INPUT_DEADZONE = 0.05f;

        public IdleState(State parent, PlayerController driver, InputManager input)
            : base(parent, driver, input) { }

        protected override void OnEnter()
        {
            Ctx.PlayDust = false;
        }

        protected override State GetTransition()
        {
            if (Mathf.Abs(Ctx.MoveInput.x) > INPUT_DEADZONE)
                return Ctx.RunIsHeld ? ((GroundedState)Parent).Run : ((GroundedState)Parent).Walk;

            return null;
        }

        protected override void OnExit()
        {
            Ctx.PlayDust = false;
        }
    }
}

namespace Game.Gameplay.Player
{
    public class WalkState : PlayerBaseState
    {
        private const float INPUT_DEADZONE = 0.05f;

        public WalkState(State parent, PlayerController driver, InputManager input)
            : base(parent, driver, input) { }

        protected override void OnEnter()
        {
            Ctx.PlayDust = true;
        }

        protected override State GetTransition()
        {
            if (Ctx.RunIsHeld && Mathf.Abs(Ctx.MoveInput.x) > INPUT_DEADZONE)
                return ((GroundedState)Parent).Run;

            if (Ctx.RbIsStationary || Mathf.Abs(Ctx.MoveInput.x) < INPUT_DEADZONE)
                return ((GroundedState)Parent).Idle;

            return null;
        }

        protected override void OnExit()
        {
            Ctx.PlayDust = false;
        }
    }
}

namespace Game.Gameplay.Player
{
    public class RunState : PlayerBaseState
    {
        private const float INPUT_DEADZONE = 0.05f;

        public RunState(State parent, PlayerController driver, InputManager input)
            : base(parent, driver, input) { }

        protected override void OnEnter()
        {
            Ctx.PlayDust = true;
        }

        protected override State GetTransition()
        {
            if (!Ctx.RunIsHeld && Mathf.Abs(Ctx.MoveInput.x) > INPUT_DEADZONE)
                return ((GroundedState)Parent).Walk;

            if (Ctx.RbIsStationary || Mathf.Abs(Ctx.MoveInput.x) < INPUT_DEADZONE)
                return ((GroundedState)Parent).Idle;

            return null;
        }

        protected override void OnExit()
        {
            Ctx.PlayDust = false;
        }
    }
}