using UnityEditor;
using UnityEngine;

namespace Game.Gameplay.Camera
{
    public static class ParallaxOrchestrator
    {
        public static event System.Action<bool> OnAuthoringStateChanged;

        public static bool DriveWithSceneViewActive { get; set; }

        public static void ToggleAuthoringMode(bool activate)
        {
            DriveWithSceneViewActive = activate;

            if (activate)
                Shader.EnableKeyword("_EDITOR_PARALLAX_ON");
            else
                Shader.DisableKeyword("_EDITOR_PARALLAX_ON");

            SceneView.RepaintAll();
            OnAuthoringStateChanged?.Invoke(activate);
        }
    }
}   