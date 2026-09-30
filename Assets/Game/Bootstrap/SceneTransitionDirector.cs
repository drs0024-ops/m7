using System;
using Cysharp.Threading.Tasks;
using Game.Core.Enums;
using Game.Core;
using Game.Core.Interfaces;
using UnityEngine;

namespace Game.Bootstrap
{
    /// <summary>
    /// Thin facade over SceneTransitionOrchestrator.
    /// Accepts a SceneField and delegates to the orchestrator.
    /// </summary>
    public class SceneTransitionDirector : ISceneTransitionDirector
    {
        private readonly SceneTransitionOrchestrator _orchestrator;

        public SceneTransitionDirector(SceneTransitionOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }

        public async UniTask StartTransition(
            SceneField scene,
            Action onComplete,
            bool spawnAtDoor,
            DoorToSpawnAt door,
            bool fromRight,
            LevelStateSnapshot? snapshot = null,
            string sourceSceneToUnload = null)
        {
            if (scene == null || !scene.IsValid())
            {
                Debug.LogWarning("[TransitionDirector] Invalid scene field, aborting.");
                return;
            }

            try
            {
                await _orchestrator.TransitionAsync(
                    scene.SceneName, onComplete, spawnAtDoor, door, fromRight,
                    sourceSceneToUnload, snapshot);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TransitionDirector] Transition to '{scene.SceneName}' failed: {e}");
                // Do NOT call onComplete on failure — caller must handle via error state.
            }
        }
    }
}   