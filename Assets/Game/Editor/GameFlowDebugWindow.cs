#if UNITY_EDITOR
using Game.Bootstrap;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// Real-time game flow debug window.
    /// Menu: Window → Game → Flow Debug
    /// </summary>
    public class GameFlowDebugWindow : EditorWindow
    {
        private Vector2 _scrollPos;

        [MenuItem("Window/Game/Flow Debug")]
        public static void Open()
        {
            var window = GetWindow<GameFlowDebugWindow>("Flow Debug");
            window.minSize = new Vector2(320, 400);
        }

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
            Repaint();
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // State
            GUILayout.Label("Game Flow Debug", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("State", GameFlowDebugInfo.CurrentState.ToString(), EditorStyles.textField);
            EditorGUILayout.LabelField("Phase", GameFlowDebugInfo.CurrentPhase.ToString(), EditorStyles.textField);
            EditorGUILayout.LabelField("Input Map", GameFlowDebugInfo.CurrentInputMap, EditorStyles.textField);
            EditorGUILayout.LabelField("Transition", GameFlowDebugInfo.IsTransitioning ? "IN PROGRESS" : "Idle", EditorStyles.textField);

            EditorGUILayout.Space(8);

            // Active Scenes
            GUILayout.Label("Active Scenes", EditorStyles.boldLabel);
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                string label = $"{scene.name} (index {i})";
                if (!scene.isLoaded) label += " [not loaded]";
                EditorGUILayout.LabelField("", label, EditorStyles.textField);
            }

            EditorGUILayout.Space(8);

            // Transition History
            GUILayout.Label("Transition History (last 10)", EditorStyles.boldLabel);
            if (GameFlowDebugInfo.TransitionHistory.Count == 0)
            {
                EditorGUILayout.LabelField("", "(none yet)", EditorStyles.textField);
            }
            else
            {
                for (int i = GameFlowDebugInfo.TransitionHistory.Count - 1; i >= 0; i--)
                {
                    EditorGUILayout.LabelField("", GameFlowDebugInfo.TransitionHistory[i], EditorStyles.textField);
                }
            }

            EditorGUILayout.Space(8);

            // Reset button
            if (GUILayout.Button("Reset History"))
            {
                GameFlowDebugInfo.Reset();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif   