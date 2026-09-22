using UnityEngine;

namespace Game.Gameplay.Enemies
{
    public class EnemeyPatrolControl : MonoBehaviour {

    [Header("Ememy Dirction")]
    [SerializeField] private bool IsFacingLeft;

    public GameObject pointA;
    public GameObject pointB;
    private Rigidbody2D rb;
    //private Animator anim;
    private Transform currentPoint;
    public float speed;


    private bool isFacingLeft;

        void Awake()
        {
            isFacingLeft = IsFacingLeft;
            //Debug.Log("Awake" + isFacingLeft);
        }

        // Start is called before the first frame update
        void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        //anim = GetComponent<Animator>();
        currentPoint = pointB.transform;
        //anim.SetBool("isRuning", true);
        //IsFacingRight = false;

    }

    // Update is called once per frame
    void Update()
    {
        //Vector2 point = currentPoint.position - transform.position;
        
        if (currentPoint == pointB.transform)
        {
            rb.linearVelocity = new Vector2(speed, 0);
        }
        else
        {
            rb.linearVelocity = new Vector2(-speed, 0);
        }

        if (Vector2.Distance(transform.position, currentPoint.position) < 0.5f && currentPoint == pointB.transform)
        {
            flip();
            currentPoint = pointA.transform;
        }

        if (Vector2.Distance(transform.position, currentPoint.position) < 0.5f && currentPoint == pointA.transform)
        {
            flip();
            currentPoint = pointB.transform;
        }

    }

    //flip the sprite on x, facing left to right...
    private void flip()
    {
        //Debug.Log("FLIP" + isFacingLeft);
        if (isFacingLeft)
        {
            transform.eulerAngles = new Vector3(0, 0, 0); //facing right
            isFacingLeft = false;
        }
        else
        {
            transform.eulerAngles = new Vector3(0, 180, 0); //facing left
            isFacingLeft = true;
        }

        /*
        //Debug.Log("flip");
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
        */
    }


    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(pointA.transform.position, 0.5f);
        Gizmos.DrawWireSphere(pointB.transform.position, 0.5f);
        Gizmos.DrawLine(pointA.transform.position, pointB.transform.position);
    }

        public void Damage(float damageAmount, Vector2 hitDirection)
        {
            throw new System.NotImplementedException();
        }
    }


}

