using UnityEngine;
using VContainer;
using Unity.Cinemachine;

namespace Game.Gameplay.Camera
{
    public class CameraBoundsManager
    {
        private readonly CameraConfigSO _config;
        private Collider2D _currentBoundary;

        private CinemachineConfiner2D[] _confiners;

        [Inject]
        public CameraBoundsManager(CameraConfigSO config)
        {
            _config = config;
            _confiners = new[]
            {
                config.Center?.GetComponent<CinemachineConfiner2D>(),
                config.NoY?.GetComponent<CinemachineConfiner2D>(),
                config.Locked?.GetComponent<CinemachineConfiner2D>()
            };
        }

        public void SetBoundary(Collider2D boundary)
        {
            _currentBoundary = boundary;
            Refresh();
        }

        public void Refresh()
        {
            if (_currentBoundary == null) return;

            for (int i = 0; i < _confiners.Length; i++)
            {
                if (_confiners[i] != null)
                    _confiners[i].BoundingShape2D = _currentBoundary;
            }
        }
    }
}   