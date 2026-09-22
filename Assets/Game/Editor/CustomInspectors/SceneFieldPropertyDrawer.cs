using Game.Core;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SceneField))]
public class SceneFieldPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var sceneAssetProp = property.FindPropertyRelative("_sceneAsset");

        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        if (sceneAssetProp != null)
        {
            sceneAssetProp.objectReferenceValue = EditorGUI.ObjectField(
                position,
                sceneAssetProp.objectReferenceValue,
                typeof(SceneAsset),
                false);
        }

        EditorGUI.EndProperty();
    }
}   