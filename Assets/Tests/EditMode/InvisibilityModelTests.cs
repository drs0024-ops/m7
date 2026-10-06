using System.Collections.Generic;
using Game.Core.Messages;
using Game.Gameplay.Upgrades;
using MessagePipe;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    [TestFixture]
    public class InvisibilityModelTests
    {
        private FakeBufferedPublisher<InvisibilityChargeChanged> _chargePublisher;
        private FakeBufferedPublisher<InvisibilityBroadcastStateChanged> _broadcastStatePublisher;
        private FakePublisher<InvisibilityBroadcastStarted> _broadcastStartedPublisher;
        private FakePublisher<InvisibilityBroadcastStopped> _broadcastStoppedPublisher;
        private FakePublisher<InvisibilityTierChanged> _tierPublisher;
        private FakeSubscriber<InvisibilityChargingChanged> _chargingSubscriber;
        private FakeSubscriber<InvisibilityInterrupted> _interruptedSubscriber;
        private FakeSubscriber<UpgradeExpired> _expiredSubscriber;
        private FakeInputState _input;
        private FakeFrameTimeProvider _time;
        private InvisibilityModel _sut;

        [SetUp]
        public void SetUp()
        {
            _chargePublisher = new FakeBufferedPublisher<InvisibilityChargeChanged>();
            _broadcastStatePublisher = new FakeBufferedPublisher<InvisibilityBroadcastStateChanged>();
            _broadcastStartedPublisher = new FakePublisher<InvisibilityBroadcastStarted>();
            _broadcastStoppedPublisher = new FakePublisher<InvisibilityBroadcastStopped>();
            _tierPublisher = new FakePublisher<InvisibilityTierChanged>();
            _chargingSubscriber = new FakeSubscriber<InvisibilityChargingChanged>();
            _interruptedSubscriber = new FakeSubscriber<InvisibilityInterrupted>();
            _expiredSubscriber = new FakeSubscriber<UpgradeExpired>();
            _input = new FakeInputState();
            _time = new FakeFrameTimeProvider();

            _sut = new InvisibilityModel(
                _chargePublisher,
                _broadcastStatePublisher,
                _broadcastStartedPublisher,
                _broadcastStoppedPublisher,
                _tierPublisher,
                _chargingSubscriber,
                _interruptedSubscriber,
                _expiredSubscriber,
                _input,
                _time,
                new InvisibilityTuning(10f, 5f, 0.6f, 10f, 30f, 60f, 80f));
            _sut.Start();
        }

        [TearDown]
        public void TearDown() => _sut.Dispose();

        [Test]
        public void Tick_ChargesAtStationRate_AndAppliesCrouchMultiplier()
        {
            StartStation(100f, 0f);
            _time.DeltaTime = 1f;
            _sut.Tick();
            Assert.That(_sut.ChargePercent, Is.EqualTo(10f));

            _input.IsCrouching = true;
            _time.DeltaTime = 1f;
            _sut.Tick();
            Assert.That(_sut.ChargePercent, Is.EqualTo(16f).Within(0.001f));
        }

        [Test]
        public void Tick_StopsAtStationChargeCeiling()
        {
            StartStation(25f, 0f);
            _time.DeltaTime = 5f;
            _sut.Tick();

            Assert.That(_sut.ChargePercent, Is.EqualTo(25f));
            Assert.That(_chargePublisher.Messages[^1].ChargePercent, Is.EqualTo(25f));
        }

        [Test]
        public void Tick_ChargesForConfiguredDurationThenStops()
        {
            StartStation(100f, 2f);
            _time.DeltaTime = 3f;
            _sut.Tick();

            Assert.That(_sut.ChargePercent, Is.EqualTo(20f));
            Assert.That(_sut.IsCharging, Is.False);
            Assert.That(_broadcastStoppedPublisher.Messages, Has.Count.EqualTo(1));
        }

        [Test]
        public void TimedOutStation_DoesNotRestartUntilPlayerExitsAndReenters()
        {
            StartStation(100f, 1f);
            _time.DeltaTime = 1f;
            _sut.Tick();
            Assert.That(_sut.IsCharging, Is.False);

            _chargingSubscriber.Publish(Charging(true, 1, 100f, 1f));
            Assert.That(_sut.IsCharging, Is.False);

            _chargingSubscriber.Publish(Charging(false, 1));
            _chargingSubscriber.Publish(Charging(true, 1, 100f, 1f));
            Assert.That(_sut.IsCharging, Is.True);
        }

        [Test]
        public void Tick_DecaysChargeAfterLeavingCharger()
        {
            StartStation(100f, 0f);
            _time.DeltaTime = 2f;
            _sut.Tick();
            _chargingSubscriber.Publish(Charging(false, stationId: 1));

            _time.DeltaTime = 1f;
            _sut.Tick();

            Assert.That(_sut.ChargePercent, Is.EqualTo(15f));
        }

        [Test]
        public void Interrupted_DrainsChargeAndUpdatesTier()
        {
            StartStation(100f, 0f);
            _time.DeltaTime = 4f;
            _sut.Tick();
            _interruptedSubscriber.Publish(new InvisibilityInterrupted(15f));

            Assert.That(_sut.ChargePercent, Is.EqualTo(25f));
            Assert.That(_sut.CurrentTier, Is.EqualTo(InvisibilityTier.Visible));
        }

        [Test]
        public void Tick_PublishesTierOnlyWhenThresholdIsCrossed()
        {
            StartStation(100f, 0f);
            _time.DeltaTime = 3f;
            _sut.Tick();
            Assert.That(_sut.CurrentTier, Is.EqualTo(InvisibilityTier.Translucent));
            Assert.That(_tierPublisher.Messages[^1].NewTier, Is.EqualTo(InvisibilityTier.Translucent));

            _time.DeltaTime = 0.1f;
            _sut.Tick();
            Assert.That(_tierPublisher.Messages, Has.Count.EqualTo(2));
        }

        [Test]
        public void UpgradeExpired_StopsChargingAndResetsCharge()
        {
            StartStation(100f, 0f);
            _time.DeltaTime = 5f;
            _sut.Tick();
            _expiredSubscriber.Publish(new UpgradeExpired(UpgradeIDs.Invisibility));

            Assert.That(_sut.ChargePercent, Is.Zero);
            Assert.That(_sut.CurrentTier, Is.EqualTo(InvisibilityTier.Visible));
            Assert.That(_sut.IsCharging, Is.False);
        }

        private void StartStation(float maxChargePercent, float maxTimeSeconds)
        {
            _chargingSubscriber.Publish(Charging(true, 1, maxChargePercent, maxTimeSeconds));
        }

        private static InvisibilityChargingChanged Charging(bool active, int stationId = 1,
            float maxChargePercent = 100f, float maxTimeSeconds = 0f) =>
            new(active, stationId, 10f, maxChargePercent, maxTimeSeconds, Vector2.zero, 15f);

        private sealed class FakeFrameTimeProvider : Game.Core.Interfaces.IFrameTimeProvider
        {
            public float DeltaTime { get; set; }
        }

        private sealed class FakeBufferedPublisher<TMessage> : IBufferedPublisher<TMessage>
            where TMessage : struct
        {
            public readonly List<TMessage> Messages = new();
            public void Publish(TMessage message) => Messages.Add(message);
        }
    }
}
