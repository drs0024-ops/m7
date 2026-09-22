using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class ObjectiveItem : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _description;
        [SerializeField] private TextMeshProUGUI _progress;

        public void Bind(string title, string description, Sprite icon, bool isComplete, int index, int total)
        {
            if (_icon != null) _icon.sprite = icon;
            if (_title != null) _title.text = title;
            if (_description != null) _description.text = description;
            if (_progress != null)
                _progress.text = isComplete ? "✓" : $"{index + 1} / {total}";
        }
    }
}   