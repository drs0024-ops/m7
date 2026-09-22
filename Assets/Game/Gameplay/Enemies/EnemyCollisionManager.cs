using System.Collections.Generic;
using Game.Gameplay.Player;
using UnityEngine;

namespace Game.Gameplay.Enemies
{
    
    public class EnemyCollisionManager : MonoBehaviour
    {
        private List<Enemy> _activeEnemies = new List<Enemy>(); // List to keep track of active enemies
        private bool stop = false;
        private PlayerStateDriverShell player;
        private Rigidbody2D rb;
        private bool activateDistance = false;
        private Enemy _enemy;
        private Vector2 newPos;
        public float desiredSeparation = 1.0f;
        public float relaxation = 0.2f;

        // Singleton instance ---------------------------------
        private static EnemyCollisionManager _instance = null;
        public static EnemyCollisionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<EnemyCollisionManager>();
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("EnemyCollisionManager");
                        _instance = obj.AddComponent<EnemyCollisionManager>();
                    }
                }
                return _instance;
            }
        }  

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
        }

        void Start()
        {
            ///if (GameManager.Instance.PlayerPrefab != null)
        
        }

        void Update() {

            if (!stop)
            {
                //if (GameManager.Instance.PlayerPrefab != null)
                //{
                    //player = GameManager2.Instance.PlayerPrefab;
                //}
                //stop = true;
                //Debug.Log("Player set in ECM");
            }


            
    }

        private void FixedUpdate()
        {
            if (_activeEnemies.Count <= 0) return;
            /*

            // Apply separation to each unique pair
            for (int i = 0; i < _activeEnemies.Count; i++)
            {
                for (int j = i + 1; j < _activeEnemies.Count; j++)
                {
                    Enemy a = _activeEnemies[i];
                    Enemy b = _activeEnemies[j];

                    Vector2 direction = a.transform.position - b.transform.position;
                    float distance = direction.magnitude;

                    if (distance < desiredSeparation && a.IsChasing || b.IsChasing)
                    {
                        Vector2 pushVector = direction.normalized * relaxation;
                        a.RB.position += pushVector;
                        b.RB.position -= pushVector;
                    }
                }
            
            }

        
            Vector3 separation = Vector3.zero;
            float separationDistance = 5f;
            int count = 0;

            foreach (Enemy enemy in _activeEnemies) 
            {
                
                if (enemy.RB != null)
                {
                    rb = enemy.RB;
                }
                if (enemy != this) {

                    float distance = Vector3.Distance(enemy.transform.position, enemy.transform.position);

                    if (distance < separationDistance) {
                        Debug.Log("distance: " + distance + " seperation: "+ separationDistance);
                        Vector3 diff = enemy.transform.position - enemy.transform.position;
                        separation += diff.normalized / distance;
                        count++;
                        //Debug.Log("Enemies are to close ");
                        enemy.ShouldBeSeperating(true);
                        activateDistance = true;
                        
                        _enemy = enemy;
                    }
                    if (distance > separationDistance)
                    {
                        enemy.ShouldBeSeperating(false);
                        activateDistance = false;
                    }
                }   
                
                
            }

            if (count > 0) separation /= count;

            Vector3 seek = (GameManager2.Instance.playerPosition - _enemy.transform.position).normalized * 2;
            Vector3 final = (seek + separation).normalized * 2;

            if (activateDistance)
            {
                newPos = _enemy.transform.position + final * Time.deltaTime;
                newPos.y = GameManager2.Instance.playerPosition.y;

                rb.MovePosition(newPos);
                //transform.position += final * Time.deltaTime; 
            }
            */
            
        }
        void OnGUI() // print var states to game scene
        {
            //GUILayout.Label("ActivateDistance " + activateDistance);
            //GUILayout.Label("NewPos " + newPos);
            //GUILayout.Label("PlayerPosisiton: " + GameManager2.Instance.playerPosition);
        }

        #region Register Enemies
        // Method to register all enemies
        public void RegisterEnemy(Enemy enemy)
        {
            if (!_activeEnemies.Contains(enemy))
            {
                _activeEnemies.Add(enemy);
                //Debug.Log("Enemy registered: " + enemy.name);

            }
        }

        // Method to unregister a player
        public void UnregisterEnemy(Enemy enemy)
        {
            if (_activeEnemies.Contains(enemy))
            {
                _activeEnemies.Remove(enemy);
                //Debug.Log("Enemy deregistered: " + enemy.name);

            }
        }   
        #endregion Register Player

    }
    
}
