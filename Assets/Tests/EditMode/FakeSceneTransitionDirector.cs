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

        public async UniTask StartTransition(
            SceneField scene,
            Action onComplete,
            bool spawnAtDoor,
            DoorToSpawnAt door,
            bool fromRight,
            LevelStateSnapshot? snapshot = null)
        {
            LastLoadedScene = scene.SceneName;
            onComplete?.Invoke();
            await UniTask.CompletedTask;
        }

        public UniTask StartTransition(
        SceneField scene,
        Action onComplete,
        bool spawnAtDoor,
        DoorToSpawnAt door,
        bool fromRight,
        LevelStateSnapshot? snapshot = null,
        string sourceSceneToUnload = null)
        {
            // your existing fake logic here
            return UniTask.CompletedTask;
        }   
    }
}   