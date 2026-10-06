#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Game.Bootstrap;

namespace Game.Editor
{
    public class InjectionValidatorWindow : EditorWindow
    {
        private Vector2 _scrollPos;
        private float _refreshRate = 0.5f;
        private float _lastRefresh;

        [MenuItem("Window/Game/Injection Validator")]
        public static void Open()
        {
            GetWindow<InjectionValidatorWindow>("Injection Validator");
        }
		
        private void Update()
		{
			if (!EditorApplication.isPlaying || EditorApplication.isPaused) return;
			if (Time.unscaledTime - _lastRefresh > _refreshRate)
			{
				_lastRefresh = Time.unscaledTime;
				Repaint();
			}
		}

        private void OnGUI()
        {
            GUILayout.Label("Injection Validator", EditorStyles.boldLabel);
            GUILayout.Space(4);

            if (!InjectionValidator.HasRun)
            {
                EditorGUILayout.HelpBox("Waiting for validation to complete...\n(Start the game and wait 2 frames)", MessageType.Info);
                return;
            }

            if (InjectionValidator.Failures.Count == 0)
            {
                EditorGUILayout.HelpBox("✅ All [Inject] fields resolved.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                $"❌ {InjectionValidator.Failures.Count} unresolved [Inject] field(s):",
                MessageType.Error);

            GUILayout.Space(4);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            foreach (var f in InjectionValidator.Failures)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"{f.ComponentName}.{f.FieldName}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"  Type: {f.FieldType}");
                EditorGUILayout.LabelField($"  Object: {f.GameObjectName}");
                EditorGUILayout.LabelField($"  Scene: {f.SceneName}");
                EditorGUILayout.EndVertical();
                GUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif   