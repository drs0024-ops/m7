using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Gameplay.Audio
{
    /// <summary>
    /// Ensures exactly one active AudioListener exists in the scene.
    /// Disables any duplicate listeners introduced by additive scene loads.
    /// </summary>
    public class AudioListenerManager : MonoBehaviour, IDisposable
    {
        #region Fields

        private AudioListener _listener;
        private bool _disposed;
        private bool _warnedInactive;

        #endregion

        #region Unity Lifecycle

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
            if (!_listener.isActiveAndEnabled && !_warnedInactive)
            {
                _warnedInactive = true;
                Debug.LogWarning($"[AudioListenerManager] Listener is NOT active/enabled! GO: {gameObject.name}, active: {gameObject.activeInHierarchy}");
            }
            else if (_listener.isActiveAndEnabled && _warnedInactive)
            {
                _warnedInactive = false;
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion

        #region Public API

        public void SetActive(bool active)
        {
            _listener.enabled = active;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        #endregion

        #region Internal

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

        #endregion
    }
}   