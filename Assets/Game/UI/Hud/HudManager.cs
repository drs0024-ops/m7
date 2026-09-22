using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.UI
{

	public class HudManager : MonoBehaviour, IDisposable
    {
        [Tooltip("List of panel configurations.")]
        [SerializeField] private List<HUDPanelConfig> _hudPanels = new();
        [Tooltip("Duration for fade transitions.")]
        [SerializeField] private float _fadeDuration = 0.3f;
        [Tooltip("Ease type for fade transitions.")]
        [SerializeField] private LeanTweenType _fadeEaseType = LeanTweenType.easeInOutQuad;

        private int[] _tweenIds;
        private int[] _hideTokens;
        private bool _disposed;

        private void Awake()
        {
            int count = _hudPanels.Count;
            _tweenIds = new int[count];
            _hideTokens = new int[count];

            for (int i = 0; i < count; i++)
            {
                _tweenIds[i] = -1;

                var cfg = _hudPanels[i];
                if (cfg.panel != null)
                {
                    if (cfg.canvasGroup == null)
                    {
                        cfg.canvasGroup = cfg.panel.GetComponent<CanvasGroup>();
                        if (cfg.canvasGroup == null)
                            cfg.canvasGroup = cfg.panel.AddComponent<CanvasGroup>();
                    }

                    cfg.panel.SetActive(false);
                    cfg.canvasGroup.alpha = 0f;
                    cfg.canvasGroup.blocksRaycasts = false;
                    cfg.canvasGroup.interactable = cfg.isConstant;
                }
            }
        }

        #region Public API

        public void ShowPanel(int index)
        {
            if (_disposed || index < 0 || index >= _hudPanels.Count) return;

            var config = _hudPanels[index];
            if (config.panel == null) return;

            _hideTokens[index]++;
            int myToken = _hideTokens[index];

            FadeInPanel(index);

            if (!config.isConstant)
                HidePanelAfterDelay(index, myToken, config.displayDuration).Forget();
        }

        public void HidePanel(int index)
        {
            if (_disposed || index < 0 || index >= _hudPanels.Count) return;
            FadeOutPanel(index);
        }

        public void ShowPanels(int[] indices)
        {
            for (int i = 0; i < indices.Length; i++)
                ShowPanel(indices[i]);
        }

        public void HidePanels(int[] indices)
        {
            for (int i = 0; i < indices.Length; i++)
                HidePanel(indices[i]);
        }

        #endregion

        #region Fade Logic

        private void FadeInPanel(int index)
        {
            var config = _hudPanels[index];

            CancelTween(index);
            config.panel.SetActive(true);
            config.canvasGroup.alpha = 0f;
            config.canvasGroup.interactable = true;
            config.canvasGroup.blocksRaycasts = true;

            LTDescr tween = LeanTween.alphaCanvas(config.canvasGroup, 1f, _fadeDuration)
                .setEase(_fadeEaseType)
                .setIgnoreTimeScale(true)
                .setOnComplete(() =>
                {
                    _tweenIds[index] = -1;
                    if (config.isConstant)
                        config.canvasGroup.interactable = true;
                });

            _tweenIds[index] = tween.id;
        }

        private void FadeOutPanel(int index)
        {
            var config = _hudPanels[index];

            CancelTween(index);

            LTDescr tween = LeanTween.alphaCanvas(config.canvasGroup, 0f, _fadeDuration)
                .setEase(_fadeEaseType)
                .setIgnoreTimeScale(true)
                .setOnComplete(() =>
                {
                    if (_disposed) return;
                    config.panel.SetActive(false);
                    config.canvasGroup.blocksRaycasts = false;
                    config.canvasGroup.interactable = false;
                    _tweenIds[index] = -1;
                });

            _tweenIds[index] = tween.id;
        }

        private void CancelTween(int index)
        {
            if (_tweenIds[index] != -1)
                LeanTween.cancel(_tweenIds[index]);
            _tweenIds[index] = -1;
        }

        #endregion

        #region Auto-Hide

        private async UniTaskVoid HidePanelAfterDelay(int index, int myToken, float delay)
        {
            await UniTask.Delay((int)(delay * 1000f), ignoreTimeScale: true);

            if (myToken != _hideTokens[index] || _disposed) return;
            FadeOutPanel(index);
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            for (int i = 0; i < _tweenIds.Length; i++)
                CancelTween(i);
        }

        private void OnDestroy()
        {
            if (!_disposed) Dispose();
        }

        #endregion
    }

}