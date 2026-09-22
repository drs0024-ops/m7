using UnityEngine;

public class TimeDiagnostic : MonoBehaviour
{
    void Update()
    {
        // This will print even if Time.timeScale is 0
        Debug.Log($"[Diagnostic] Time.timeScale: {Time.timeScale} | Unscaled Delta: {Time.unscaledDeltaTime}");
        
        if (Time.timeScale == 0f)
        {
            Debug.LogWarning("[Diagnostic] GAME IS PAUSED! Update loops relying on Time.deltaTime are frozen.");
        }
    }
}   