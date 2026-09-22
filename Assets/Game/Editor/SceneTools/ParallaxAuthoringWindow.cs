using System;
using UnityEditor;
using UnityEngine;
using Game.Gameplay.Camera;
using Game.Gameplay.World;

namespace Game.Editor
{
    public class ParallaxAuthoringWindow : EditorWindow
    {
        [SerializeField] private bool _isParallaxEnabled;
        [SerializeField] private bool _driveWithSceneView;
        [SerializeField] private string _editorCameraId;
        [SerializeField] private string _cinemachineCamId;
        [SerializeField] private string _gameCameraAnchorId;
        [SerializeField] private Vector3 _anchorPosition;

        [NonSerialized] private Transform _editorCamera;
        [NonSerialized] private Transform _cinemachineCam;
        [NonSerialized] private Transform _gameCameraAnchor;

        private Vector3 _currentDelta;
        private float _currentZoomRatio = 1f;
        private float _runtimeOrthoSize = 5f;

        [MenuItem("Tools/Parallax Authoring")]
        public static void Open() => GetWindow<ParallaxAuthoringWindow>("Parallax");

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            _editorCamera = RestoreTransform(_editorCameraId);
            _cinemachineCam = RestoreTransform(_cinemachineCamId);
            _gameCameraAnchor = RestoreTransform(_gameCameraAnchorId);

            if (Camera.main != null)
                _runtimeOrthoSize = Camera.main.orthographicSize;

            if (_isParallaxEnabled && !Application.isPlaying)
                EnterAuthoring();

            ParallaxOrchestrator.DriveWithSceneViewActive = _driveWithSceneView;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;

            ParallaxOrchestrator.ToggleAuthoringMode(false);
            ParallaxOrchestrator.DriveWithSceneViewActive = false;
            UpdateCameraStates(false);
        }

        private void OnEditorUpdate()
        {
            Shader.SetGlobalFloat("_IsPlayingState", Application.isPlaying ? 1f : 0f);

            if (!_isParallaxEnabled) return;

            BroadcastCameraData();
            SceneView.RepaintAll();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_isParallaxEnabled || !_driveWithSceneView) return;

            Handles.BeginGUI();
            Vector2 center = new Vector2(sceneView.position.width / 2f, sceneView.position.height / 2f);
            EditorGUI.DrawRect(new Rect(center.x - 16, center.y - 1, 32, 2), Color.white);
            EditorGUI.DrawRect(new Rect(center.x - 1, center.y - 16, 2, 32), Color.white);
            Handles.EndGUI();
        }

        private void OnGUI()
        {
            GUILayout.Label("Parallax Setup", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _editorCamera = (Transform)EditorGUILayout.ObjectField(
                "Editor Camera", _editorCamera, typeof(Transform), true);
            if (EditorGUI.EndChangeCheck()) _editorCameraId = GetId(_editorCamera);

            EditorGUI.BeginChangeCheck();
            _cinemachineCam = (Transform)EditorGUILayout.ObjectField(
                "Cinemachine VCam", _cinemachineCam, typeof(Transform), true);
            if (EditorGUI.EndChangeCheck()) _cinemachineCamId = GetId(_cinemachineCam);

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            _gameCameraAnchor = (Transform)EditorGUILayout.ObjectField(
                "Game Camera Anchor", _gameCameraAnchor, typeof(Transform), true);
            if (EditorGUI.EndChangeCheck()) _gameCameraAnchorId = GetId(_gameCameraAnchor);

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            _isParallaxEnabled = GUILayout.Toggle(_isParallaxEnabled, "Enable Parallax Preview");
            if (EditorGUI.EndChangeCheck())
            {
                ParallaxOrchestrator.ToggleAuthoringMode(_isParallaxEnabled);
                UpdateCameraStates(_isParallaxEnabled);
                if (_isParallaxEnabled) BroadcastCameraData();
                ResetParallaxLayers();
            }

            bool newDrive = GUILayout.Toggle(_driveWithSceneView, "Drive with Scene View");
            if (newDrive != _driveWithSceneView)
            {
                _driveWithSceneView = newDrive;
                ParallaxOrchestrator.DriveWithSceneViewActive = newDrive;
                ResetParallaxLayers();
            }

            if (GUILayout.Button("Update Pivot to Game Cam Pos") && _gameCameraAnchor != null)
            {
                _anchorPosition = _gameCameraAnchor.position;
                BroadcastCameraData();
            }

            EditorGUILayout.Space();
            GUILayout.Label("Debug (Read-Only)", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Vector3Field("Anchor", _anchorPosition);
            EditorGUILayout.Vector3Field("Delta", _currentDelta);
            EditorGUILayout.FloatField("Zoom Ratio", _currentZoomRatio);
            EditorGUI.EndDisabledGroup();
        }

        // --- Internal ---

        private void BroadcastCameraData()
        {
            _currentDelta = Vector3.zero;
            _currentZoomRatio = 1f;

            if (_driveWithSceneView && SceneView.lastActiveSceneView != null)
            {
                Vector3 svPos = SceneView.lastActiveSceneView.camera.transform.position;
                Vector3 gamePos = _gameCameraAnchor != null ? _gameCameraAnchor.position : _anchorPosition;
                _currentDelta = svPos - gamePos;
            }
            else if (_editorCamera != null)
            {
                _currentDelta = _editorCamera.position - _anchorPosition;
                float ortho = Camera.main != null ? Camera.main.orthographicSize : _runtimeOrthoSize;
                _currentZoomRatio = ortho / _runtimeOrthoSize;
            }

            Shader.SetGlobalVector("_EditorCameraDelta",
                new Vector4(_currentDelta.x, _currentDelta.y, 0f, _currentZoomRatio));
        }

        private void UpdateCameraStates(bool active)
        {
            if (_editorCamera != null)
                _editorCamera.gameObject.SetActive(active);

            if (_cinemachineCam != null)
                _cinemachineCam.gameObject.SetActive(!active);
        }

        private void EnterAuthoring()
        {
            ParallaxOrchestrator.ToggleAuthoringMode(true);
            UpdateCameraStates(true);
            BroadcastCameraData();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredPlayMode)
            {
                ParallaxOrchestrator.ToggleAuthoringMode(false);
                UpdateCameraStates(false);
            }
            else if (state == PlayModeStateChange.EnteredEditMode && _isParallaxEnabled)
            {
                EnterAuthoring();
            }
        }

        private void ResetParallaxLayers()
        {
            var layers = FindObjectsByType<ParallaxLayerMine>(FindObjectsSortMode.None);
            for (int i = 0; i < layers.Length; i++)
                layers[i].ResetCameraReference();
        }

        private static string GetId(Transform t)
        {
            if (t == null) return "";
            return GlobalObjectId.GetGlobalObjectIdSlow(t).ToString();
        }

        private static Transform RestoreTransform(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (GlobalObjectId.TryParse(id, out GlobalObjectId gid))
            {
                var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                if (obj is Transform t) return t;
                if (obj is GameObject go) return go.transform;
            }
            return null;
        }
    }
}   