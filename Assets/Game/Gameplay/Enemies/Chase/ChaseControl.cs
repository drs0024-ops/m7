using Game.Gameplay.Enemies;
using UnityEngine;


public class ChaseControl : MonoBehaviour
{
    //private PlayerInvisibility playerInvisibilty;
    public FlyingEnemy[] enemyArray;
    private bool playerInRange;


    void Start()
    {
        //playerInvisibilty = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInvisibility>();
    }


    void Update()
    {
        if (playerInRange)
        {
            //Debug.Log("player invissible: " + playerInvisibilty.IsInvisable);
            //if (playerInRange && playerInvisibilty.IsInvisable)
            //{
              //  StopChase();
            //}
            //if (!playerInvisibilty.IsInvisable && playerInRange)
            //{
            //    StartChase();
            //}
        }

        if (!playerInRange)
        {
            StopChase();
        }


    }
    private void StopChase()
    {
        foreach (FlyingEnemy enemy in enemyArray)
            {
                //Debug.Log("Flying Enemy: " + enemy);
                enemy.chase = false;
            }
    }

    private void StartChase ()
    {
        foreach (FlyingEnemy enemy in enemyArray)
            {
                //Debug.Log("Chase started");
                enemy.chase = true;
            }
    }

    public bool PlayerIsInvissible()
    {
        //if (playerInvisibilty.IsInvisable)
        //{
        //    return true;
        //}
        return false;
    } 

    private void OnTriggerEnter2D(Collider2D collision)
    {
        
        if (collision.CompareTag("Player"))
        {
            //Debug.Log("oject collieded with ChaseControl is: " + collision.gameObject.name);
            playerInRange = true;
            //Debug.Log("Player collided with ChaseControl");
            foreach (FlyingEnemy enemy in enemyArray)
            {
                Debug.Log("Flying Enemy: " + enemy);
                enemy.chase = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;
            foreach (FlyingEnemy enemy in enemyArray)
            {
                enemy.chase = false;
            }
        }
    }
}
    


