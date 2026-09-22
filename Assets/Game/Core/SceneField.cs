using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core
{
    [Serializable]
    public struct SceneField : ISerializationCallbackReceiver
    {
        [SerializeField] private UnityEngine.Object _sceneAsset;
        [SerializeField, HideInInspector] private string _scenePath;

        public string ScenePath => _scenePath;
        public string SceneName => string.IsNullOrEmpty(_scenePath)
            ? ""
            : System.IO.Path.GetFileNameWithoutExtension(_scenePath);

        public static implicit operator string(SceneField field) => field.ScenePath;

        public SceneField(string scenePath)
        {
            _scenePath = scenePath;
            _sceneAsset = null;
        }

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(_scenePath)) return false;
            return SceneUtility.GetBuildIndexByScenePath(_scenePath) != -1;
        }

        public bool IsLoaded => SceneManager.GetSceneByPath(_scenePath).IsValid();

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (_sceneAsset != null)
                _scenePath = UnityEditor.AssetDatabase.GetAssetPath(_sceneAsset);
#endif
        }

        public void OnAfterDeserialize() { }
    }
}   