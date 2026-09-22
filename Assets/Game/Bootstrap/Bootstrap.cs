using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

/// <summary>
/// Entry-point MonoBehaviour. Additive-loads the PersistentManagers scene on Start.
/// Does NOT choose a target scene — that decision is owned by GameFlowSystem.
/// This scene persists for the entire session (hosts persistent audio, EventSystem, Options panel, CRT overlay).
/// </summary>
public class Bootstrap : MonoBehaviour
{
    [Header("Scene Names")]
    public string persistentManagersSceneName = "PersistentManagers";

    #region Lifecycle

    private async void Start()
    {
        var op = SceneManager.LoadSceneAsync(persistentManagersSceneName, LoadSceneMode.Additive);
        if (op == null)
        {
            Debug.LogError($"[Bootstrap] LoadSceneAsync returned NULL for '{persistentManagersSceneName}'!");
            return;
        }
        await op.ToUniTask();
    }

    #endregion
}   