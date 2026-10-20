#if UNITY_EDITOR
using UnityEngine;
using Unity.Cinemachine;
using UnityEditor;
using VContainer;

namespace Game.Gameplay.Camera
{
    /// <summary>
    /// Editor-only camera driver for visual parallax authoring. Its runtime guard
    /// disables the complete editor-camera rig if it is present in a play scene.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class SceneViewDriverDisabler : MonoBehaviour
    {
        #region Dependencies

        [Tooltip("If assigned, the camera follows this object. If null, uses Scene View.")]
        [SerializeField] private Transform _trackObject;
        private CameraSwitcher _switcher;

        #endregion

        #region State

        [Range(1f, 30f)]
        [SerializeField] private float _smoothing = 12f;

        private UnityEngine.Camera _unityCamera;
        private CinemachineBrain _brain;
        private Vector3 _savedDamping;
        private bool _savedBrainEnabled;
        private bool _savedConfinerEnabled;
        private CinemachinePositionComposer _activeComposer;
        private CinemachineConfiner2D _activeConfiner;
        private bool _isDriving;

        #endregion

        #region Public API

        [Inject]
        public void Construct(CameraSwitcher switcher)
        {
            _switcher = switcher;
        }

        #endregion

        #region Internal

        private void Awake()
        {
            if (Application.isPlaying)
            {
                Transform rigRoot = transform.parent != null ? transform.parent : transform;
                Debug.LogWarning(
                    $"[SceneViewDriverDisabler] Editor camera rig '{rigRoot.name}' found in Play Mode; disabling it.",
                    rigRoot.gameObject);
                rigRoot.gameObject.SetActive(false);
                return;
            }

            _unityCamera = GetComponent<UnityEngine.Camera>();
            _unityCamera.depth = -1;
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        // Brain/vcams disabled while driving would otherwise be serialized into the play session.
        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode && _isDriving)
                RestoreState();
        }

        private void LateUpdate()
        {
            if (Application.isPlaying)
            {
                if (_isDriving) RestoreState();
                return;
            }

            bool shouldDrive = ParallaxOrchestrator.DriveWithSceneViewActive
                            && SceneView.lastActiveSceneView != null;

            if (shouldDrive && !_isDriving)
                EnterEditMode();
            else if (!shouldDrive && _isDriving)
            {
                RestoreState();
                return;
            }

            if (_isDriving)
                SyncCamera();
        }

        private void EnterEditMode()
        {
            _isDriving = true;

            _activeComposer = _switcher?.CurrentComposer;
            _activeConfiner = _switcher?.CurrentConfiner;

            if (_activeComposer != null)
                _savedDamping = _activeComposer.Damping;

            _brain = FindFirstObjectByType<CinemachineBrain>();
            if (_brain != null)
            {
                _savedBrainEnabled = _brain.enabled;
                _brain.enabled = false;
            }

            if (_activeConfiner != null)
            {
                _savedConfinerEnabled = _activeConfiner.enabled;
                _activeConfiner.enabled = false;
            }

            if (_activeComposer != null)
                _activeComposer.Damping = Vector3.zero;

            var allVcams = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
            for (int i = 0; i < allVcams.Length; i++)
                allVcams[i].enabled = false;
        }

        private void SyncCamera()
        {
            if (_trackObject != null)
            {
                transform.SetPositionAndRotation(
                    _trackObject.position,
                    _trackObject.rotation);
            }
            else
            {
                var svCam = SceneView.lastActiveSceneView?.camera;
                if (svCam == null) return;

                float t = 1f - Mathf.Exp(-_smoothing * Time.deltaTime);
                transform.position = Vector3.Lerp(transform.position, svCam.transform.position, t);
                transform.rotation = Quaternion.Slerp(transform.rotation, svCam.transform.rotation, t);
            }
        }

        private void RestoreState()
        {
            if (_brain != null)
                _brain.enabled = _savedBrainEnabled;

            if (_activeConfiner != null)
                _activeConfiner.enabled = _savedConfinerEnabled;

            if (_activeComposer != null)
                _activeComposer.Damping = _savedDamping;

            var allVcams = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
            for (int i = 0; i < allVcams.Length; i++)
                allVcams[i].enabled = true;

            _isDriving = false;
        }

        private void OnDestroy()
        {
            if (_isDriving) RestoreState();
        }

        #endregion
    }
}
#endif   