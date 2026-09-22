using UnityEngine;

namespace Game.Parallax
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class BackgroundScroller : MonoBehaviour
    {
        [SerializeField] private float _scrollSpeed = 0.5f;

        private SpriteRenderer _renderer;
        private Vector2 _offset;

        private void Start()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            _offset.x += _scrollSpeed * Time.deltaTime;
            _renderer.material.mainTextureOffset = _offset;
        }
    }
}   