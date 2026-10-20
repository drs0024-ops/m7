#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public sealed class EnemyDebugWindow : EditorWindow
    {
        private EnemyDebugPanel _panel;

        [MenuItem("Window/Game/Enemy Debug")]
        private static void Open()
        {
            var window = GetWindow<EnemyDebugWindow>("Enemy Debug");
            window.minSize = new Vector2(720f, 420f);
        }

        private void OnEnable()
        {
            _panel = new EnemyDebugPanel();
            _panel.Enable();
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        { 
            EditorApplication.update -= OnEditorUpdate;
            _panel?.Dispose();
            _panel = null;
        }

        private void OnEditorUpdate()
        {
            _panel?.Refresh();
            Repaint();
        }

        private void OnGUI()
        {
            _panel?.DrawContents(position.width - 16f);
        }
    }
}
#endif