using Game.Gameplay.Camera;
using UnityEngine;

namespace Game.Parallax
{
    [ExecuteInEditMode]
    public class ParallaxAuthoringComponent : MonoBehaviour
    {
        private static readonly Vector3 MassiveExtents = new(100000f, 100000f, 100000f);

        private SpriteRenderer _spriteRenderer;
        private bool _isBoundsInflated;

        private void OnEnable()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            ParallaxOrchestrator.OnAuthoringStateChanged += HandleAuthoringStateChanged;
        }

        private void OnDisable()
        {
            ParallaxOrchestrator.OnAuthoringStateChanged -= HandleAuthoringStateChanged;
            RevertBounds();
        }

        private void HandleAuthoringStateChanged(bool isActive)
        {
            if (isActive && !_isBoundsInflated)
                InflateBounds();
            else if (!isActive && _isBoundsInflated)
                RevertBounds();
        }

        private void InflateBounds()
        {
            if (_spriteRenderer == null) return;
            _spriteRenderer.localBounds = new Bounds(Vector3.zero, MassiveExtents);
            _isBoundsInflated = true;
        }

        private void RevertBounds()
        {
            if (_spriteRenderer == null) return;
            _spriteRenderer.ResetLocalBounds();
            _isBoundsInflated = false;
        }
    }
}   