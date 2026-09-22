using UnityEngine;

namespace Game.Core.Interfaces
{
    public class Killable : MonoBehaviour
    {
        public void Kill()
        {
            GetComponent<Animator>().SetTrigger("Die");
            GetComponent<Collider2D>().enabled = false;
            Destroy(gameObject, 2f); // Destroy after animation
        }
    }
}
