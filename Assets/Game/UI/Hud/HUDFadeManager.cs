using UnityEngine;
using System;


public class HUDFadeManager : MonoBehaviour
{
    [Header("Panel Configuration")]
    [SerializeField] private HUDPanel[] panels;

    private void Awake()
    {
        if (panels == null) return;
        
        foreach (var panel in panels)
        {
            if (panel.canvasGroup == null) continue;

            // Set initial visual state
            panel.canvasGroup.alpha = panel.startHidden ? panel.minAlpha : panel.maxAlpha;
            
            // Set initial interaction state (Inverse of startHidden)
            UpdateInteraction(panel, !panel.startHidden);
        }
    }

    /// <summary>
    /// Fades a specific panel using its own defined Duration, Ease, and Alpha limits.
    /// </summary>
    /// <param name="panelName">Name of the panel defined in the inspector array.</param>
    /// <param name="fadeIn">True to fade to MaxAlpha, False to fade to MinAlpha.</param>
    /// <param name="customDuration">Optional override. If < 0, uses the panel's own fadeDuration.</param>
    public void FadePanel(string panelName, bool fadeIn, float customDuration = -1f)
    {
        var panel = Array.Find(panels, p => p.name == panelName);
        
        if (panel == null || panel.canvasGroup == null)
        {
            Debug.LogWarning($"[HUDFadeManager] Panel '{panelName}' not found or missing CanvasGroup.");
            return;
        }

        float duration = customDuration >= 0f ? customDuration : panel.fadeDuration;
        float targetAlpha = fadeIn ? panel.maxAlpha : panel.minAlpha;

        // Cancel existing tweens to prevent conflicts
        LeanTween.cancel(panel.canvasGroup.gameObject); 

        // Set initial state immediately to prevent "one-frame" glitches
        // If fading IN: start invisible and non-interactive.
        // If fading OUT: start visible and interactive (will update in tween).
        panel.canvasGroup.alpha = fadeIn ? panel.minAlpha : panel.maxAlpha;
        panel.canvasGroup.blocksRaycasts = !fadeIn; // False if fading in, True if fading out
        panel.canvasGroup.interactable = !fadeIn;

        LTDescr tween = LeanTween.alphaCanvas(panel.canvasGroup, targetAlpha, duration)
            .setEase(panel.easeType);

        // Synchronize interaction with visual alpha for BOTH FadeIn and FadeOut
        tween.setOnUpdate((float val) => {
            if (panel.canvasGroup == null) return;

            if (fadeIn)
            {
                // Enable interaction ONLY when mostly visible (e.g., > 80%)
                bool isActive = (val > 0.8f);
                panel.canvasGroup.blocksRaycasts = isActive;
                panel.canvasGroup.interactable = isActive;
            }
            else
            {
                // Disable interaction ONLY when mostly invisible (e.g., < 20%)
                // This prevents clicking through a visible panel during fade out
                bool isStillVisible = (val > 0.2f);
                panel.canvasGroup.blocksRaycasts = isStillVisible;
                panel.canvasGroup.interactable = isStillVisible;
            }
        });

        // Ensure final state is correct when tween completes
        tween.setOnComplete(() => {
            if (panel.canvasGroup == null) return;
            panel.canvasGroup.blocksRaycasts = fadeIn;
            panel.canvasGroup.interactable = fadeIn;
            panel.canvasGroup.alpha = targetAlpha; // Snap to exact target
        });
    }   

    /// <summary>
    /// Helper to toggle interaction and raycasting based on state.
    /// </summary>
    private void UpdateInteraction(HUDPanel panel, bool isActive)
    {
        if (panel.canvasGroup == null) return;
        
        // Critical: Both must match to prevent "ghost clicks" or unresponsive visible UI
        panel.canvasGroup.interactable = isActive;
        panel.canvasGroup.blocksRaycasts = isActive;
    }

    // Helpers for index-based access
    public void FadePanelIn(int index, float customDuration = -1f)
    {
        if (index >= 0 && index < panels.Length)
            FadePanel(panels[index].name, true, customDuration);
    }

    public void FadePanelOut(int index, float customDuration = -1f)
    {
        if (index >= 0 && index < panels.Length)
            FadePanel(panels[index].name, false, customDuration);
    }
}

[System.Serializable]
public class HUDPanel
{
    public string name;
    public CanvasGroup canvasGroup;
    
    [Tooltip("If true, this group starts at Min Alpha")]
    public bool startHidden = false;

    [Header("Animation Settings")]
    [Range(0.1f, 2f)]
    [Tooltip("Duration specific to this panel")]
    public float fadeDuration = 0.5f;
    
    [Tooltip("Ease type specific to this panel")]
    public LeanTweenType easeType = LeanTweenType.easeInOutQuad;

    [Header("Alpha Limits")]
    [Range(0f, 1f)]
    [Tooltip("Target alpha when faded out")]
    public float minAlpha = 0f;
    
    [Range(0f, 1f)]
    [Tooltip("Target alpha when faded in")]
    public float maxAlpha = 1f;
}