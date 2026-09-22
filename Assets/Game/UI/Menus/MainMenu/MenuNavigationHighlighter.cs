using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;

namespace Game.UI
{
    public class MenuNavigationHighlighter : MonoBehaviour
    {
        [SerializeField] private GameObject _firstSelectedGameObject;
        [SerializeField] private float _highlightFadeDuration = 0.5f;
        [SerializeField] private LeanTweenType _highlightEaseType = LeanTweenType.easeOutQuad;

        private EventSystem _eventSystem;

        [Inject]
        private void Construct(EventSystem eventSystem)
        {
            _eventSystem = eventSystem;
        }

        public void ApplyInitialHighlight()
        {
            LeanTween.delayedCall(gameObject, 0f, PerformHighlightLogic)
                .setIgnoreTimeScale(true);
        }

        private void PerformHighlightLogic()
        {
            var target = _firstSelectedGameObject;

            if (target == null && _eventSystem?.currentSelectedGameObject != null)
                target = _eventSystem.currentSelectedGameObject;

            if (target == null)
            {
                Debug.LogError("[MenuNavigationHighlighter] No target object available.");
                return;
            }

            _eventSystem.SetSelectedGameObject(null);
            _eventSystem.SetSelectedGameObject(target);

            var selectable = target.GetComponent<Selectable>();
            if (selectable == null)
            {
                Debug.LogError($"[MenuNavigationHighlighter] '{target.name}' has no Selectable component.");
                return;
            }

            var button = target.GetComponent<Button>();
            if (button != null)
                FadeButtonToHighlight(button);
            else
                selectable.OnSelect(new BaseEventData(_eventSystem));
        }

        private void FadeButtonToHighlight(Button button)
        {
            var image = button.GetComponent<Image>();
            if (image == null) return;

            LeanTween.value(button.gameObject, (Color val) =>
            {
                image.color = val;
            }, image.color, button.colors.highlightedColor, _highlightFadeDuration)
                .setEase(_highlightEaseType)
                .setIgnoreTimeScale(true);
        }
    }
}   