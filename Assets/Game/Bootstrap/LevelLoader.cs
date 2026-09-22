// Loads/unloads level scenes, creates LevelLifetimeScope, spawns and injects the HUD prefab.
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Bootstrap.Scopes;
using System.Threading;
using VContainer.Unity;

namespace Game.Bootstrap
{
    public class LevelLoader : MonoBehaviour
    {
        [SerializeField] private LevelLifetimeScope _levelScopePrefab;
        [SerializeField] private ProjectLifetimeScope _projectScope;
        [SerializeField] private GameObject _hudPrefab;

        private LevelLifetimeScope _currentLevelScope;
        private GameObject _hudInstance;
        private readonly List<Scene> _loadedScenes = new(4);
        private CancellationTokenSource _cts;

        public LevelLifetimeScope CurrentLevelScope => _currentLevelScope;

        private void Awake()
        {
            _cts = new CancellationTokenSource();
        }

        public async UniTask LoadLevelAsync(string levelSceneName, string[] additiveScenes)
        {
            if (_projectScope == null)
            {
                Debug.LogError("[LevelLoader] _projectScope is not assigned. Check Inspector.");
                return;
            }
            if (string.IsNullOrEmpty(levelSceneName))
            {
                Debug.LogError("[LevelLoader] levelSceneName is null or empty.");
                return;
            }

            await UnloadLevelAsync();

            _currentLevelScope = Instantiate(_levelScopePrefab, _projectScope.transform);

            var loadOp = SceneManager.LoadSceneAsync(levelSceneName, LoadSceneMode.Single);
            await UniTask.WaitUntil(() => loadOp.isDone);

            if (additiveScenes != null)
            {
                for (int i = 0; i < additiveScenes.Length; i++)
                {
                    var op = SceneManager.LoadSceneAsync(additiveScenes[i], LoadSceneMode.Additive);
                    await UniTask.WaitUntil(() => op.isDone);

                    var scene = SceneManager.GetSceneByPath(additiveScenes[i]);
                    if (scene.IsValid())
                        _loadedScenes.Add(scene);
                }
            }

            _hudInstance = Instantiate(_hudPrefab, transform);
            _hudInstance.name = "HUD";
            _currentLevelScope.Container.InjectGameObject(_hudInstance);
        }   

        private IEnumerator LoadLevelInternal(string levelSceneName, string[] additiveScenes, UniTaskCompletionSource tcs)
        {
            yield return UnloadLevelInternal();

            _currentLevelScope = Instantiate(_levelScopePrefab, _projectScope.transform);

            var loadOp = SceneManager.LoadSceneAsync(levelSceneName, LoadSceneMode.Single);
            while (!loadOp.isDone) yield return null;

            if (additiveScenes != null)
            {
                for (int i = 0; i < additiveScenes.Length; i++)
                {
                    var op = SceneManager.LoadSceneAsync(additiveScenes[i], LoadSceneMode.Additive);
                    while (!op.isDone) yield return null;

                    var scene = SceneManager.GetSceneByPath(additiveScenes[i]);
                    if (scene.IsValid())
                        _loadedScenes.Add(scene);
                }
            }

            // Spawn HUD and inject all child MonoBehaviours
            _hudInstance = Instantiate(_hudPrefab, transform);
            _hudInstance.name = "HUD";
            _currentLevelScope.Container.InjectGameObject(_hudInstance);

            tcs.TrySetResult();
        }

        public UniTask UnloadLevelAsync()
        {
            var tcs = new UniTaskCompletionSource();
            StartCoroutine(UnloadLevelInternalAsync(tcs));
            return tcs.Task;
        }

        private IEnumerator UnloadLevelInternalAsync(UniTaskCompletionSource tcs)
        {
            yield return UnloadLevelInternal();
            tcs.TrySetResult();
        }

        private IEnumerator UnloadLevelInternal()
        {
            if (_hudInstance != null)
            {
                Destroy(_hudInstance);
                _hudInstance = null;
            }

            if (_currentLevelScope != null)
            {
                _currentLevelScope.Dispose();
                Destroy(_currentLevelScope.gameObject);
                _currentLevelScope = null;
            }

            for (int i = 0; i < _loadedScenes.Count; i++)
            {
                var op = SceneManager.UnloadSceneAsync(_loadedScenes[i]);
                while (!op.isDone) yield return null;
            }
            _loadedScenes.Clear();
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}   