using System.Linq;
using System.Collections;
using UnityEngine;
using Game.Core.Interfaces;
using Game.Core.StateMachine;
using System;
using Game.Gameplay.Player;

namespace Game.Gameplay.Enemies
{

public class Enemy : MonoBehaviour, IDamagable, IEnemyMoveable, ITriggerCheckable
{

    public EnemyAttack AttackComponent { get; private set; }

    // In Enemy.cs
    public bool IsCoolingDown => AttackComponent != null && !AttackComponent.CanAttack;   

    //[SerializeField] public PlayerStateDriver player;
    public bool _gizmosLable;
    public Transform PlayerTransform;
    //[SerializeField] private GameObject enemyPrefab;
    [SerializeField] private EnemyStatsBase EnemyStats;
    public EnemyStatsBase EnemyStatsBaseInstance {get; set;}

    private EnemyRoot _root;
    private StateMachine _machine;

#region Behavior SO's
    [Header("Enemy Behavior SO's")]
    [SerializeField] private EnemyIdleSOBase EnemyIdleBase;
    [SerializeField] private EnemyChaseSOBase EnemyChaseBase;
    [SerializeField] private EnemyAttackSOBase EnemyAttackBase;

    public EnemyIdleSOBase EnemyIdleBaseInstance { get; set; }
    public EnemyChaseSOBase EnemyChaseBaseInstance { get; set; }
    public EnemyAttackSOBase EnemyAttackBaseInstance { get; set; }

    #endregion

    public bool HasTakenDamage { get; set; }
    [field: SerializeField] public float Maxhealth { get; set; } = 100f;
    private float damageAmount;
    public float CurrentHealth { get; set; }
    public Rigidbody2D RB { get; set; }
    public Vector2 Velocity { get; set;}
    public float VerticalVelocity { get; set; }
    public bool IsFacingRight { get; set; } = true;
    public bool IsGrounded { get { return _isGrounded; } }
    public bool IsAggroed { get; set; }
    public bool IsWithinStrickingDistance { get; set; }
    
    //[SerializeField] private Collider2D parentCollider;
    [Header("Colliders")]
    
    [SerializeField] private Collider2D _ceilingCheck;
    [SerializeField] private Collider2D _groundCheck;
    [SerializeField] private Collider2D _knockBackCheck;
    //[SerializeField] private Collider2D _aggroed;
    //[SerializeField] private Collider2D _strikingDistance;

    [SerializeField] GameObject collectablePrefab;

    
    //public Transform groundCheck;
    //public float speed = 2f;
    //public bool drawGizmos = true;
    //public float groundRadius = 0.2f;
    public LayerMask groundMask;
    string lastPath;
    private Vector2 _moveInput;

    [Header("Way Point Data")]
    public Transform[] waypoints; // Assign waypoints in Inspector
    public int currentWaypointIndex = 0;
    //public float _MovementSpeed = 5f;
    public Transform startingPoint;
    public GameObject pointA;
    public GameObject pointB;

    //public Transform currentPoint;
    //float waitTime = 2f;
    private Vector2 _MoveVolocity {get; set; }
    public Vector2 targetPosition;
    //public float smoothTime = 2f;

    // Collision checks
    
    private bool _isGrounded;
    //collision check vars
    private RaycastHit2D _groundHit;
    private RaycastHit2D _ceilingCheckHit;
    private RaycastHit2D _wallHitLeft;
    private RaycastHit2D _wallHitRitght;
    bool _hitWallLeft = false;
    bool _hitWallRight = false;
    bool _wallHit = false;
    private bool stopOnwallCallLeft = false;
    private bool stopOnwallCallRight = false;
    private bool _isOnBouncePlatfom;

    
    //Property with a setter that triggers the event
    private bool _hitWall;
    private bool _onWall;
    public event Action<bool> OnWallHitChanged;
    public bool OnWall
    {
        get { return _onWall; }
        set
        {
            if (_onWall != value) // Optional: only fire if value actually changed
            {
                _onWall = value;
                OnWallHitChanged?.Invoke(_onWall); // Invoke event with new value
                Debug.Log("OnWall Changed");
            }
        }
    }

    StateMachine machine;
    public State root;
    //public bool WayPointWander {get; set;}
    private float waitTimer = 0f;
    private bool isWaiting = false;

    //public bool jumpPressed;
    private bool JumpWasPressed;
    private bool JumpWasReleased;
    private bool IsJumping;
    private float JumpBufferTimer;
    private bool JumpReleaseDuringBuffer;
    private int NumberOfJumpsUsed;
    private float CoyoteTimer;
    private bool BumpedHead;
    private bool CeilingHit;

    //apex vars
    private float ApexPoint;
    private float TimePastApexThreshold;
    private bool IsPastApexThreshold;
    private bool IsFalling;
    private bool IsFastFalling;
    private float FastFallTime;
    private float FastFallReleaseSpeed;
    private KnockBack knockBack;
    private bool IsBeingKnockedBack;
    //private StatesESM statesESM;
    private State currentState;
    private State newState;

    public bool IsAttacking {get; private set;} = false;
    
    // Rigidbody vars
    public float stationaryThreshold = 0.01f;
    private bool rbsStationary;

    //public float pauseDuration = 2f;
    private bool ShouldMove {get; set; } = true;
    public float smoothTime = 0.1f;
    public bool IsPatrolling {get; set; } = false;
    private bool pausedAtWayPoint {get; set; } = false;
    private bool isMovingToWayPoint {get; set;} = false;
    private Vector3 previousPosition;
    private bool isChasingPlayer {get; set;} = false;
    private float currentSpeed = 0f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private float timer;
    public float duration = 2f;
    public bool playerPressent {get; set; } = false;
    private bool stop {get; set;} = false;
    private float attackTimeCounter = 0f;
    //private float timeBetweenAttacks = 0.15f;
    private IDamagable iDamageable;
    public bool ShouldBeDamaging {get; private set;} = false;
    private int coolDownTime {get; set;} = 1;
    public bool IsCoollingDown {get; set; } = false;
    private float totalTime {get; set; } = 0f;
    private bool timesUp = false;
    private int hitsUsed = 0;

    private GameObject player;
    private Collider2D playerFootCollider;
    private Vector3 lastPosition;
    private float movementThreshold = 0.001f;
    private Vector3 targetVelocity = Vector3.zero;
    public float decelerationFactor = 0.95f;
    public Vector2 MoveVolocity;

    [SerializeField] private float easeInEaseOutDuration = 2f;
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    private float startTime;
    private bool isMoving = false;
    private float journeyLength;
    private Vector2 currentPosition;
    private Vector2 newPosition;
    float easedProgress;
    public Vector2 currentVelocity;
    private Vector2 lastVelocity;
    private EnemyCollisionManager ECM;
    public bool IsChasing {get; set;} = false;
    public bool Chasing
    {
        get { return IsChasing; }
        set { IsChasing = value; }
    }
    private bool _shouldBeSeperating; 
    private Collider2D [] childColliders;
    //public Animator Animator;
    private EnemyModel enemyModel;
    public bool EnemyIsStill {get; set;} = true;

        public float MaxHealth => throw new NotImplementedException();

        public bool IsDead => throw new NotImplementedException();

        //public AnimationStateManager AnimationStateManager;

        private int pauseAtWayPointTime;
    public bool IsWithinStrikingDistance; 
    public void ChasePlayer(Transform target, float deltaTime)
    {
        if (target == null) return;
        // movement logic: steer toward target, apply velocity, etc.
    }

    public void SetAnimation(string name)
    {
        // Replace with your actual animation system
        //_animator?.SetTrigger(triggerName);
    }

    void Awake()
    {
        // Instantiate instances of the scriptableObjects to assure they are unique, and stored per enemy, not using the same SO for all enemys
        EnemyStatsBaseInstance = Instantiate(EnemyStats);
        EnemyIdleBaseInstance = Instantiate(EnemyIdleBase);
        EnemyChaseBaseInstance = Instantiate(EnemyChaseBase);
        EnemyAttackBaseInstance = Instantiate(EnemyAttackBase);

        AttackComponent = GetComponent<EnemyAttack>();

        // Initialize StateMachine
        root = new EnemyRoot(this);
        var builder = new StateMachineBuilder(root);
        machine = builder.Build();

        //EnemyStatsBaseInstance.Initialze(gameObject, this);
        //EnemyIdleBaseInstance.Initialze(gameObject, this);
        //EnemyChaseBaseInstance.Initialize(gameObject, this);
        //EnemyAttackBaseInstance.Initialze(gameObject, this);

        // set enemy stats
        
        //EnemyCollisionManager.Instance.RegisterEnemy(this);
        _shouldBeSeperating = false;

        childColliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D childCollider in childColliders)
        {
            if (childCollider != _ceilingCheck || _groundCheck) // avoid ignoring self
            {
                Physics2D.IgnoreCollision(_ceilingCheck, childCollider);
                Physics2D.IgnoreCollision(_groundCheck, childCollider);
            }
        }

        // get enemy Animator through the AnimationStateManager on every EnemyModel
        //if (AnimationStateManager = GetComponentInChildren<AnimationStateManager>())
        //{
            //Debug.Log("Model Animator set up");

        // } else Debug.LogError("No AnimationManager found on EnemyModel");

        
    }
    protected virtual void Start()
    {
        // Get the player transform from the GameManager's player prefab reference
        //PlayerTransform = GameManager.Instance.PlayerPrefab.transform;
        IsCoollingDown = false; // starts enemy ready to attack
        attackTimeCounter = 0; // set to zero to allow attacks right at scene load

        RB = GetComponent<Rigidbody2D>();
        previousPosition = transform.position;
        
        if (waypoints.Count() != 0)
        {
            targetPosition = waypoints[currentWaypointIndex].position;
            targetPosition.y = transform.position.y;
            isMoving = true;
            startTime = Time.time;
            currentWaypointIndex = 0;
            journeyLength = Vector2.Distance(RB.position, targetPosition);

        }else if (waypoints.Count() == 0) Debug.LogError("No Way Points Assigned");
            
        transform.position = startingPoint.position;
        knockBack = GetComponent<KnockBack>();
        IsFacingRight = true;

        // set enemy stats
        SetEnemyStats();
    }
    protected virtual void Update()
    {
        float x = 0f;
        if (RB != null)
        {
            if (RB.linearVelocityX < 0) x -= 1f;
            if (RB.linearVelocityX > 0) x += 1f;
            x = Mathf.Clamp(x, -1f, 1f);
            _moveInput.x = x;

        }

        //Debug.Log("Enemy State: " + lastPath);
        machine.Tick(Time.deltaTime); // pass delta to StateMachine Update
        //var path = StatePath(machine.Root.Leaf());
        //if (path != lastPath)
        //{
            //UpdateText(path);
           // lastPath = path;
        //}

        currentState = machine.Root.Leaf();

        IsBeingKnockedBack = knockBack.IsBeingKnockedBack;
        //JumpChecks();

        //if (GameManager.Instance.PlayerIsInScene)
        //{
            //playerTransform = GameManager2.Instance.PlayerStateDriver.transform;
            //Debug.Log("GM Player possistion" + GameManager2.Instance.PlayerStateDriver.transform.position);

        //}

        Timer();
        
    }
    protected virtual void FixedUpdate()
    {
        
            
        var v = RB.linearVelocity; // Read velocity directly from the RigidBody,  Velocity (prior to Unity 6). Holds X and Y.
        v.x = Velocity.x; // set var with what the state wants.
        v.y = Velocity.y; // set var with what the state wants.

        RB.linearVelocity = v; // Override rb velocity with what the State wants with the var.
        Velocity = RB.linearVelocity; // sync rigid body with ctx
        lastVelocity = RB.linearVelocity;
        //Jump();
        CollisionChecks();
        machine.TickFixedUpdate(Time.deltaTime); // pass delta to StateMachine FixedUpdate for Physics
        CheckForLeftOrRightFacing(Velocity);

        rbsStationary = RB.linearVelocity.sqrMagnitude <stationaryThreshold * stationaryThreshold;

        if (_hitWallLeft || _hitWallRight)
        {
            _onWall = true;

        } else _onWall = false;
        
    }
    private void SetEnemyStats () {
        damageAmount = EnemyStats.damageAmount;
        Maxhealth = EnemyStats.maxHealth;
        CurrentHealth = EnemyStats.maxHealth;
        
        
    }
    public void ShouldBeSeperating (bool shouldBeSeperating) {
        _shouldBeSeperating = shouldBeSeperating;
    }
    public Vector2 GetLastFrameVelocity()
    {
        return lastVelocity;
    }
#region Collectible functions
    // create collectable to drop after death of this enemy
    void DropCollectable()
    {
        GameObject loot = Instantiate(collectablePrefab, transform.position, Quaternion.identity);
        loot.transform.parent = null;
        
        if (transform.parent != null)
        {
            Destroy(transform.parent.gameObject);
        }
    }
#endregion
#region Timers
    
#endregion
#region Collision Checks
    private void CollisionChecks()
    {
        CheckIsGrounded();
        WallCheck();
    }
    private void CheckIsGrounded()
    {
        Vector2 boxCastOrigin = new Vector2(_groundCheck.bounds.center.x, _groundCheck.bounds.min.y);
        Vector2 boxCastSize = new Vector2(_groundCheck.bounds.size.x, EnemyStats.GroundDetectionRayLenth);

        _groundHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.down, EnemyStats.GroundDetectionRayLenth, EnemyStats.GroundLayer);

        if (_groundHit.collider != null)
        {
            Debug.Log("Should be grounded");
            _isGrounded = true;
            VerticalVelocity = 0f;
        }
        else { _isGrounded = false; }

        #region Debug Visulization
        //draw out the ray  cast vissualy
        if (EnemyStats.DebugShowIsGroundedBox)
        {
            Color rayColor;
            if (_isGrounded)
            {
                rayColor = Color.green;
            }
            else { rayColor = Color.red; }

            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y), Vector2.down * EnemyStats.GroundDetectionRayLenth, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y), Vector2.down * EnemyStats.GroundDetectionRayLenth, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x - (boxCastSize.x / 2), boxCastOrigin.y - EnemyStats.GroundDetectionRayLenth), Vector2.right * boxCastSize.x, rayColor);
        }

        #endregion
    }
    private void WallCheck()
    {
        float offsetDistance = _knockBackCheck.bounds.size.x * 0.50f;
        //Vector2 boxCastOrigin = new Vector2(_bodyColl.bounds.center.x, _bodyColl.bounds.center.y);
        Vector2 boxCastOriginLeft = new Vector2(_knockBackCheck.bounds.center.x - offsetDistance, _knockBackCheck.bounds.center.y);
        Vector2 boxCastOriginRight = new Vector2(_knockBackCheck.bounds.center.x + offsetDistance, _knockBackCheck.bounds.center.y);

        Vector2 boxCastSize = new Vector2(_knockBackCheck.bounds.size.x, EnemyStats.WallDetectionRayLenth);

        // detect left wall
        _wallHitLeft = Physics2D.BoxCast(boxCastOriginLeft, boxCastSize, 0f, Vector2.left, EnemyStats.WallDetectionRayLenth, EnemyStats.WallLayer);
        // detect right wall
        _wallHitRitght = Physics2D.BoxCast(boxCastOriginRight, boxCastSize, 0f, Vector2.right, EnemyStats.WallDetectionRayLenth, EnemyStats.WallLayer);

        
        if (_wallHitLeft.collider != null)
        {
            Debug.Log("Enemy hit wall on left");

            if (!stopOnwallCallLeft)
            {
                _hitWall = true;
                _hitWallLeft = true;
                stopOnwallCallLeft = true;
            }
        } 
        else 
        {
            //_hitWall = false;
            _hitWallLeft = false;
            stopOnwallCallLeft = false;
        }

        if (_wallHitRitght.collider != null)
        {
            Debug.Log("Enemy hit wall on right");
            
            if (!stopOnwallCallRight)
            {
                //_hitWall = true;
                _hitWallRight = true;
                stopOnwallCallRight = true;
            }
        } 
        else 
        {
            //_hitWall = false;
            _hitWallRight = false;
            stopOnwallCallRight = false;
            
        }            
    }
    private void SetWallHit()
    {
        
    }
    // Example usage method
    public void ToggleHitWall()
    {
        //HitWall = !HitWall; // This automatically triggers OnBoolChanged
    }
    public void CheckForLeftOrRightFacing(Vector2 velocity)
    {

        Vector3 currentDirection = (transform.position - previousPosition).normalized;
        previousPosition = transform.position;

        // Use currentDirection to determine movement direction
        // For 2D: check currentDirection.x or currentDirection.y
        if (currentDirection.x > 0) {
            // Moving right 
            Vector3 rotator = new Vector3(transform.rotation.x, 0f, transform.rotation.z);
            transform.rotation = Quaternion.Euler(rotator);
            IsFacingRight = true;

        } else if (currentDirection.x < 0) {
            // Moving left
            Vector3 rotator = new Vector3(transform.rotation.x, 180f, transform.rotation.z);
            transform.rotation = Quaternion.Euler(rotator);
            IsFacingRight = false;
        }
        if (currentDirection.y > 0) {
            // Moving up
        } else if (currentDirection.y < 0) {
            // Moving down
        }

        /*
        if (IsFacingRight && velocity.x < 0f)
        {
            Vector3 rotator = new Vector3(transform.rotation.x, 180f, transform.rotation.z);
            transform.rotation = Quaternion.Euler(rotator);
            IsFacingRight = !IsFacingRight;
        }

        else if (!IsFacingRight && velocity.x > 0f)
        {
            Vector3 rotator = new Vector3(transform.rotation.x, 0f, transform.rotation.z);
            transform.rotation = Quaternion.Euler(rotator);
            IsFacingRight = !IsFacingRight;
        }
        */

    }
#endregion 
#region Heath Die Functions

    public void GiveDamage (int damage) {
        
        iDamageable = player.GetComponentInChildren<IDamagable>();
            
        if (iDamageable != null)
        {
            Debug.Log("push player");
            iDamageable.Damage(damage, transform.right);
            // increase attackCount by 1 each time damage given
            
        }
    }
    // Take Damage
    public void Damage(float damageAmount, Vector2 hitDirection)
    {
        CurrentHealth -= damageAmount;
        Debug.Log("Taking Damage");

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }
    public void Die()
    {
        
        //play death animation
        //disable enemy
        // reset ememy stats
        // move to new spwan point

        // destroy enemy object, or .... 
        //Destroy(gameObject);

        if (collectablePrefab != null)
        {
            DropCollectable();
        }
        
        // Destroy the parent object when this child is destroyed
        if (transform.parent != null)
        {
            Destroy(transform.parent.gameObject);
            Debug.Log("Should be Destroyed: "); 
        }
    }
    #endregion
#region CHASE PLAYER
    // New Chase, 5/25/2026
    public void ChasePlayer(Vector2 targetPosition)
    {
        if (PlayerTransform == null) return;

        // Get player's collider for precise offset
        Collider2D playerCollider = PlayerTransform.GetComponent<Collider2D>();
        if (playerCollider == null) return;

        Vector3 playerExtents = playerCollider.bounds.extents;
        
        // Calculate offset to prevent overlap
        // Adjust multiplier as needed for your game's scale
        float offsetMultiplier = 0.8f;
        Vector2 offset = (Vector2)(PlayerTransform.right) * (playerExtents.x * offsetMultiplier);

        // Apply offset so enemy stops before touching player
        Vector2 finalTarget = targetPosition - offset;
        finalTarget.y = transform.position.y;

        Vector2 currentPosition = RB.position;
        Vector2 newPosition = Vector2.Lerp(currentPosition, finalTarget, 
            EnemyStats.MaxRunSpeed * Time.fixedDeltaTime);
        
        RB.MovePosition(newPosition);
    }   
    /*
    // Orignial Chase method
    public void ChasePlayer()
    {
        if (!stop) // call only once per enemy
        {
            // find player
            player = GameObject.FindWithTag("Player");

            if (player != null)
            {
                // get player collider to mesure
                playerFootCollider = player.GetComponentInChildren<Collider2D>();
            }

            stop = true;
        }

        // get player collider bounds
        Vector3 extents = playerFootCollider.bounds.extents;

        // calculate player collider bounds to prevent colliders collision and unwanted behavior
        Vector3 playerOffset = extents * transform.localScale.x; // 
        Vector2 targetPosition = player.transform.position - player.transform.right * playerOffset.x;
        
        // keep enemy on ground
        targetPosition.y = transform.position.y; 
        
        // Interpolate position
        currentPosition = RB.position;
        targetPosition.y = transform.position.y;
        newPosition = Vector2.Lerp(currentPosition, targetPosition, EnemyStats.MaxRunSpeed * Time.deltaTime);
        newPosition.y = transform.position.y;

        
        // Move rigidbody (physics-aware)
        RB.MovePosition(newPosition);

        
    }
    */

    #endregion
#region Idle Functions
    // not curently in use
    private IEnumerator WaitToChangeTarget (Vector3 targetPos, float speed)
    {

        var startPos = transform.position;

        // keep RB on ground
        startPos.y = transform.position.y; 

        var distance = Vector3.Distance(startPos, targetPos);
        targetPos.y = transform.position.y; // keep on ground
        var duration = distance / speed;
        var timePassed = 0f; 

        while (timePassed < duration)
        {
            float factor = timePassed / duration;
            float easedFactor = easeCurve.Evaluate(factor); // Replaces Mathf.SmoothStep
            
            //transform.position = (Vector3.Lerp(startPos, targetPos, easedFactor));
            RB.MovePosition(Vector3.Lerp(startPos, targetPos, easedFactor));

            if (_hitWallLeft || _hitWallRight)
            {
                yield break;
            }

            yield return null;

            timePassed += Time.deltaTime;

        }

        //RB.position = targetPos; // assure at position
    }
    // using as base for idle movement
    public void  MoveEnemy () {

        if (RB == null || waypoints.Length == 0) return;

        if (!ShouldMove) return;

        if (OnWall)
        {
            if (_hitWallLeft)
            {
                currentWaypointIndex = 1;
                targetPosition = waypoints[currentWaypointIndex].position;
                

                //StartCoroutine(MoveToTarget(targetPosition, EnemyStats.MaxWalkSpeed));
            }

            if (_wallHitRitght)
            {
                currentWaypointIndex = 0;
                targetPosition = waypoints[currentWaypointIndex].position;
                

                //StartCoroutine(MoveToTarget(targetPosition, EnemyStats.MaxWalkSpeed));
            }
            
            //return;

        } 
        
        StartCoroutine(MoveToTarget(targetPosition, EnemyStats.MaxWalkSpeed));
        
        
        // Check if reached waypoint
        if (Vector2.Distance(RB.position, targetPosition) < 0.05f)
        {
            //Debug.Log("Set Waypoint");
            StartCoroutine(PauseAtWayPoint(2));
            SetNextWaypoint();
        }


    }
    private IEnumerator MoveToTarget(Vector3 targetPos, float speed)
    {

        var startPos = transform.position;

        // keep RB on ground
        startPos.y = transform.position.y; 

        var distance = Vector3.Distance(startPos, targetPos);
        targetPos.y = transform.position.y; // keep on ground
        var duration = distance / speed;
        var timePassed = 0f; 

        while (this.ShouldMove && timePassed < duration)
        {
            float factor = timePassed / duration;
            float easedFactor = easeCurve.Evaluate(factor); // Replaces Mathf.SmoothStep
            
            //transform.position = (Vector3.Lerp(startPos, targetPos, easedFactor));
            RB.MovePosition(Vector3.Lerp(startPos, targetPos, easedFactor));

            if (_hitWall)
            {
                StartCoroutine(PauseAtWayPoint(3));
                yield break;
            }

            yield return null;

            timePassed += Time.deltaTime;

        }

        //RB.position = targetPos; // assure at position
    }
    private void SetNextWaypoint()
    {
        if (currentWaypointIndex >= waypoints.Length)
        {
            // Optional: loop or stop
            //return;
        }

        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        targetPosition = waypoints[currentWaypointIndex].position;

        // keep enemy on ground
        //targetPosition.y = transform.position.y;
    }

    private void MoveBetweenObsticals () {
            if (_hitWallLeft)
            {
                RB.AddForce(Vector2.right * EnemyStats.MaxWalkSpeed * Time.deltaTime);
        
            }

            if (_hitWallRight)
            {
                RB.AddForce(-Vector2.right * EnemyStats.MaxWalkSpeed  * Time.deltaTime);
            }
            

    }
    IEnumerator PauseAtWayPoint(int seconds)
    {
        pauseAtWayPointTime = seconds;
        this.ShouldMove = false;
        int counter = seconds;
        //AnimationStateManager.SetAnimationState(AnimationStates.Still);
        while (counter > 0)
        {
            yield return new WaitForSeconds(1);
            counter--;
            _hitWall = false;
        }
        //AnimationStateManager.SetAnimationState(AnimationStates.Idle);
        // reset startTime
        //startTime = Time.time;
        //journeyLength = Vector2.Distance(RB.position, targetPosition);
        this.ShouldMove = true;
    }

    // not for Idle/wander
    private IEnumerator MoveEnemySmoothly(Vector3 targetPosition, float speed)
    {
        if (speed <= 0) yield break;

        Vector3 startPosition = transform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);
        float duration = distance / speed; // Time based on speed and distance

        float timePassed = 0f;
        while (timePassed < duration)
        {
            float factor = timePassed / duration;
            float easedFactor = easeCurve.Evaluate(factor);
            //factor = Mathf.SmoothStep(0f, 1f, factor); // Apply ease-in and ease-out
            
            RB.MovePosition(Vector3.Lerp(startPosition, targetPosition, easedFactor));

            //transform.position = Vector3.Lerp(startPosition, targetPosition, factor);
            yield return null;

            timePassed += Time.deltaTime;
        }

        transform.position = targetPosition; // Ensure exact arrival
    }   
    
    // not using as of 3.28.2016 
    public void WayPointWander () {
        if (RB == null || waypoints.Length == 0) return;

        if (!ShouldMove) return;
        
        // Move toward target using ease curve
        float distanceCovered = (Time.time - startTime) * EnemyStats.MaxWalkSpeed;
        float journeyProgress = distanceCovered / journeyLength;
        Mathf.Clamp01(journeyProgress);

        // Apply ease-in-out curve
        easedProgress = easeCurve.Evaluate(journeyProgress);
        Mathf.Clamp01(easedProgress);

        if (float.IsNaN(easedProgress))
        {
            Debug.LogError("easedProgress is NaN!");
            return;
        }

        // Interpolate position
        currentPosition = RB.position;
        targetPosition.y = transform.position.y;
        newPosition = Vector2.Lerp(currentPosition, targetPosition, easedProgress);
        newPosition.y = transform.position.y;

        // capture the current RB X velocity for Airborne State to know 
        currentVelocity.x = newPosition.x; 
        
        // Move rigidbody (physics-aware)
        RB.MovePosition(newPosition);

        // Check if reached waypoint
        if (Vector2.Distance(RB.position, targetPosition) < 0.01f)
        {
            
            StartCoroutine(PauseAtWayPoint(5));
            SetNextWaypoint();
        }
    }
    
    // Not currently used
    public void WanderToWayPointByElapsedTime () {
        Debug.Log("Should be wandering");
        if (!ShouldMove) return;
        

        float elapsedTime = Time.time - startTime;
        float t = Mathf.Clamp01(elapsedTime / easeInEaseOutDuration);

        // Ease-in-out interpolation (smoothstep)
        float easedT = Mathf.SmoothStep(0f, 1f, t);

        // Calculate target velocity using direction and eased progress
        Vector2 direction = (targetPosition - RB.position).normalized;
        targetPosition.y = transform.position.y;
        Vector2 targetVelocity = direction * EnemyStats.MaxWalkSpeed;

        // Apply interpolated velocity
        Velocity = Vector2.Lerp(Vector2.zero, targetVelocity, easedT);

        // Check if reached waypoint
        if (Vector2.Distance(RB.position, targetPosition) < 0.1f)
        {
            Velocity = Vector2.zero;
            StartCoroutine(PauseAtWayPoint(1));

            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            targetPosition = waypoints[currentWaypointIndex].position;
            startTime = Time.time;
        }
    }
    public void PatrollPauseAtWayPoint (float timeToTarget, int pauseTime) {
        if (!this.isMovingToWayPoint)
        {
            if (waypoints.Count() == 0 || currentWaypointIndex >= waypoints.Count()) return;
            
            Vector2 targetPosition = waypoints[currentWaypointIndex].position;
        
            StartCoroutine(MoveEnemyToTarget(targetPosition, timeToTarget, pauseTime));
        }
        
    }
    
    #endregion
#region MOVE Functions
    private IEnumerator MoveEnemyToTarget(Vector2 targetPosition, float duration, int pauseAtWaypoint)
    {
        this.isMovingToWayPoint = true;
        float elapsedTime = Time.time - startTime;
        float t = Mathf.Clamp01(elapsedTime / easeInEaseOutDuration);

        // Ease-in-out interpolation (smoothstep)
        float easedT = Mathf.SmoothStep(0f, 1f, t);

        // Calculate target velocity using direction and eased progress
        Vector2 direction = (targetPosition - RB.position).normalized;
        targetPosition.y = transform.position.y;
        Vector2 targetVelocity = direction * EnemyStats.MaxWalkSpeed;

        while (elapsedTime < duration)
        {
            // Apply interpolated velocity
            Velocity = Vector2.Lerp(Vector2.zero, targetVelocity, easedT);
        
            elapsedTime += Time.time - startTime;
            yield return null;
        }

        RB.position = targetPosition;
        //transform.position = targetPosition; // Ensure exact arrival

        if (Vector2.Distance(transform.position, targetPosition) < 0.1f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }

        this.pausedAtWayPoint = true;

        yield return new WaitForSeconds(pauseAtWaypoint);
        this.pausedAtWayPoint = false;
        this.isMovingToWayPoint = false;

        Debug.Log("Wait time is over");

        
    }
    private IEnumerator MoveEnemyToTarget2(Vector2 targetPosition, float duration, int pauseAtWaypoint)
    {
        this.isMovingToWayPoint = true;
        Vector2 startPosition = transform.position;
        //Vector2 direction = (targetPosition - RB.position).normalized; // use with RigidBody
        //Vector2 newVelocity = direction * enemyStats.MaxWalkSpeed; // use with rigidbody
        float elapsedTime = 0;
        //CheckForLeftOrRightFacing(Velocity);
        while (elapsedTime < duration)
        {
            // SmoothStep creates ease-in and ease-out effect
            var factor = Mathf.SmoothStep(0, 1, elapsedTime / duration);
            // keep enemy on ground
            targetPosition.y = transform.position.y; 
            RB.linearVelocityY = 0f;
            //RB.linearVelocity = newVelocity; // move by RigidBody
            RB.MovePosition(Vector2.Lerp(startPosition, targetPosition, factor)); // move by RB
            //transform.position = Vector2.Lerp(startPosition, targetPosition, factor); // move by transform
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        RB.position = targetPosition;
        //transform.position = targetPosition; // Ensure exact arrival

        if (Vector2.Distance(transform.position, targetPosition) < 0.1f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }

        this.pausedAtWayPoint = true;

        yield return new WaitForSeconds(pauseAtWaypoint);
        this.pausedAtWayPoint = false;
        this.isMovingToWayPoint = false;

        Debug.Log("Wait time is over");
    }
    public void Move(Vector2 direction, bool walk, float acceleration, float deceleration)
    {
        //set the x movement 
        Debug.Log("Should be moving, direction/Velocity: " + direction +"/" + Velocity);
        // if (move)
        if (direction != Vector2.zero)
        {
            direction.y = transform.position.y;

            Vector2 targetVelocity = Vector2.zero;

            if (!walk) // run or chase
            {
                targetVelocity = new Vector2(direction.x, direction.y) * EnemyStats.MaxRunSpeed;
                //OnRunning(true);
            }
            else // Walk
            {
                targetVelocity = new Vector2(direction.x, direction.y) * EnemyStats.MaxWalkSpeed;
                //OnRunning(false);
            }

            _MoveVolocity = Vector2.Lerp(_MoveVolocity, targetVelocity, acceleration * Time.fixedDeltaTime);
            Velocity = new Vector2(_MoveVolocity.x, transform.position.y);
            //Velocity = Vector2.MoveTowards(transform.position, direction, Velocity.x);
            
            
        }

        else if (direction == Vector2.zero) // else if (!move)
        {
            _MoveVolocity = Vector2.Lerp(_MoveVolocity, Vector2.zero, deceleration * Time.fixedDeltaTime);

            Velocity = new Vector2(_MoveVolocity.x, VerticalVelocity);

        }
    }
    
    public void MoveEnemy (Vector2 velolicy) {
        
    }

    #endregion      
#region Animation Triggers
    private void  AnimationTriggerEvent (AnimationTriggerType triggerType) 
    {
        
    }

    public enum AnimationTriggerType
    {
        EnemyDamaged,
        PlayWalkingSound
    }

    #endregion
#region Distance / Agro Checks
    public void SetAggroStatus(bool isAggored)
    {
        IsAggroed = isAggored;
    }

    public void SetStrikingDistanceBool(bool isWithinStrikingDistance)
    {
        IsWithinStrickingDistance = isWithinStrikingDistance;
    }

    public void SetKnockBackCheck () {

        //iDamageable = GameManager2.Instance.PlayerStateDriver.GetComponent<IDamagable>();
            
        if (iDamageable != null)
        {
            Debug.Log("push player");
            iDamageable.Damage(0, transform.right);
            // increase attackCount by 1 each time damage given
            
        }
    }

    #endregion      
#region Attack Functions
    public void ShouldBeDamagingToTrue () 
    {
        ShouldBeDamaging = true;
        
    }

    public void ShouldBeDamagingToFalse () 
    {
        ShouldBeDamaging = false;
    }      
    
    void Timer()
    {
        if (totalTime > 0)
        {
            // cooldown has started

            totalTime -= Time.deltaTime;
            // Calculate minutes and seconds
            int minutes = Mathf.FloorToInt(totalTime / 60);
            int seconds = Mathf.FloorToInt(totalTime % 60);
        }
        else
        {
            totalTime = 0;
            timesUp = true;
        }
    }
    
    private IEnumerator CoolDown()
    {
        yield return new WaitForSeconds(EnemyStats.coolDownTime);
        hitsUsed = 0; // reset times enemy can hit
        Debug.Log("Attack ended");
        this.IsCoollingDown = false; // reset cool down to false
    }
    private void AttackTimeTimer () {

        attackTimeCounter -= Time.deltaTime;

        if (attackTimeCounter == 0)
        {
            attackTimeCounter = EnemyStats.timeBetweenHits;
        }
    }

    private IEnumerator AttackTime()
    {

        this.IsAttacking = true;
        
        while (attackTimeCounter >0)
        {
            
            yield return new WaitForSeconds(coolDownTime);

            IsAttacking = false;
        }


        this.pausedAtWayPoint = true;

        yield return new WaitForSeconds(coolDownTime);
        this.IsAttacking = false;

        Debug.Log("Wait time is over");
    }  

    public void Attack()
    {
        
        if (!IsCoollingDown && attackTimeCounter > EnemyStats.timeBetweenHits)
        {
            ShouldBeDamagingToTrue();


            iDamageable = player.GetComponentInChildren<IDamagable>();
            
            if (iDamageable != null)
            {
                iDamageable.Damage(EnemyStats.damageAmount, transform.right);
                // increase attackCount by 1 each time damage given
                hitsUsed ++;
            }

            attackTimeCounter = 0; // reset attacktime count down

        }

        if (hitsUsed == EnemyStats.HitsAllowed)
        {
            StartCoroutine(CoolDown());
            IsCoollingDown = true;
            ShouldBeDamagingToFalse();
        }

        attackTimeCounter += Time.deltaTime;
    }

    #endregion  
#region Jump Checks
    public void JumpChecks()
    {
        //WHEN WE PRESS THE JUMP BUTTON
        if (JumpWasPressed) // need to update to call jump from states
        {
            JumpBufferTimer = EnemyStats.JumpBufferTime;
            JumpReleaseDuringBuffer = false;
        }

        //WHEN WE RELEASE THE JUMP BUTTON
        if (JumpWasReleased) // need to update to release jump from state
        {
            if (JumpBufferTimer > 0f)
            {
                JumpReleaseDuringBuffer = true;
            }

            if (IsJumping && VerticalVelocity > 0f)
            {
                if (IsPastApexThreshold)
                {
                    IsPastApexThreshold = false;
                    IsFastFalling = true;
                    FastFallTime = EnemyStats.TimeForUpwardsCancel;
                    VerticalVelocity = 0f;

                }
                else
                {
                    IsFastFalling = true;
                    FastFallReleaseSpeed = VerticalVelocity;
                }
            }
        }

        //INITIATE JUMP WITH JUMP BUFFERING AND COYOTE TIME
        if (JumpBufferTimer > 0f && !IsJumping && (_isGrounded || CoyoteTimer > 0f))
        {
            InitiateJump(1);

            if (JumpReleaseDuringBuffer)
            {
                IsFastFalling = true;
                FastFallReleaseSpeed = VerticalVelocity;
            }

        }

        //DOUBLE JUMP
        else if (JumpBufferTimer > 0f && IsJumping && NumberOfJumpsUsed < EnemyStats.NumberOfJumpsAllowed)
        {
            IsFastFalling = false;
            InitiateJump(1);
        }

        //handle air jump AFTER the coyote time has lapsed (take off an extra jump so we don't get a bounus jump)
        else if (JumpBufferTimer > 0f && IsFalling && NumberOfJumpsUsed < EnemyStats.NumberOfJumpsAllowed - 1)
        {
            InitiateJump(2);
            IsFastFalling = false;
        }

        //LANDED
        if ((IsJumping || IsFalling) && _isGrounded && VerticalVelocity <= 0f)
        {
            IsJumping = false;
            IsFalling = false;
            IsFastFalling = false;
            FastFallTime = 0f;
            IsPastApexThreshold = false;
            NumberOfJumpsUsed = 0;

            VerticalVelocity = Physics2D.gravity.y;
        }
    }
    public void Jump()
    {
        if (!IsBeingKnockedBack)
        {
            //APPLY GRAVITY WHILE JUMPING
            if (IsJumping)
            {
                //CHECK FOR HEAD BUMP
                if (BumpedHead)
                {
                    IsFastFalling = true;

                }

                //GRAVITY ON ASCENDING
                if (VerticalVelocity >= 0f)
                {
                    //APEX CONTROLS
                    ApexPoint = Mathf.InverseLerp(EnemyStats.InitialJumpVelocity, 0f, VerticalVelocity);

                    if (ApexPoint > EnemyStats.ApexThreshold)
                    {
                        if (!IsPastApexThreshold)
                        {
                            IsPastApexThreshold = true;
                            TimePastApexThreshold = 0f;
                        }

                        if (IsPastApexThreshold)
                        {
                            TimePastApexThreshold += Time.fixedDeltaTime;
                            if (TimePastApexThreshold < EnemyStats.ApexHangTime)
                            {
                                VerticalVelocity = 0f;
                            }
                            else
                            {
                                VerticalVelocity = -0.01f;
                            }
                        }

                    }

                    //GRAVIty ON ASCENDING BUT NOT APEX THRESHOLD
                    else
                    {
                        VerticalVelocity += EnemyStats.Gravity * Time.fixedDeltaTime;
                        if (IsPastApexThreshold)
                        {
                            IsPastApexThreshold = false;
                        }
                    }
                }

                //GRAVITY ON DESCENDING
                else if (!IsFastFalling)
                {
                    VerticalVelocity += EnemyStats.Gravity * EnemyStats.GravityOnReleaseMultiplier * Time.fixedDeltaTime;
                }

                else if (VerticalVelocity < 0f)
                {
                    if (!IsFalling)
                    {
                        IsFalling = true;
                    }
                }
            }

            //JUMP CUT
            if (IsFastFalling)
            {
                if (FastFallTime >= EnemyStats.TimeForUpwardsCancel)
                {
                    VerticalVelocity += EnemyStats.Gravity * EnemyStats.GravityOnReleaseMultiplier * Time.fixedDeltaTime;
                }
                else if (FastFallTime < EnemyStats.TimeForUpwardsCancel)
                {
                    VerticalVelocity = Mathf.Lerp(FastFallReleaseSpeed, 0f, (FastFallTime / EnemyStats.TimeForUpwardsCancel));
                }

                FastFallTime += Time.fixedDeltaTime;
            }

            //NORMAL GRAVITY WHILE FALLING
            if (!_isGrounded && !IsJumping)
            {
                if (!IsFalling)
                {
                    IsFalling = true;
                }

                VerticalVelocity += EnemyStats.Gravity * Time.fixedDeltaTime;
            }

            //CLAMP FALL SPEED
            VerticalVelocity = Mathf.Clamp(VerticalVelocity, -EnemyStats.MaxFallSpeed, 50f);

            Velocity = new Vector2(RB.linearVelocity.x, VerticalVelocity);

        }
        


    }
    public void GravityOnDecending()
    {
        // calculate gravity 
        VerticalVelocity += EnemyStats.Gravity * EnemyStats.GravityOnReleaseMultiplier * Time.fixedDeltaTime;
        //CLAMP FALL SPEED
        VerticalVelocity = Mathf.Clamp(VerticalVelocity, -EnemyStats.MaxFallSpeed, 50f);

        // apply gravity with current RB X movment as last known RB X movment
        Velocity = new Vector2(GetLastFrameVelocity().x, VerticalVelocity);
        //RB.MovePosition(new Vector2(_MoveVolocity.x,VerticalVelocity));

    }
    public void GravityOnDecending2 () {

        float deltaX = Mathf.Abs(transform.position.x - lastPosition.x);
        bool isMovingOnX = deltaX > movementThreshold;
        
        Vector3 velocity = transform.position - lastPosition;
        lastPosition = transform.position;

        if (isMovingOnX)
        {
            Debug.Log("Object is moving on X-axis");
            // Object is moving on X-axis
            if (Mathf.Abs(velocity.x) < movementThreshold)
            {
                // Apply damping to slow down
                transform.position = new Vector3(
                    transform.position.x,
                    VerticalVelocity,
                    transform.position.z
                );
            }
            else
            {
                // Continue moving normally

            }
        }
        else
        {
            // Object is not moving on X-axis
            VerticalVelocity += EnemyStats.Gravity * EnemyStats.GravityOnReleaseMultiplier * Time.fixedDeltaTime;
            //CLAMP FALL SPEED
            VerticalVelocity = Mathf.Clamp(VerticalVelocity, -EnemyStats.MaxFallSpeed, 50f);

            Velocity = new Vector2(Velocity.x, VerticalVelocity);
        }

        

        
    }
    private void InitiateJump(int numberOfJumpsUsed)
    {
        if (!IsJumping)
        {
            IsJumping = true;
        }

        JumpBufferTimer = 0f;
        NumberOfJumpsUsed += numberOfJumpsUsed;

        var rb = RB;

        if (rb != null)
        {
            var v = rb.linearVelocity;
            v.y = EnemyStats.InitialJumpVelocity;

            VerticalVelocity = v.y;

        }else Debug.Log("rigidBody is null");
    }

#endregion           
#region Helpers and Gizmos

    private void DebugPrint (string title, float variable) {
        
        Debug.Log(title + ": " + variable);
    }
    IEnumerator PauseFunction(int time) {
        yield return new WaitForSeconds(time);
        // add code to run after pause base on time 
    }

    void OnDrawGizmosSelected() 
    {
        //if (!drawGizmos || groundCheck == null) return;
        Gizmos.color = Color.white;
        Gizmos.color = Color.white;
        //Gizmos.DrawWireSphere(groundCheck.position, groundRadius);

    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(startingPoint.transform.position, 0.5f);
        Gizmos.DrawWireSphere(pointA.transform.position, 0.5f);
        Gizmos.DrawWireSphere(pointB.transform.position, 0.5f);
        Gizmos.DrawLine(pointA.transform.position, pointB.transform.position);
    }
    void OnGUI() // print var states to game scene
    {
        if (_gizmosLable)
        {
            GUILayout.Label("Onwall " + OnWall);
            GUILayout.Label("hitwall "+ _hitWall);
            GUILayout.Label("hitwall left/right  " + _hitWallLeft + " " + _hitWallRight);
            GUILayout.Label("stopOnwallCallLeft " + stopOnwallCallLeft);
            //GUILayout.Label("Enemy Current health " + CurrentHealth);
            GUILayout.Label("Enemy STATE: "  + lastPath);
            //GUILayout.Label("Enemy Velocity: "  + Velocity);
            //GUILayout.Label("RB Posisiton: "  + RB.position);
            //GUILayout.Label("Transform position: "  + transform.position);
            //GUILayout.Label("Target Position " + targetPosition);
            GUILayout.Label("Current Waypoint Index: "  + currentWaypointIndex);
        
            //GUILayout.Label("Gravity: "  + enemyStats.Gravity);
            //GUILayout.Label("Grounded: "  + _isGrounded);
            //if (waypoints.Count() != 0)
            //{
            GUILayout.Label("Waypoint Possition: "  + waypoints[currentWaypointIndex].position); 
            //}
            
            GUILayout.Label("Ispaused at Waypoint: "  + this.pausedAtWayPoint);
            GUILayout.Label("IsAggroed "  + this.IsAggroed);
            GUILayout.Label("Striking Distance "  + this.IsWithinStrickingDistance);
            //GUILayout.Label("PLAYER TRANSFORM "  + this.player.transform);
            //GUILayout.Label("PLAYER Possition "  + this.playerTransform.position);
            //GUILayout.Label("Target Possistion "  + this.targetPosition);
            //GUILayout.Label("Hits used "  + this.hitsUsed);
            //GUILayout.Label("Attacktimer "  + this.attackTimeCounter);
            GUILayout.Label("IsCollinDown"  + this.IsCoollingDown);
            //GUILayout.Label("CoolDown Time " + enemyStats.coolDownTime);
            //GUILayout.Label("IsMoving " + this.isMoving);
            //GUILayout.Label("RB POSITION " + this.RB.position);
            //GUILayout.Label("Current POSITION " + currentPosition);
            //GUILayout.Label("Target POSITION " + targetPosition);
            //GUILayout.Label("Eased Progress " + easedProgress);
            GUILayout.Label("SholdMove" + ShouldMove);
            
        }
        
    

    }  

    //static string StatePath(State s)
    //{
       // return string.Join(" > ", s.PathToRoot().Reverse().Select(n => n.GetType().Name));
   //}
    #endregion
    
    void OnDestroy()
    {
       // EnemyCollisionManager.Instance.UnregisterEnemy(this);
    }

    public void Damage(float damageAmount, int sourseId, Vector2 hitDirection)
    {
        
    }

        public void Damage(float amount, Vector3 knockbackDirection)
        {
            throw new NotImplementedException();
        }
    }
}