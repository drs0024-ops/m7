using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Gameplay.Audio
{
    public class AudioListenerManager : MonoBehaviour, IDisposable
    {
        private AudioListener _listener;
        private bool _disposed;

        private void Awake()
        {
            _listener = GetComponent<AudioListener>();
            if (_listener == null)
                _listener = gameObject.AddComponent<AudioListener>();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            EnforceSingleListener();
            
        }

        private void Update()
        {
            if (!_listener.isActiveAndEnabled)
                Debug.LogWarning($"[AudioListenerManager] Listener is NOT active/enabled! GO: {gameObject.name}, active: {gameObject.activeInHierarchy}");
        }   

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (!_disposed) Dispose();
        }
 
        public void SetActive(bool active)
        {
            _listener.enabled = active;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive)
                EnforceSingleListener();
        }

        private void EnforceSingleListener()
        {
            var all = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].gameObject != gameObject && all[i].enabled)
                    all[i].enabled = false;
            }
        }
    }
}   