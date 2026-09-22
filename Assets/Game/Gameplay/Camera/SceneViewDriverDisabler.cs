#if UNITY_EDITOR
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Camera
{
    public class SceneViewDriverDisabler : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _editorPreviewCamera;
        [SerializeField] private string _editorCameraTag = "EditorCam";

        private CameraSwitcher _switcher;

        private UnityEngine.Camera _unityCamera;
        private CinemachineBrain _brain;
        private Vector3 _savedDamping;
        private bool _savedBrainEnabled;
        private bool _savedConfinerEnabled;
        private bool _isDriving;

        [Inject]
        public void Construct(CameraSwitcher switcher)
        {
            _switcher = switcher;
        }

        private void Awake()
        {
            _unityCamera = GetComponent<UnityEngine.Camera>();

            if (_editorPreviewCamera == null)
            {
                var camObj = GameObject.FindGameObjectWithTag(_editorCameraTag);
                if (camObj != null)
                    _editorPreviewCamera = camObj.GetComponent<CinemachineCamera>();
            }

            if (_editorPreviewCamera != null)
                _editorPreviewCamera.gameObject.SetActive(false);
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

            if (shouldDrive) EnterEditMode();
            else if (_isDriving) RestoreState();
        }

        private void EnterEditMode()
        {
            if (_isDriving) return;

            var composer = _switcher.CurrentComposer;
            var confiner = _switcher.CurrentConfiner;

            // Save state
            if (composer != null) _savedDamping = composer.Damping;
            _brain = FindFirstObjectByType<CinemachineBrain>();
            if (_brain != null) _savedBrainEnabled = _brain.enabled;
            if (confiner != null) _savedConfinerEnabled = confiner.enabled;

            // Disable persistent system
            if (_brain != null) _brain.enabled = false;
            if (confiner != null) confiner.enabled = false;
            if (composer != null) composer.Damping = Vector3.zero;

            _isDriving = true;

            // Sync to scene view
            var sceneViewCam = SceneView.lastActiveSceneView.camera;
            if (sceneViewCam == null) return;

            if (_editorPreviewCamera != null)
            {
                _editorPreviewCamera.gameObject.SetActive(true);
                _editorPreviewCamera.transform.SetPositionAndRotation(
                    sceneViewCam.transform.position, sceneViewCam.transform.rotation);

                var cam = _editorPreviewCamera.GetComponent<UnityEngine.Camera>();
                if (cam != null && sceneViewCam.orthographic)
                    cam.orthographicSize = sceneViewCam.orthographicSize;
            }

            if (_unityCamera != null)
            {
                transform.SetPositionAndRotation(
                    sceneViewCam.transform.position, sceneViewCam.transform.rotation);
            }
        }

        private void RestoreState()
        {
            var composer = _switcher.CurrentComposer;
            var confiner = _switcher.CurrentConfiner;

            if (_brain != null) _brain.enabled = _savedBrainEnabled;
            if (confiner != null) confiner.enabled = _savedConfinerEnabled;
            if (composer != null) composer.Damping = _savedDamping;

            if (_editorPreviewCamera != null)
                _editorPreviewCamera.gameObject.SetActive(false);

            _isDriving = false;
        }

        private void OnDestroy()
        {
            if (_isDriving) RestoreState();
        }
    }
}
#endif   