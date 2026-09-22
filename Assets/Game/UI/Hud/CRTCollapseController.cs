using System;
using System.Collections;
using UnityEngine;

namespace Game.UI
{
    public class CRTCollapseController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Material _hudCrtMaterial;

        [Header("Animation")]
        [Range(0.1f, 5f)] [SerializeField] private float _collapseDuration = 1.2f;
        [SerializeField] private AnimationCurve _curve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.15f, 0f),
            new Keyframe(0.4f, 1f),
            new Keyframe(1f, 1f)
        );

        private Coroutine _activeRoutine;
        public Action OnComplete;
        public bool IsCollapsing => _activeRoutine != null;
        public float Progress { get; private set; }

        public void PlayCollapse()
        {
            if (_hudCrtMaterial == null) return;
            StopCollapse();
            _activeRoutine = StartCoroutine(CollapseRoutine());
        }

        public void StopCollapse()
        {
            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
                _activeRoutine = null;
            }
        }

        public void ResetCollapse()
        {
            StopCollapse();
            Progress = 0f;
            if (_hudCrtMaterial != null)
                _hudCrtMaterial.SetFloat("_Collapse", 0f);
        }

        private IEnumerator CollapseRoutine()
        {
            float elapsed = 0f;
            while (elapsed < _collapseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _collapseDuration);
                Progress = t;
                _hudCrtMaterial.SetFloat("_Collapse", _curve.Evaluate(t));
                yield return null;
            }
            Progress = 1f;
            _hudCrtMaterial.SetFloat("_Collapse", 1f);
            _activeRoutine = null;
            OnComplete?.Invoke();
        }
    }
}   