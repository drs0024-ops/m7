using System;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Core.Enums;
using Game.Core.Interfaces;

namespace Game.Tests
{
    public class FakeSceneTransitionDirector : ISceneTransitionDirector
    {
        public string LastLoadedScene;
        public string LastSourceSceneToUnload;
        public TransitionResult NextResult = TransitionResult.Ok;

        public async UniTask<TransitionResult> StartTransition(
            SceneField scene,
            Action onComplete,
            bool spawnAtDoor,
            DoorToSpawnAt door,
            bool fromRight,
            LevelStateSnapshot? snapshot = null,
            string sourceSceneToUnload = null)
        {
            LastLoadedScene = scene.SceneName;
            LastSourceSceneToUnload = sourceSceneToUnload;
            onComplete?.Invoke();
            await UniTask.CompletedTask;
            return NextResult;
        }
    }
}   