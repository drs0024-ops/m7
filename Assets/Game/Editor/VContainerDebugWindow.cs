#if UNITY_EDITOR
using Game.Bootstrap;
using UnityEditor;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Editor
{
    public class VContainerDebugWindow : EditorWindow
    {
        private Vector2 _scroll;
        private string _log = "";

        [MenuItem("Window/Game/Debug")]
        public static void Open() => GetWindow<VContainerDebugWindow>("Game Debug");

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Scope lifecycle + message trace. " +
                "Full DI tree: Window → VContainer → Diagnostics",
                MessageType.Info);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("Message Trace (last 50)", EditorStyles.boldLabel);

            var tracer = GetTracer();
            if (tracer != null)
            {
                var entries = tracer.GetRecent(50);
                for (int i = entries.Length - 1; i >= 0; i--)
                    EditorGUILayout.LabelField(entries[i], EditorStyles.wordWrappedLabel);
            }
            else
            {
                EditorGUILayout.LabelField("(No tracer found — is the game running?)");
            }

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Scope Log", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(_log, GUILayout.MinHeight(200));

            EditorGUILayout.EndScrollView();
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            Repaint();
        }

        private MessagePipeTracer GetTracer()
		{
			try
			{
				var scopes = UnityEngine.Object.FindObjectsByType<LifetimeScope>(FindObjectsSortMode.None);
				foreach (var s in scopes)
				{
					if (s.gameObject.scene.name == "Bootstrap")
						return s.Container.Resolve<MessagePipeTracer>();
				}
				return null;
			}
			catch
			{
				return null;
			}
		}     
	}
}
#endif   