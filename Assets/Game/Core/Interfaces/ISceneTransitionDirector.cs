using System;
using Cysharp.Threading.Tasks;
using Game.Core.Enums;

namespace Game.Core.Interfaces
{
    public interface ISceneTransitionDirector
    {
        UniTask<TransitionResult> StartTransition(
            SceneField scene,
            Action onComplete,
            bool spawnAtDoor,
            DoorToSpawnAt door,
            bool fromRight,
            LevelStateSnapshot? snapshot = null,
            string sourceSceneToUnload = null);
    }
}   