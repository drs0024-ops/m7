using UnityEngine;

namespace Game.Gameplay.Player
{
    [CreateAssetMenu(menuName = "Player/Movement Stats")]
    public class PlayerMovementStats : ScriptableObject
    {
        #region Walk & Run
        [Header("Walk")]
        [Range(1f, 100f)] public float MaxWalkSpeed = 12.5f;
        [Range(0.25f, 50f)] public float GroundAcceleration = 5f;
        [Range(0.25f, 50f)] public float GroundDeceleration = 20f;
        [Range(0.25f, 50f)] public float AirAcceleration = 5f;
        [Range(0.25f, 50f)] public float AirDeceleration = 5f;

        [Header("Run")]
        [Range(1f, 100f)] public float MaxRunSpeed = 20f;
        #endregion

        #region Collision Checks
        [Header("Grounded/Collision Checks")]
        public LayerMask GroundLayer;
        public LayerMask WallLayer;
        public LayerMask OneWayPlatform;

        // ✅ INCREASED from 0.02f to 0.15f
        public float GroundDetectionRayLength = 0.15f;
        public float HeadDetectionRayLength = 0.15f;
        public float WallDetectionRayLength = 0.05f;
        [Range(0f, 1f)] public float HeadWidth = 0.75f;

        // ✅ Small downward nudge to keep grounded check reliable
        [Tooltip("Small downward nudge to keep grounded check reliable")]
        public float GroundSnapVelocity = -0.2f;
        #endregion

        #region Jump Physics
        [Header("Jump")]
        public float JumpHeight = 6.5f;
        [Range(1f, 1.1f)] public float JumpHeightCompensationFactor = 1.054f;
        public float TimeTillJumpApex = 0.35f;
        [Range(0.01f, 5f)] public float GravityOnReleaseMultiplier = 2f;
        public float MaxFallSpeed = 26f;
        [Range(1, 5)] public int NumberOfJumpsAllowed = 2;
        public float PowerJumpMultiplier = 2.5f;

        [Header("Jump Cut")]
        [Range(0.02f, 0.3f)] public float TimeForUpwardsCancel = 0.027f;

        [Header("Jump Apex")]
        [Range(0.5f, 1f)] public float ApexThreshold = 0.97f;
        [Range(0.01f, 1f)] public float ApexHangTime = 0.075f;
        #endregion

        #region Timers
        [Header("Jump Buffer")]
        [Range(0f, 1f)] public float JumpBufferTime = 0.125f;

        [Header("Jump Coyote Time")]
        [Range(0f, 1f)] public float JumpCoyoteTime = 0.1f;
        #endregion

        #region Derived Values
        public float Gravity { get; private set; }
        public float InitialJumpVelocity { get; private set; }
        public float AdjustedJumpHeight { get; private set; }
        public float MinJumpHeight { get; private set; }
        #endregion

        #region Initialization
        private void OnValidate() => CalculateValues();
        private void OnEnable() => CalculateValues();

        private void CalculateValues()
        {
            MinJumpHeight = 1f;
            AdjustedJumpHeight = JumpHeight * JumpHeightCompensationFactor;
            Gravity = -(2f * AdjustedJumpHeight) / Mathf.Pow(TimeTillJumpApex, 2f);
            InitialJumpVelocity = Mathf.Abs(Gravity) * TimeTillJumpApex;
        }
        #endregion
    }
}
