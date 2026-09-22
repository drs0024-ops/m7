using UnityEngine;

namespace Game.Gameplay.Parallax
{
    [ExecuteInEditMode]
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private float _parallaxFactor = 1f;

        private Vector3 _initialWorldPosition;

        private void Awake()
        {
            _initialWorldPosition = transform.position;
        }

        public void Move(Vector3 delta)
        {
            Vector3 newPos = _initialWorldPosition;
            newPos.x -= delta.x * _parallaxFactor;
            newPos.y -= delta.y * _parallaxFactor;
            transform.position = newPos;
        }
    }
}   