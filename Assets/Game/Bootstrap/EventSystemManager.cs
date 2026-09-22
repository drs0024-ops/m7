using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Bootstrap
{
    public class EventSystemManager : MonoBehaviour
    {
        private void Awake()
        {
            var existing = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

            for (int i = 1; i < existing.Length; i++)
                Destroy(existing[i].gameObject);

            DontDestroyOnLoad(gameObject);
        }
    }
}   