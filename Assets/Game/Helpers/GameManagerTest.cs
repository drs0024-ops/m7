using UnityEngine;

public class GameManagerTest : MonoBehaviour
{
    //GameManager GM;

    void Awake()
    {
        //GM = GameManager.Instance;
        //GM.OnStateChange += HandleOnStateChange;


        //Debug.Log("Current game state when Awakes: " + GM.gameState);
        //GM.ChangeState(GameState.Intro);
    }

    void Start()
    {
        //Debug.Log("Current game state when Starts: " + GM.gameState);
    }

    void Update()
    {
        
    }

    public void HandleOnStateChange()
    {
        //Debug.Log("Handling state change to: " + GM.gameState);

    }
}
