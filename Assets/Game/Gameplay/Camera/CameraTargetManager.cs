using Unity.Cinemachine;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Camera
{
    public class CameraTargetManager
    {
        private readonly CameraConfigSO _config;

        [Inject]
        public CameraTargetManager(CameraConfigSO config)
        {
            _config = config;
        }

        public void UpdateTargets(Transform follow, Collider2D boundary)
        {
            UpdateCamera(_config.Center, follow, boundary);
            UpdateCamera(_config.NoY, follow, boundary);
            UpdateCamera(_config.Locked, follow, boundary);
        }

        public void ResetBounds()
        {
            SetBoundary(_config.Center, null);
            SetBoundary(_config.NoY, null);
            SetBoundary(_config.Locked, null);
        }

        private void UpdateCamera(CinemachineCamera cam, Transform follow, Collider2D boundary)
        {
            if (cam == null) return;

            if (follow != null)
                cam.Follow = follow;

            SetBoundary(cam, boundary);
        }

        private void SetBoundary(CinemachineCamera cam, Collider2D boundary)
        {
            if (cam == null) return;

            var confiner = cam.GetComponent<CinemachineConfiner2D>();
            if (confiner == null) return;

            if (confiner.BoundingShape2D != boundary)
            {
                confiner.BoundingShape2D = boundary;
                confiner.InvalidateBoundingShapeCache();
            }
        }
    }
}   