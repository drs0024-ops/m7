using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.UI 
{
    public class FadePanelManager : MonoBehaviour
    {
        [SerializeField] private GameObject _startPanelReference;
        [SerializeField] private string _startPanelIDOverride;

        [Header("Defaults")]
        [SerializeField] private float _defaultFadeDuration = 0.5f;
        [SerializeField] private LeanTweenType _defaultEaseType = LeanTweenType.easeInOutQuad;

        private readonly Dictionary<string, CanvasGroup> _panelMap = new();
        private string _startPanelID;
        private string _currentPanelID;

        private void Awake()
        {
            DiscoverPanels();
            ResolveStartID();
        }

        private void Start()
        {
            if (_panelMap.Count == 0)
            {
                Debug.LogError("[FadePanelManager] No panels found. Check children for PanelIdentity components.");
                return;
            }

            if (!string.IsNullOrEmpty(_startPanelID) &&
                _panelMap.TryGetValue(_startPanelID, out var startGroup))
            {
                startGroup.alpha = 1f;
                startGroup.interactable = true;
                startGroup.blocksRaycasts = true;
                _currentPanelID = _startPanelID;
            }
        }

        #region Discovery

        private void DiscoverPanels()
        {
            var identities = GetComponentsInChildren<PanelIdentity>(true);

            foreach (var identity in identities)
            {
                if (identity != null ? identity.Config : null == null || string.IsNullOrEmpty(identity.Config.panelID)) continue;

                string id = identity.Config.panelID;
                if (_panelMap.ContainsKey(id))
                {
                    Debug.LogError($"[FadePanelManager] Duplicate Panel ID '{id}' on '{identity.gameObject.name}'. Ignoring.");
                    continue;
                }

                var group = identity.GetComponent<CanvasGroup>();
                if (group == null) group = identity.gameObject.AddComponent<CanvasGroup>();

                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;

                _panelMap[id] = group;
            }
        }

        private void ResolveStartID()
        {
            if (!string.IsNullOrEmpty(_startPanelIDOverride))
            {
                _startPanelID = _startPanelIDOverride;
                return;
            }

            if (_startPanelReference != null)
            {
                var identity = _startPanelReference.GetComponent<PanelIdentity>();
                if (identity != null ? identity.Config : null != null && !string.IsNullOrEmpty(identity.Config.panelID))
                {
                    _startPanelID = identity.Config.panelID;
                    return;
                }
            }

            _startPanelID = GetFirstPanelID();
        }

        private string GetFirstPanelID()
        {
            foreach (var key in _panelMap.Keys) return key;
            return null;
        }

        #endregion

        #region Panel Switching

        public void SwitchPanel(string targetPanelID)
        {
            if (!_panelMap.TryGetValue(targetPanelID, out var incoming))
            {
                Debug.LogError($"[FadePanelManager] Target panel '{targetPanelID}' not found.");
                return;
            }

            if (_currentPanelID == targetPanelID)
            {
                incoming.interactable = true;
                incoming.blocksRaycasts = true;
                return;
            }

            CanvasGroup outgoing = null;
            if (!string.IsNullOrEmpty(_currentPanelID))
                _panelMap.TryGetValue(_currentPanelID, out outgoing);

            if (outgoing != null && outgoing != incoming)
            {
                LeanTween.cancel(outgoing.gameObject);
                LeanTween.value(outgoing.gameObject, (float val) => outgoing.alpha = val,
                    outgoing.alpha, 0f, _defaultFadeDuration)
                    .setEase(_defaultEaseType)
                    .setIgnoreTimeScale(true)
                    .setOnComplete(() =>
                    {
                        outgoing.interactable = false;
                        outgoing.blocksRaycasts = false;
                    });
            }

            LeanTween.cancel(incoming.gameObject);
            incoming.gameObject.SetActive(true);

            LeanTween.value(incoming.gameObject, (float val) => incoming.alpha = val,
                incoming.alpha, 1f, _defaultFadeDuration)
                .setEase(_defaultEaseType)
                .setIgnoreTimeScale(true)
                .setOnComplete(() =>
                {
                    incoming.interactable = true;
                    incoming.blocksRaycasts = true;
                    _currentPanelID = targetPanelID;
                });
        }

        #endregion

        #region Sequential Fade Out

        public async UniTask FadeOutAllPanelsSequential()
        {
            if (_panelMap.Count == 0) return;

            CanvasGroup background = null;
            var otherPanels = new List<CanvasGroup>();

            foreach (var pair in _panelMap)
            {
                if (pair.Key == "BackGround")
                    background = pair.Value;
                else
                    otherPanels.Add(pair.Value);
            }

            // Fade all non-background panels simultaneously
            var tasks = new List<UniTask>();

            foreach (var panel in otherPanels)
            {
                if (panel == null) continue;

                if (panel.alpha <= 0f)
                {
                    panel.interactable = false;
                    panel.blocksRaycasts = false;
                    continue;
                }

                tasks.Add(FadePanelAsync(panel));
            }

            if (tasks.Count > 0)
                await UniTask.WhenAll(tasks);

            // Fade background last
            if (background != null && background.alpha > 0f)
                await FadePanelAsync(background);
        }

        private async UniTask FadePanelAsync(CanvasGroup panel)
        {
            LeanTween.cancel(panel.gameObject);

            var tcs = new UniTaskCompletionSource();
            LeanTween.alphaCanvas(panel, 0f, _defaultFadeDuration)
                .setEase(_defaultEaseType)
                .setIgnoreTimeScale(true)
                .setOnComplete(() =>
                {
                    panel.interactable = false;
                    panel.blocksRaycasts = false;
                    tcs.TrySetResult();
                });

            await tcs.Task;
        }

        #endregion
    }
}   