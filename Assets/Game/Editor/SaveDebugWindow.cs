#if UNITY_EDITOR
using System.IO;
using Game.Bootstrap;
using Game.Bootstrap.Scopes;
using Game.Gameplay.Save;
using UnityEditor;
using UnityEngine;
using VContainer;

namespace Game.Editor
{
    public class SaveDebugWindow : EditorWindow
    {
        private Vector2 _scroll;
        private MessagePipeTracer _tracer;
        private SaveManager _saveManager;

        [MenuItem("Window/Game/Save Debug")]
        private static void Open()
        {
            var window = GetWindow<SaveDebugWindow>("Save Debug");
            window.minSize = new Vector2(380, 300);
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
            ResolveServices();
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Save Debug", EditorStyles.boldLabel);
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play mode to monitor save activity.", MessageType.Info);
                return;
            }

            ResolveServices();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (_tracer == null)
            {
                EditorGUILayout.HelpBox("Save tracer unavailable. Check that the Bootstrap scope is running.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField("Latest Checkpoint", _tracer.LastCheckpoint);
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Save Status", _tracer.LastSaveStatus, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Latest Save Event", _tracer.LastSaveEvent, EditorStyles.wordWrappedLabel);
            }

            DrawSaveFileStatus();
            DrawSaveHistory();

            EditorGUILayout.EndScrollView();
        }

        private void DrawSaveFileStatus()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Save File", EditorStyles.boldLabel);
            if (_saveManager == null)
            {
                EditorGUILayout.LabelField("Path", "Save manager unavailable");
                return;
            }

            string path = _saveManager.SaveFilePath;
            bool exists = File.Exists(path);
            EditorGUILayout.LabelField("Path", path, EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("On Disk", exists ? "Yes" : "No");

            if (exists)
            {
                var info = new FileInfo(path);
                EditorGUILayout.LabelField("Last Written", info.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
                EditorGUILayout.LabelField("Size", $"{info.Length:N0} bytes");
            }

            if (GUILayout.Button("Reveal Save Folder"))
                EditorUtility.RevealInFinder(Path.GetDirectoryName(path));
        }

        private void DrawSaveHistory()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Checkpoint and Save Events", EditorStyles.boldLabel);
            if (_tracer == null) return;

            string[] entries = _tracer.GetRecent(128);
            bool found = false;
            for (int i = entries.Length - 1; i >= 0; i--)
            {
                if (!IsSaveEvent(entries[i])) continue;
                found = true;
                EditorGUILayout.LabelField(entries[i], EditorStyles.wordWrappedLabel);
            }

            if (!found)
                EditorGUILayout.LabelField("No checkpoint or save events observed yet.");
        }

        private static bool IsSaveEvent(string entry)
        {
            return entry.Contains("CheckpointReached")
                || entry.Contains("SaveRequest")
                || entry.Contains("SaveCompleted")
                || entry.Contains("SaveFailed")
                || entry.Contains("LoadCompleted")
                || entry.Contains("LoadFailed");
        }

        private void ResolveServices()
        {
            if (!Application.isPlaying) return;

            try
            {
                var rootScope = FindFirstObjectByType<ProjectLifetimeScope>();
                if (rootScope == null) return;

                _tracer = rootScope.Container.Resolve<MessagePipeTracer>();
                _saveManager = rootScope.Container.Resolve<SaveManager>();
            }
            catch
            {
                _tracer = null;
                _saveManager = null;
            }
        }
    }
}
#endif