using UnityEngine;

namespace Game.UI
{
    public class PanelIdentity : MonoBehaviour
    {
        [SerializeField] private PanelConfigSO _config;

        public PanelConfigSO Config => _config;

        private void Awake()
        {
            if (_config == null)
                Debug.LogWarning($"[PanelIdentity] '{gameObject.name}' has no PanelConfigSO assigned!");

            if (GetComponent<CanvasGroup>() == null)
                gameObject.AddComponent<CanvasGroup>();
        }
    }
}   