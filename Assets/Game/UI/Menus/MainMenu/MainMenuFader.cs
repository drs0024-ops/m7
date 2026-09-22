using System;
using UnityEngine;

namespace Game.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class MainMenuFader : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _overlayCanvasGroup;
        [SerializeField] private CanvasGroup _mainMenuCanvasGroup;
        [SerializeField] private float _waitTimeAfterLoad = 0.5f;
        [SerializeField] private float _fadeDuration = 1.0f;
        [SerializeField] private LeanTweenType _fadeEaseType = LeanTweenType.easeInOutQuad;

        private LTDescr _currentFadeTween;

        private void Awake()
        {
            if (_overlayCanvasGroup == null)
                _overlayCanvasGroup = GetComponentInChildren<CanvasGroup>();
            if (_mainMenuCanvasGroup == null)
                _mainMenuCanvasGroup = GetComponent<CanvasGroup>();
        }

        public void StartFadeInSequence(Action onComplete = null)
        {
            if (_overlayCanvasGroup == null || _mainMenuCanvasGroup == null)
            {
                Debug.LogError("[MainMenuFader] CanvasGroup references missing!");
                return;
            }

            LeanTween.cancel(_overlayCanvasGroup.gameObject);
            LeanTween.cancel(_mainMenuCanvasGroup.gameObject);

            _overlayCanvasGroup.gameObject.SetActive(true);
            _overlayCanvasGroup.alpha = 1f;
            _overlayCanvasGroup.interactable = false;
            _overlayCanvasGroup.blocksRaycasts = true;

            _mainMenuCanvasGroup.gameObject.SetActive(true);
            _mainMenuCanvasGroup.alpha = 1f;
            _mainMenuCanvasGroup.interactable = false;
            _mainMenuCanvasGroup.blocksRaycasts = false;

            FadeOverlay(0f, () =>
            {
                _overlayCanvasGroup.blocksRaycasts = false;
                _mainMenuCanvasGroup.interactable = true;
                _mainMenuCanvasGroup.blocksRaycasts = true;
                onComplete?.Invoke();
            });
        }   

        public void StartFadeOutSequence(Action onComplete)
        {
            if (_overlayCanvasGroup == null || _mainMenuCanvasGroup == null)
            {
                Debug.LogError("[MainMenuFader] CanvasGroup references missing!");
                onComplete?.Invoke();
                return;
            }

            LeanTween.cancel(_overlayCanvasGroup.gameObject);
            LeanTween.cancel(_mainMenuCanvasGroup.gameObject);

            _overlayCanvasGroup.gameObject.SetActive(true);
            _overlayCanvasGroup.alpha = 0f;
            _overlayCanvasGroup.interactable = false;
            _overlayCanvasGroup.blocksRaycasts = true;

            FadeOverlay(1f, () =>
            {
                _mainMenuCanvasGroup.interactable = false;
                _mainMenuCanvasGroup.blocksRaycasts = false;
                onComplete?.Invoke();
            });
        }

        private void FadeOverlay(float targetAlpha, Action onComplete)
        {
            _currentFadeTween = LeanTween.alphaCanvas(_overlayCanvasGroup, targetAlpha, _fadeDuration)
                .setDelay(_waitTimeAfterLoad)
                .setEase(_fadeEaseType)
                .setIgnoreTimeScale(true)
                .setOnComplete(() => onComplete?.Invoke());
        }
    }
}   

#region  prior version 6/23/2026
/*
using UnityEngine;
using System.Collections;

/// <summary>
/// Handles the smooth fade-in animation by fading out a black overlay to reveal the main menu.
/// Attach this to the same GameObject as MainMenuManager.
/// </summary>
public class MainMenuFader : MonoBehaviour
{
    #region References

    [Tooltip("The CanvasGroup controlling the black overlay (starts opaque, fades to transparent).")]
    [SerializeField] private CanvasGroup overlayCanvasGroup;

    [Tooltip("The CanvasGroup controlling the main menu UI (starts hidden/inactive).")]
    [SerializeField] private CanvasGroup mainMenuCanvasGroup;

    #endregion

    #region Settings

    [Header("Animation Settings")]
    
    [Tooltip("Time to wait after the scene loads before starting the fade-out of the overlay.")]
    [SerializeField] private float waitTimeAfterLoad = 0.5f;

    [Tooltip("Duration of the fade-out effect in seconds.")]
    [SerializeField] private float fadeDuration = 1.0f;

    [Tooltip("Easing style for the fade animation.")]
    [SerializeField] private LeanTweenType fadeEaseType = LeanTweenType.easeInOutQuad;

    #endregion

    #region State

    private LTDescr _currentFadeTween;

    #endregion

    #region Public Methods

    /// <summary>
    /// Initiates the fade-in sequence: waits, then fades the black overlay out to reveal the menu.
    /// Call this after the scene is fully loaded.
    /// </summary>
        public void StartFadeInSequence()
    {
        if (overlayCanvasGroup == null || mainMenuCanvasGroup == null)
        {
            Debug.LogError("[MainMenuFader] CanvasGroup references are missing!");
            return;
        }

        // --- 1. INITIAL STATE SETUP ---
        
        // Overlay: Active, Opaque (1), Blocks Raycasts
        overlayCanvasGroup.gameObject.SetActive(true);
        overlayCanvasGroup.alpha = 1f;
        overlayCanvasGroup.interactable = false; 
        overlayCanvasGroup.blocksRaycasts = true; 

        // Main Menu: Active, OPAQUE (1) but Hidden behind overlay, Non-Interactable
        // CRITICAL FIX: Alpha must be 1 so it is visible when overlay fades
        mainMenuCanvasGroup.gameObject.SetActive(true);
        mainMenuCanvasGroup.alpha = 1f; 
        mainMenuCanvasGroup.interactable = false; // Prevents input during fade
        mainMenuCanvasGroup.blocksRaycasts = false; // Doesn't block overlay

        if (_currentFadeTween != null)
        {
            LeanTween.cancel(overlayCanvasGroup.gameObject);
        }

        StartCoroutine(FadeOutOverlayAfterWait());
    }

    private IEnumerator FadeOutOverlayAfterWait()
    {
        yield return new WaitForSeconds(waitTimeAfterLoad);

        // Fade Overlay OUT (1 -> 0) to reveal the menu (which is already Alpha 1)
        _currentFadeTween = LeanTween.alphaCanvas(overlayCanvasGroup, 0f, fadeDuration)
            .setEase(fadeEaseType)
            .setOnComplete(() => {
                
                // --- 2. FINAL STATE SETUP ---
                overlayCanvasGroup.gameObject.SetActive(false); 
                overlayCanvasGroup.alpha = 1f; // Reset for next time

                // Enable Main Menu interaction
                mainMenuCanvasGroup.interactable = true;
                mainMenuCanvasGroup.blocksRaycasts = true;
                // Alpha remains 1

                MenuNavigationHighlighter highlighter = FindFirstObjectByType<MenuNavigationHighlighter>();
                highlighter?.ApplyInitialHighlight();
            });
    }   

    #endregion
}   

*/
#endregion