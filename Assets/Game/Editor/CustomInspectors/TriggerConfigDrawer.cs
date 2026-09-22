using UnityEngine;
using Game.Gameplay.Camera;
using UnityEditor;

[CustomPropertyDrawer(typeof(TriggerConfig))]
public class TriggerConfigDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, "Trigger Configuration", true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            var swap = property.FindPropertyRelative("swapCameras");
            var pan = property.FindPropertyRelative("panCameraOnContact");

            EditorGUILayout.PropertyField(swap, new GUIContent("Swap Cameras"));

            if (swap.boolValue)
                EditorGUILayout.PropertyField(property.FindPropertyRelative("switchData"), new GUIContent("Switch Data"));

            EditorGUILayout.PropertyField(pan, new GUIContent("Pan on Contact"));

            if (pan.boolValue)
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("panDirection"), new GUIContent("Pan Direction"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("panDistance"), new GUIContent("Pan Distance"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("panTime"), new GUIContent("Pan Time"));
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }
}   