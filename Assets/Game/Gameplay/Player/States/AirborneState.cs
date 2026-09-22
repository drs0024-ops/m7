using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using Game.Core.StateMachine;

namespace Game.Gameplay.Player
{
    public class AirborneState : PlayerBaseState
    {
        private const float MIN_FALL_KICK = -2f;

        private readonly IPublisher<PlayerJumped> _jumpedPublisher;
        private readonly IPublisher<PlayerDoubleJumped> _doubleJumpedPublisher;

        public AirborneState(State parent, PlayerController pc, InputManager input,
            IPublisher<PlayerJumped> jumpedPublisher,
            IPublisher<PlayerDoubleJumped> doubleJumpedPublisher)
            : base(parent, pc, input)
        {
            _jumpedPublisher = jumpedPublisher;
            _doubleJumpedPublisher = doubleJumpedPublisher;
        }

        protected override void OnEnter()
        {
            Ctx.IsGrounded = false;
            Ctx.IsJumping = true;
            Ctx.IsFalling = false;
            Ctx.FastFallTime = 0f;
            Ctx.IsFastFalling = false;
        }

        protected override void OnFixedUpdate(float deltaTime)
        {
            if (Pc.IsBeingKnockedBack()) return;

            if (Ctx.IsFastFalling)
                HandleFastFall(deltaTime);
            else
                Ctx.Velocity.y += Ctx.MoveStats.Gravity * deltaTime;

            ClampVelocity();
        }

        protected override State GetTransition()
        {
            if (CheckGrounded())
                return ((PlayerRoot)Parent).Grounded;
            return null;
        }

        protected override void OnUpdate(float deltaTime)
        {
            HandleJumpRelease();

            if (Ctx.JumpPressed)
            {
                Ctx.JumpBufferTimer = Ctx.MoveStats.JumpBufferTime;
                AttemptJump();
            }
        }

        #region Logic

        private void HandleFastFall(float dt)
        {
            if (Ctx.FastFallTime >= Ctx.MoveStats.TimeForUpwardsCancel)
            {
                Ctx.Velocity.y += Ctx.MoveStats.Gravity * Ctx.MoveStats.GravityOnReleaseMultiplier * dt;
            }
            else
            {
                Ctx.Velocity.y = Mathf.Lerp(Ctx.FastFallReleaseSpeed, MIN_FALL_KICK,
                    Ctx.FastFallTime / Ctx.MoveStats.TimeForUpwardsCancel);
            }
            Ctx.FastFallTime += dt;
        }

        private void ClampVelocity()
        {
            float maxFall = -Ctx.MoveStats.MaxFallSpeed;
            Ctx.Velocity.y = Mathf.Clamp(Ctx.Velocity.y, maxFall, Ctx.MoveStats.MaxRunSpeed);
        }

        private void HandleJumpRelease()
        {
            if (!Ctx.JumpWasReleased) return;
            if (Ctx.IsJumping && Ctx.Velocity.y > 0f)
            {
                if (Ctx.IsPastApexThreshold)
                {
                    Ctx.IsPastApexThreshold = false;
                    Ctx.IsFastFalling = true;
                    Ctx.FastFallTime = 0f;
                    Ctx.Velocity.y = MIN_FALL_KICK;
                }
                else
                {
                    Ctx.IsFastFalling = true;
                    Ctx.FastFallReleaseSpeed = Ctx.Velocity.y;
                }
            }
        }

        private void AttemptJump()
        {
            if (Ctx.JumpBufferTimer <= 0f) return;

            if ((Ctx.IsGrounded || Ctx.CoyoteTimer > 0f) && !Ctx.IsJumping)
            {
                Ctx.InitiateJump(1);
                _jumpedPublisher.Publish(new PlayerJumped(1));
                return;
            }

            if (Ctx.IsJumping && Ctx.JumpsUsed < Ctx.MoveStats.NumberOfJumpsAllowed)
            {
                Ctx.InitiateJump(1);
                _jumpedPublisher.Publish(new PlayerJumped(Ctx.JumpsUsed + 1));
                return;
            }

            if (Ctx.IsFalling && Ctx.JumpsUsed < Ctx.MoveStats.NumberOfJumpsAllowed - 1)
            {
                Ctx.InitiateJump(2);
                Ctx.IsFalling = false;
                _doubleJumpedPublisher.Publish(PlayerDoubleJumped.Default);
            }
        }

        #endregion
    }
}   