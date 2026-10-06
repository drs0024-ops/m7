using System;
using Game.Core.Interfaces;
using Game.Core.Messages;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Upgrades
{
    /// <summary>Thin scene component exposing the scoped invisibility model.</summary>
    public sealed class InvisibilityController : MonoBehaviour, IDisposable
    {
        private IInvisibilityModel _model;
        private bool _disposed;

        /// <summary>Whether an invisibility station is currently charging.</summary>
        public bool IsCharging => _model != null && _model.IsCharging;

        /// <summary>Current charge in the inclusive range zero to one hundred.</summary>
        public float ChargePercent => _model?.ChargePercent ?? 0f;

        /// <summary>Current charge-derived visibility tier.</summary>
        public InvisibilityTier CurrentTier => _model?.CurrentTier ?? InvisibilityTier.Visible;

        /// <summary>Injects the scene-scoped charge model.</summary>
        [Inject]
        private void Inject(IInvisibilityModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        /// <summary>Disposes the model and releases this façade.</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _model?.Dispose();
            _model = null;
        }

        private void OnDestroy() => Dispose();
    }
}   