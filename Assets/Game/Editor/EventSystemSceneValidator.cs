#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// Editor-only validator. Warns if any scene other than Bootstrap
    /// contains an EventSystem component. Catches the root cause at
    /// authoring time instead of patching at runtime.
    /// </summary>
    [InitializeOnLoad]
    public static class EventSystemSceneValidator
    {
        private const string BootstrapSceneName = "Bootstrap";

        static EventSystemSceneValidator()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == BootstrapSceneName) return;

            var systems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] == null) continue;
                if (systems[i].gameObject.scene.name == scene.name)
                {
                    Debug.LogWarning(
                        $"[EventSystemValidator] Scene \"{scene.name}\" contains an EventSystem. " +
                        $"Only the Bootstrap scene should have one. Remove it from this scene.",
                        systems[i].gameObject);
                }
            }
        }
    }
}
#endif   