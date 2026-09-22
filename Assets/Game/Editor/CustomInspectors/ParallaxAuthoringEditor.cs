using UnityEditor;
using UnityEngine;
using Game.Parallax;

[CustomEditor(typeof(ParallaxAuthoringComponent))]
[CanEditMultipleObjects]
public class ParallaxAuthoringEditor : Editor
{
    private void OnSceneGUI()
    {
        if (!Shader.IsKeywordEnabled("_EDITOR_PARALLAX_ON") || Tools.current != Tool.Move)
        {
            Tools.hidden = false;
            return;
        }

        var comp = (ParallaxAuthoringComponent)target;
        var sr = comp.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sharedMaterial == null) return;

        Tools.hidden = true;

        Vector4 camDelta = Shader.GetGlobalVector("_EditorCameraDelta");
        float speed = sr.sharedMaterial.HasProperty("_ParallaxSpeed")
            ? sr.sharedMaterial.GetFloat("_ParallaxSpeed")
            : 100f;
        float factor = (100f - speed) / 100f;
        float zoomRatio = camDelta.w == 0f ? 1f : camDelta.w;

        Vector3 offset = new Vector3(
            (camDelta.x * factor) / zoomRatio,
            (camDelta.y * factor) / zoomRatio,
            0f);

        Vector3 visualPosition = comp.transform.position + offset;

        EditorGUI.BeginChangeCheck();
        Vector3 newVisualPosition = Handles.PositionHandle(visualPosition, Quaternion.identity);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(comp.transform, "Move Parallax Sprite");
            comp.transform.position += newVisualPosition - visualPosition;
        }
    }

    private void OnDisable()
    {
        Tools.hidden = false;
    }
}   