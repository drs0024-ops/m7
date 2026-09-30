#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Unity.Cinemachine;
using Game.Core.Enums;
using Game.Core.Messages;
using MessagePipe;
using Camera = UnityEngine.Camera;

namespace Game.Editor
{
    /// <summary>
    /// Editor-only debug window for inspecting and controlling the camera system.
    /// Window → Game → Camera Debug
    /// </summary>
    public class CameraDebugWindow : EditorWindow
    {
        #region State

        private CameraMode _currentMode;
        private string _activeCamName = "—";
        private string _confinerBounds = "—";
        private Vector3 _damping = Vector3.zero;
        private bool _impulseActive;

        #endregion

        #region Public API

        [MenuItem("Window/Game/Camera Debug")]
        private static void Open()
        {
            GetWindow<CameraDebugWindow>("Camera Debug");
        }

        #endregion

        #region Internal

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (!Application.isPlaying) return;

            RefreshState();
            Repaint();
        }

        private void RefreshState()
        {
            var brain = FindFirstObjectByType<CinemachineBrain>();
            if (brain == null) return;

            var activeVCam = brain.ActiveVirtualCamera;
            if (activeVCam is CinemachineCamera cam)
            {
                _activeCamName = cam.gameObject.name;
                _currentMode = InferModeFromName(cam.gameObject.name);

                var composer = cam.GetComponent<CinemachinePositionComposer>();
                if (composer != null)
                    _damping = composer.Damping;

                var confiner = cam.GetComponent<CinemachineConfiner2D>();
                if (confiner != null && confiner.BoundingShape2D != null)
                {
                    var rect = confiner.BoundingShape2D.bounds;
                    _confinerBounds = $"min({rect.min.x:F1},{rect.min.y:F1}) max({rect.max.x:F1},{rect.max.y:F1})";
                }
                else
                {
                    _confinerBounds = "None";
                }
            }
            else
            {
                _activeCamName = "None";
            }

            var impulse = FindFirstObjectByType<CinemachineImpulseSource>();
            _impulseActive = impulse != null && impulse.ImpulseDefinition.AmplitudeGain > 0f;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Camera Debug", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Active Mode", _currentMode.ToString());
            EditorGUILayout.LabelField("Active vCam", _activeCamName);
            EditorGUILayout.LabelField("Confiner Bounds", _confinerBounds);
            EditorGUILayout.LabelField("Damping (X,Y,Z)", $"{_damping.x:F3}, {_damping.y:F3}, {_damping.z:F3}");
            EditorGUILayout.LabelField("Impulse Active", _impulseActive ? "Yes" : "No");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Force Switch", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("CenterFollow"))
                    ForceSwitch(CameraMode.CenterFollow);
                if (GUILayout.Button("NoYFollow"))
                    ForceSwitch(CameraMode.NoYFollow);
                if (GUILayout.Button("LockedRoom"))
                    ForceSwitch(CameraMode.LockedRoom);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Reset Bounds"))
            {
                var publisher = GlobalMessagePipe.GetPublisher<CameraBoundsResetRequest>();
                publisher.Publish(default);
            }
        }

        private void ForceSwitch(CameraMode mode)
        {
            var publisher = GlobalMessagePipe.GetPublisher<CameraSwitchRequest>();
            publisher.Publish(new CameraSwitchRequest(mode));
        }

        private static CameraMode InferModeFromName(string name)
        {
            if (name.Contains("CenterFollow")) return CameraMode.CenterFollow;
            if (name.Contains("NoYFollow")) return CameraMode.NoYFollow;
            if (name.Contains("LockedRoom")) return CameraMode.LockedRoom;
            return CameraMode.CenterFollow;
        }

        #endregion
    }
}
#endif   