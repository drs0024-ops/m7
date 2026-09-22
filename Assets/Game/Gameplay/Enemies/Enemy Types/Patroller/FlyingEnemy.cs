
using UnityEngine;

namespace Game.Gameplay.Enemies
{
    public class FlyingEnemy : MonoBehaviour
    {
        public float speed;
        public bool chase = false;
        public Transform startingPoint;
        private GameObject player;
        //private PlayerInvisibility playerInvisibilty;
        private Rigidbody2D rb;
        

        // Start is called before the first frame update
        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            player = GameObject.FindGameObjectWithTag("Player");
            //playerInvisibilty = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInvisibility>();
            if (player == null) Debug.Log("Player is not found by enemy");
            rb.position = startingPoint.position;

        }

        private void FixedUpdate () {
            //make sure the player is present then case and flip to face the player at all times
            if (player == null)
            {
                //Debug.Log("Player is null");
                return;
            }
            else if (chase == true)
            {
                Chase();
                Flip();
            }
            else if (chase == false)
            {
                RetrunStartingPoint();

            }
        }

        private void Chase()
        {
            rb.MovePosition(Vector2.MoveTowards(rb.position, player.transform.position, speed * Time.fixedDeltaTime));

            //change speed, shoot, animation and reset variable
            if (Vector2.Distance(transform.position, player.transform.position) <= 0.5f)
            {
                //change speed, shoot, animation
            }
            else
            {
                //reset variable
            }
        }

        private void FlipToStartingPoint()
        {
            //checks for the locaion of the starting point to face when returning to it
            if (transform.position.x == startingPoint.position.x)
            {
                //do nothing
            }
            else if (transform.position.x > startingPoint.transform.position.x)
            {
                //flips to face the starting point left
                transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            else
            {
                //flips to face the starting point right
                transform.rotation = Quaternion.Euler(0, 0, 0);
            }
        }

        private void RetrunStartingPoint()
        {
            //checks to see if it's on the starting point
            if (Vector2.Distance(transform.position, startingPoint.position) < 0.1f)
            {
                // do nothing
            }
            else
            {
                //turn to face the starting point
                FlipToStartingPoint();
            }
            transform.position = Vector2.MoveTowards(transform.position, startingPoint.position, speed * Time.deltaTime / 2);
        }

        private void Flip()
        {
            if (transform.position.x > player.transform.position.x)
            {
                transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
            }       
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(startingPoint.transform.position, 0.5f);
            //Gizmos.DrawWireSphere(startingPointFlyer.transform.position, 0.5f);
            // Gizmos.DrawLine(pointA.transform.position, pointB.transform.position);
        }

    }


}
