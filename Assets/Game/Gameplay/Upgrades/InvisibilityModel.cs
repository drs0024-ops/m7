using System;
using System.Collections.Generic;
using Game.Core.Interfaces;
using Game.Core.Messages;
using MessagePipe;
using VContainer.Unity;

namespace Game.Gameplay.Upgrades
{
    /// <summary>Pure charge state machine for invisibility.</summary>
    public sealed class InvisibilityModel : IInvisibilityModel, IStartable, ITickable, IDisposable
    {
        private const float ComparisonEpsilon = 0.001f;

        private readonly IBufferedPublisher<InvisibilityChargeChanged> _chargePublisher;
        private readonly IBufferedPublisher<InvisibilityBroadcastStateChanged> _broadcastStatePublisher;
        private readonly IPublisher<InvisibilityBroadcastStarted> _broadcastStartedPublisher;
        private readonly IPublisher<InvisibilityBroadcastStopped> _broadcastStoppedPublisher;
        private readonly IPublisher<InvisibilityTierChanged> _tierPublisher;
        private readonly ISubscriber<InvisibilityChargingChanged> _chargingSubscriber;
        private readonly ISubscriber<InvisibilityInterrupted> _interruptedSubscriber;
        private readonly ISubscriber<UpgradeExpired> _expiredSubscriber;
        private readonly IInputState _input;
        private readonly IFrameTimeProvider _timeProvider;
        private readonly InvisibilityTuning _tuning;
        private readonly List<IDisposable> _subscriptions = new(3);

        private float _lastPublishedCharge = float.NaN;
        private float _chargeRate;
        private float _stationMaxChargePercent = 100f;
        private float _stationMaxChargeTimeSeconds;
        private float _stationElapsedSeconds;
        private float _broadcastRadius;
        private float _stationPositionX;
        private float _stationPositionY;
        private int _stationId = -1;
        private int _timedOutStationId = -1;
        private bool _disposed;

        /// <summary>Current charge in the inclusive range zero to one hundred.</summary>
        public float ChargePercent { get; private set; }

        /// <summary>Current charge-derived visibility tier.</summary>
        public InvisibilityTier CurrentTier { get; private set; } = InvisibilityTier.Visible;

        /// <summary>Whether an invisibility charger is actively charging.</summary>
        public bool IsCharging { get; private set; }

        /// <summary>Creates the model and subscribes to charge, damage, and expiry messages.</summary>
        public InvisibilityModel(
            IBufferedPublisher<InvisibilityChargeChanged> chargePublisher,
            IBufferedPublisher<InvisibilityBroadcastStateChanged> broadcastStatePublisher,
            IPublisher<InvisibilityBroadcastStarted> broadcastStartedPublisher,
            IPublisher<InvisibilityBroadcastStopped> broadcastStoppedPublisher,
            IPublisher<InvisibilityTierChanged> tierPublisher,
            ISubscriber<InvisibilityChargingChanged> chargingSubscriber,
            ISubscriber<InvisibilityInterrupted> interruptedSubscriber,
            ISubscriber<UpgradeExpired> expiredSubscriber,
            IInputState input,
            IFrameTimeProvider timeProvider,
            InvisibilityTuning tuning)
        {
            _chargePublisher = chargePublisher ?? throw new ArgumentNullException(nameof(chargePublisher));
            _broadcastStatePublisher = broadcastStatePublisher ?? throw new ArgumentNullException(nameof(broadcastStatePublisher));
            _broadcastStartedPublisher = broadcastStartedPublisher ?? throw new ArgumentNullException(nameof(broadcastStartedPublisher));
            _broadcastStoppedPublisher = broadcastStoppedPublisher ?? throw new ArgumentNullException(nameof(broadcastStoppedPublisher));
            _tierPublisher = tierPublisher ?? throw new ArgumentNullException(nameof(tierPublisher));
            _chargingSubscriber = chargingSubscriber ?? throw new ArgumentNullException(nameof(chargingSubscriber));
            _interruptedSubscriber = interruptedSubscriber ?? throw new ArgumentNullException(nameof(interruptedSubscriber));
            _expiredSubscriber = expiredSubscriber ?? throw new ArgumentNullException(nameof(expiredSubscriber));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            _chargeRate = tuning.ChargeRate;

            _subscriptions.Add(_chargingSubscriber.Subscribe(OnChargingChanged));
            _subscriptions.Add(_interruptedSubscriber.Subscribe(OnInterrupted));
            _subscriptions.Add(_expiredSubscriber.Subscribe(OnUpgradeExpired));
        }

        /// <summary>Publishes initial visible state to buffered subscribers.</summary>
        public void Start()
        {
            if (_disposed) return;
            PublishCharge();
            _tierPublisher.Publish(new InvisibilityTierChanged(CurrentTier));
            _broadcastStatePublisher.Publish(
                new InvisibilityBroadcastStateChanged(false, _stationPositionX, _stationPositionY, 0f));
        }

        /// <summary>Advances charge or passive decay without per-tick allocations.</summary>
        public void Tick()
        {
            if (_disposed) return;

            float deltaTime = _timeProvider.DeltaTime;
            if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;

            if (IsCharging)
            {
                float chargingDeltaTime = deltaTime;
                if (_stationMaxChargeTimeSeconds > 0f)
                {
                    float remainingTime = _stationMaxChargeTimeSeconds - _stationElapsedSeconds;
                    if (remainingTime <= 0f)
                    {
                        _timedOutStationId = _stationId;
                        StopCharging();
                        return;
                    }
                    chargingDeltaTime = Math.Min(deltaTime, remainingTime);
                }

                _stationElapsedSeconds += chargingDeltaTime;

                float maximum = Clamp(_stationMaxChargePercent, 0f, 100f);
                if (ChargePercent < maximum)
                {
                    float crouchMultiplier = _input.IsCrouching ? _tuning.CrouchChargeMultiplier : 1f;
                    SetCharge(Clamp(ChargePercent + _chargeRate * crouchMultiplier * chargingDeltaTime, 0f, maximum));
                }

                if (_stationMaxChargeTimeSeconds > 0f
                    && _stationElapsedSeconds >= _stationMaxChargeTimeSeconds)
                {
                    _timedOutStationId = _stationId;
                    StopCharging();
                }
            }
            else if (ChargePercent > 0f)
            {
                SetCharge(Clamp(ChargePercent - _tuning.DecayRate * deltaTime, 0f, 100f));
            }
        }

        private void OnChargingChanged(InvisibilityChargingChanged message)
        {
            if (_disposed) return;

            if (!message.IsCharging)
            {
                if (message.StationId == _timedOutStationId)
                    _timedOutStationId = -1;
                if (IsCharging && message.StationId == _stationId)
                    StopCharging();
                return;
            }

            if (message.StationId == _timedOutStationId) return;
            if (IsCharging && message.StationId != _stationId)
                StopCharging();

            bool wasCharging = IsCharging;
            _stationId = message.StationId;
            _chargeRate = Math.Max(0f, message.ChargeRate);
            _stationMaxChargePercent = Clamp(message.MaxChargePercent, 0f, 100f);
            _stationMaxChargeTimeSeconds = Math.Max(0f, message.MaxChargeTimeSeconds);
            _stationPositionX = message.Position.x;
            _stationPositionY = message.Position.y;
            _broadcastRadius = Math.Max(0f, message.BroadcastRadius);

            if (wasCharging) return;

            IsCharging = true;
            _stationElapsedSeconds = 0f;
            _broadcastStartedPublisher.Publish(
                new InvisibilityBroadcastStarted(_stationPositionX, _stationPositionY, _broadcastRadius));
            _broadcastStatePublisher.Publish(
                new InvisibilityBroadcastStateChanged(true, _stationPositionX, _stationPositionY, _broadcastRadius));
        }

        private void StopCharging()
        {
            if (!IsCharging) return;

            IsCharging = false;
            _broadcastStoppedPublisher.Publish(
                new InvisibilityBroadcastStopped(_stationPositionX, _stationPositionY));
            _broadcastStatePublisher.Publish(
                new InvisibilityBroadcastStateChanged(false, _stationPositionX, _stationPositionY, 0f));
            _stationId = -1;
            _stationElapsedSeconds = 0f;
        }

        private void OnInterrupted(InvisibilityInterrupted message)
        {
            if (message.DrainPercent > 0f)
                SetCharge(Clamp(ChargePercent - message.DrainPercent, 0f, 100f));
        }

        private void OnUpgradeExpired(UpgradeExpired message)
        {
            if (message.UpgradeId != UpgradeIDs.Invisibility) return;
            _timedOutStationId = -1;
            StopCharging();
            SetCharge(0f);
        }

        private void SetCharge(float value)
        {
            ChargePercent = value;
            PublishCharge();

            InvisibilityTier nextTier = GetTier(value);
            if (nextTier == CurrentTier) return;
            CurrentTier = nextTier;
            _tierPublisher.Publish(new InvisibilityTierChanged(CurrentTier));
        }

        private void PublishCharge()
        {
            if (Approximately(_lastPublishedCharge, ChargePercent)) return;
            _lastPublishedCharge = ChargePercent;
            _chargePublisher.Publish(new InvisibilityChargeChanged(ChargePercent));
        }

        private InvisibilityTier GetTier(float charge)
        {
            if (charge < _tuning.TranslucentThreshold) return InvisibilityTier.Visible;
            if (charge < _tuning.CloakedSlowedThreshold) return InvisibilityTier.Translucent;
            if (charge < _tuning.CloakedThreshold) return InvisibilityTier.CloakedSlowed;
            return InvisibilityTier.Cloaked;
        }

        private static bool Approximately(float left, float right) =>
            Math.Abs(left - right) < ComparisonEpsilon;

        private static float Clamp(float value, float min, float max) =>
            value < min ? min : value > max ? max : value;

        /// <summary>Disposes all MessagePipe subscriptions.</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i]?.Dispose();
            _subscriptions.Clear();
        }
    }
}