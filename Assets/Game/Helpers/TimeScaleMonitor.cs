using UnityEngine;
using System.Diagnostics;

public class TimeScaleMonitor : MonoBehaviour
{
    private float lastTimeScale;

    void Awake()
    {
        lastTimeScale = Time.timeScale;
        // Ensure this runs before other scripts if possible
        // Script Execution Order can be adjusted in Project Settings
    }

    void Update()
    {
        if (Time.timeScale != lastTimeScale)
        {
            UnityEngine.Debug.LogWarning($"Time.timeScale changed from {lastTimeScale} to {Time.timeScale}!");
            UnityEngine.Debug.LogWarning("Call Stack:\n" + new StackTrace().ToString());
            
            lastTimeScale = Time.timeScale;
        }
    }
}   