// Plain number display for orb count. Brief color flash on collect, fades back to HUD color.
using System;
using System.Collections.Generic;
using Game.Core.Enums;
using Game.Core.Messages;
using MessagePipe;
using TMPro;
using UnityEngine;
using VContainer.Unity;

namespace Game.UI
{
    public class OrbCountController : MonoBehaviour, IStartable, IDisposable
    {
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private Color _humanoidFlash = new Color(1f, 0.82f, 0.5f, 1f);   // #FFD080
        [SerializeField] private Color _nonHumanoidFlash = new Color(0.5f, 1f, 0.82f, 1f); // #80FFD0
        [SerializeField] private float _flashDuration = 0.5f;

        private Color _baseColor;
        private ISubscriber<OrbCollectedMessage> _sub;
        private readonly List<IDisposable> _subscriptions = new();
        private bool _disposed;
        private int _tweenId = -1;

        public OrbCountController() { }

        void IStartable.Start()
        {
            _sub = GlobalMessagePipe.GetSubscriber<OrbCollectedMessage>();
            _subscriptions.Add(_sub.Subscribe(OnOrbCollected));

            _baseColor = _countText.color;
            _countText.text = "0";
        }

        private void OnOrbCollected(OrbCollectedMessage msg)
        {
            if (_disposed) return;

            _countText.text = msg.NewTotal.ToString();

            Color flash = msg.Type == OrbType.Humanoid ? _humanoidFlash : _nonHumanoidFlash;
            FlashColor(flash);
        }

        private void FlashColor(Color target)
        {
            if (_tweenId != -1) LeanTween.cancel(_tweenId);

            _countText.color = target;

            _tweenId = LeanTween.color(_countText.gameObject, _baseColor, _flashDuration)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() => _tweenId = -1).id;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_tweenId != -1) LeanTween.cancel(_tweenId);
            foreach (var d in _subscriptions) d?.Dispose();
            _subscriptions.Clear();
        }

        private void OnDestroy()
        {
            if (!_disposed) Dispose();
        }
    }
}   