using Game.Gameplay.World;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DoorTriggerInteraction))]
public class DoorLabelHandle : Editor
{
    private static GUIStyle _labelStyle;

    private void OnEnable()
    {
        _labelStyle = new GUIStyle();
        _labelStyle.normal.textColor = Color.white;
        _labelStyle.alignment = TextAnchor.MiddleCenter;
    }

    private void OnSceneGUI()
    {
        var door = (DoorTriggerInteraction)target;
        Handles.Label(door.transform.position + Vector3.up * 2f,
            door.CurrentDoorPosition.ToString(), _labelStyle);
    }
}   