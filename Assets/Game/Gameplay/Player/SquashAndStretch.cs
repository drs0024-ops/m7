using UnityEngine;

namespace Game.Gameplay.Player
{
    public class SquashAndStretch : MonoBehaviour
    {
        [SerializeField] private Transform _sprite;
        [SerializeField] private float _stretch = 0.1f;
        [SerializeField] private Transform _squashParent;

        private Rigidbody2D _rb;
        private Vector3 _originalScale;
        private GameObject _squashObject;

        private void Start()
        {
            _rb = GetComponent<Rigidbody2D>();
            _originalScale = _sprite.localScale;

            if (_squashParent == null)
            {
                _squashObject = new GameObject($"_squash_{name}");
                _squashParent = _squashObject.transform;
                _squashParent.SetParent(transform, false);
            }
        }

        private void Update()
        {
            _sprite.parent = transform;
            _sprite.localPosition = Vector3.zero;
            _sprite.localScale = _originalScale;
            _sprite.localRotation = Quaternion.identity;

            _squashParent.position = transform.position;

            Vector3 velocity = _rb.linearVelocity;
            if (velocity.sqrMagnitude > 0.01f)
            {
                _squashParent.rotation = Quaternion.FromToRotation(Vector3.right, velocity);
            }
            else
            {
                _squashParent.rotation = Quaternion.identity;
            }

            float scaleX = 1.0f + (velocity.magnitude * _stretch);
            float scaleY = 1.0f / scaleX;

            _sprite.parent = _squashParent;
            _squashParent.localScale = new Vector3(scaleX, scaleY, 1.0f);
        }

        private void OnDestroy()
        {
            if (_squashObject != null)
                Destroy(_squashObject);
        }
    }
}   