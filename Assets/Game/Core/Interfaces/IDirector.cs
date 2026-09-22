using Cysharp.Threading.Tasks;

namespace Game.Core.Interfaces
{
    public interface IDirector
    {
        UniTask LoadSceneAdditiveAsync(string sceneName);
        UniTask UnloadSceneAsync(string sceneName);
        void SetActiveScene(string sceneName);
    }
}   