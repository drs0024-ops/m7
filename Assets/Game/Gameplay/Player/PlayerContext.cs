using UnityEngine;

namespace Game.Gameplay.Player
{
    public class PlayerContext
    {
        #region Core References
        public PlayerMovementStats MoveStats;
        public Rigidbody2D Rb;
        public Animator Anim;
        #endregion

        #region Movement State
        public bool RbIsStationary;
        public Vector2 MoveInput;
        public Vector2 Velocity;
        public bool IsGrounded;
        public bool RunIsHeld;
        public bool IsFacingRight = true;
        public bool IsBeingKnockedBack;
        public bool JumpIsHeld;
        public bool IsTouchingWallLeft;
        public bool IsTouchingWallRight;
        #endregion

        #region Jump State
        public bool JumpPressed;
        public bool JumpWasReleased;
        public bool IsJumping;
        public float CoyoteTimer;
        public float JumpBufferTimer;
        public int JumpsUsed;
        public bool BumpedHead;
        public bool DownWasPressed;
        #endregion

        #region Apex & Fall State
        public float ApexPoint;
        public float TimePastApexThreshold;
        public bool IsPastApexThreshold;
        public bool IsFalling;
        public bool IsFastFalling;
        public float FastFallTime;
        public float FastFallReleaseSpeed;
        #endregion

        #region Effects
        public bool PlayDust;
        public bool IsBouncing;
        public bool IsOnBounceObject;
        public bool PowerJump;
        public float PowerJumpMultiplier = 2f;
        #endregion

        public bool LastOneWayIgnoreState;

        #region Movement Logic
        public void Move(float accel, float decel, Vector2 input, float dt)
        {
            float maxSpeed = RunIsHeld ? MoveStats.MaxRunSpeed : MoveStats.MaxWalkSpeed;
            Vector2 target = input != Vector2.zero ? new Vector2(input.x * maxSpeed, 0) : Vector2.zero;
            float lerp = (input != Vector2.zero) ? accel : decel;
            Velocity = new Vector2(Mathf.Lerp(Velocity.x, target.x, lerp * dt), Velocity.y);
        }
        #endregion

        #region Jump Logic
        public void InitiateJump(int count)
        {
            IsJumping = true;
            JumpsUsed += count;
            JumpBufferTimer = 0f;
            IsFastFalling = false;

            if (PowerJump)
            {
                Velocity = new Vector2(Velocity.x, MoveStats.InitialJumpVelocity * PowerJumpMultiplier);
                IsBouncing = false;
                PowerJump = false;
                IsOnBounceObject = false;
            }
            else
            {
                Velocity = new Vector2(Velocity.x, MoveStats.InitialJumpVelocity);
            }
        }
        #endregion
    }
}   