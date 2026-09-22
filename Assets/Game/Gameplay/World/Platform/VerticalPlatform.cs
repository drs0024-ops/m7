using UnityEngine;

namespace Game.Gameplay.World
{
    [RequireComponent(typeof(PlatformEffector2D))]
    public class VerticalPlatform : MonoBehaviour
    {
        [SerializeField] private float _holdTime = 0.5f;

        private PlatformEffector2D _effector;
        private float _holdTimer;
        private bool _isFlipped;

        private void Start()
        {
            _effector = GetComponent<PlatformEffector2D>();
        }

        private void Update()
        {
            bool downHeld = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
            bool jumpPressed = Input.GetKeyDown(KeyCode.Space);

            if (downHeld && !_isFlipped)
            {
                _holdTimer -= Time.deltaTime;
                if (_holdTimer <= 0f)
                    FlipDown();
            }
            else if (!downHeld && !_isFlipped)
            {
                _holdTimer = _holdTime;
            }

            if (jumpPressed && _isFlipped)
                FlipUp();
        }

        private void FlipDown()
        {
            _isFlipped = true;
            _effector.rotationalOffset = 180f;
        }

        private void FlipUp()
        {
            _isFlipped = false;
            _effector.rotationalOffset = 0f;
            _holdTimer = _holdTime;
        }
    }
}   