using UnityEngine;

namespace Game.Gameplay.Enemies
{
    
    
	[CreateAssetMenu(fileName = "Enemy StatsBasceSO", menuName = "Enemy StatsBaseSO")]
	[SelectionBase]

	public class EnemyStatsBase : ScriptableObject {
		protected Enemy enemy;
		protected Transform transform;
		protected GameObject gameObject;
		protected EnemyAttack Attack;
		
		public virtual void Initialze(GameObject gameObject, Enemy enemy)
		{
			this.gameObject = gameObject;
			transform = gameObject.transform;
			this.enemy = enemy;
		}
		
		[Header("Enemy Stats")]
		public string enemyName;
		public string description;
		public int health = 100;
		public float maxHealth;
		public float detectRange = 10f;
		public float damageAmount = 10;
		//public float attackTime = 0.15f;
		public int HitsAllowed = 3;
		public float timeBetweenHits = 0.15f;
		public int coolDownTime = 2;
		
		public float attackRange = 1.5f;
		
		public LayerMask attackableLayer;
		

		[Header("Walk and Run")]
		[Range(1f, 100f)] public float MaxWalkSpeed = 12.5f;
		[Range(1f, 100f)] public float MaxRunSpeed = 20f;
		[Range(0.25f, 50f)] public float GroundAcceleration = 5f;
		[Range(0.25f, 50f)] public float GroundDeceleration = 20f;
		[Range(0.25f, 50f)] public float AirAcceleration = 5f;
		[Range(0.25f, 50f)] public float AirDeceleration = 5f;

		[Header("Grounded/Collision Checks")]
		public LayerMask GroundLayer;
		public LayerMask WallLayer;
		public float GroundDetectionRayLenth = 0.02f;
		public float HeadDetectionRayLenth = 0.02f;
		public float WallDetectionRayLenth = 0.02f;
		[Range(0f, 1f)] public float HeadWidth = 0.75f;

		[Header("Jump")]
		public float JumpHeight = 6.5f;
		[Range(1f, 1.1f)] public float JumpHeightCompensationFactor = 1.054f;
		public float TimeTillJumpApex = 0.35f;
		[Range(0.01f, 5f)] public float GravityOnReleaseMultiplier = 2f;
		public float MaxFallSpeed = 26f;
		[Range(1, 5)] public int NumberOfJumpsAllowed = 2;

		[Header("Jump Cut")]
		[Range(0.02f, 0.3f)] public float TimeForUpwardsCancel = 0.027f;

		[Header("JumpApex")]
		[Range(0.5f, 1f)] public float ApexThreshold = 0.97f;
		[Range(0.01f, 1f)] public float ApexHangTime = 0.75f;

		[Header("Jump Buffer")]
		[Range(0f, 1f)] public float JumpBufferTime = 0.125f;

		[Header("Jump Coyote Time")]
		[Range(0f, 1f)] public float JumpCoyoteTime = 0.1f;

		[Header("Debug")]
		public bool DebugShowIsGroundedBox;
		public bool DebugShowHeadBumpBox;

		[Header("JumpVisualization Tool")]
		public bool ShowWalkJumpArc = false;
		public bool ShowRunJumpArc = false;
		public bool StopOnCollion = true;
		public bool DrawRight = true;
		[Range(5, 100)] public int ArcResolution = 20;
		[Range(0, 500)] public int VisualizationSteps = 90;

		public float Gravity { get; private set; }

		public float InitialJumpVelocity { get; private set; }

		public float AdjustedJumpHeight { get; private set; }   

		public virtual void OnEnemySpawn() {}
		public virtual void PreformAbillity() {}
		
		private void OnValidate()
		{
			CalulateValues();
		}

		private void OnEnable()
		{
			CalulateValues(); 
		}

		private void CalulateValues()
		{
			AdjustedJumpHeight = JumpHeight * JumpHeightCompensationFactor;
			Gravity = -(2f * AdjustedJumpHeight) / Mathf.Pow(TimeTillJumpApex, 2f);
			InitialJumpVelocity = Mathf.Abs(Gravity) * TimeTillJumpApex; 
		}
	}
}





