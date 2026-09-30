using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace Game.Bootstrap
{
    /// <summary>
    /// Logs scope build completion and disposal with timing.
    /// Registered as Lifetime.Scoped in every scope (Project + Scene).
    /// </summary>
    public class ScopeLifecycleLogger : IStartable, IDisposable
    {
        private readonly string _scopeName;
        private readonly Stopwatch _sw = new();
        private bool _logged;

        public ScopeLifecycleLogger()
        {
            _scopeName = Application.isPlaying
                ? SceneManager.GetActiveScene().name
                : "Test";
        }

        void IStartable.Start()
        {
            if (_logged) return;
            _logged = true;
            _sw.Start();
            UnityEngine.Debug.Log($"[DI] Scope '{_scopeName}' ready.");
        }

        public void Dispose()
        {
            if (_sw.IsRunning)
            {
                _sw.Stop();
                UnityEngine.Debug.Log($"[DI] Scope '{_scopeName}' disposed after {_sw.ElapsedMilliseconds}ms.");
            }
        }
    }
}   