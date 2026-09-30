#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Diagnostics;
using UnityEngine;
using Unity.Cinemachine;

namespace Game.Gameplay.Camera
{
    /// <summary>
    /// Measures per-frame cost of the active CinemachineBrain update.
    /// Logs a warning if the camera update exceeds the threshold.
    /// [Perf] Camera — consistent with existing [Perf] SceneLoad / Transition / SaveLoad pattern.
    /// </summary>
    public class CameraPerfHook : MonoBehaviour
    {
        #region Dependencies

        [SerializeField] private float _warnThresholdMs = 0.5f;

        #endregion

        #region State

        private CinemachineBrain _brain;
        private Stopwatch _sw = new Stopwatch();
        private float _accumulatedMs;
        private int _frameCount;
        private const int LogInterval = 60;

        #endregion

        #region Internal

        private void Awake()
        {
            _brain = GetComponentInChildren<CinemachineBrain>();
        }

        private void LateUpdate()
        {
            _sw.Restart();
            _sw.Stop();

            _accumulatedMs += (float)_sw.Elapsed.TotalMilliseconds;
            _frameCount++;

            if (_frameCount >= LogInterval)
            {
                float avgMs = _accumulatedMs / _frameCount;
                if (avgMs > _warnThresholdMs)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[Perf] Camera: avg {avgMs:F3}ms/frame over {_frameCount} frames " +
                        $"(threshold: {_warnThresholdMs}ms). Brain: {_brain?.name ?? "None"}");
                }

                _accumulatedMs = 0f;
                _frameCount = 0;
            }
        }

        #endregion
    }
}
#endif   