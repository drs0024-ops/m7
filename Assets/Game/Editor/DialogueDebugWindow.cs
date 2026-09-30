#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Game.Core.Messages;
using MessagePipe;
using Game.UI;

namespace Game.Editor
{
    /// <summary>
    /// Editor-only debug window for the dialogue system.
    /// Window → Game → Dialogue Debug
    /// </summary>
    public class DialogueDebugWindow : EditorWindow
    {
        #region State

        private string _state = "—";
        private bool _isActive;

        #endregion

        #region Public API

        [MenuItem("Window/Game/Dialogue Debug")]
        private static void Open()
        {
            GetWindow<DialogueDebugWindow>("Dialogue Debug");
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
            if (!Application.isPlaying)
            {
                _state = "Not Playing";
                _isActive = false;
                return;
            }

            var controller = FindFirstObjectByType<DialogueController>();
            if (controller != null)
            {
                _state = controller.State.ToString();
                _isActive = controller.IsConversationActive;
            }
            else
            {
                _state = "Controller Not Found";
                _isActive = false;
            }

            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dialogue Debug", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("State", _state);
            EditorGUILayout.LabelField("Active", _isActive ? "Yes" : "No");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Force End"))
                {
                    var controller = FindFirstObjectByType<DialogueController>();
                    if (controller != null)
                    {
                        var method = typeof(DialogueController).GetMethod(
                            "EndConversation",
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        method?.Invoke(controller, null);
                    }
                }

                if (GUILayout.Button("Force Advance"))
                {
                    var controller = FindFirstObjectByType<DialogueController>();
                    if (controller != null)
                    {
                        var method = typeof(DialogueController).GetMethod(
                            "Advance",
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        method?.Invoke(controller, null);
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Publish Test Dialogue", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Use a DialogueDataSO asset to publish DialogueRequested and test the full pipeline.",
                MessageType.Info);
        }

        #endregion
    }
}
#endif   