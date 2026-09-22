using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public enum PauseTab
    {
        Resume = 0,
        Objectives = 1,
        Options = 2,
        Exit = 3
    }

    public class PauseMenuTabs : MonoBehaviour
    {
        [SerializeField] private Button[] _tabButtons;
        [SerializeField] private GameObject[] _panels;

        [Header("Active Tab Highlight")]
        [SerializeField] private Color _activeColor = Color.white;
        [SerializeField] private Color _inactiveColor = new Color(1f, 1f, 1f, 0.5f);

        private int _activeIndex = -1;

        public void OnMenuOpened() => ShowTab(PauseTab.Resume);
        public void OnMenuClosed() => _activeIndex = -1;

        public void ShowResumeTab()     => ShowTab(PauseTab.Resume);
        public void ShowObjectivesTab() => ShowTab(PauseTab.Objectives);
        public void ShowOptionsTab()    => ShowTab(PauseTab.Options);
        public void ShowExitTab()       => ShowTab(PauseTab.Exit);

        public int ActiveIndex => _activeIndex;

        private void ShowTab(PauseTab tab)
        {
            int index = (int)tab;
            if (index < 0 || index >= _panels.Length) return;

            for (int i = 0; i < _panels.Length; i++)
            {
                if (_panels[i] != null)
                    _panels[i].SetActive(i == index);
            }

            UpdateButtonStates(index);
            _activeIndex = index;
        }

        private void UpdateButtonStates(int activeIndex)
        {
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] == null) continue;
                var img = _tabButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = (i == activeIndex) ? _activeColor : _inactiveColor;
            }
        }
    }
}   