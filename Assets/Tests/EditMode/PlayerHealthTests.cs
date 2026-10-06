using UnityEngine;
using Game.Gameplay.Player;
using Game.Core.Messages;
using NUnit.Framework;
using Game.Core;
using Game.Gameplay.Upgrades;
using UnityEngine.TestTools;

namespace Game.Tests
{
    [TestFixture]
    public class PlayerHealthTests
    {
        private FakePublisher<PlayerDamaged> _damagedPub;
        private FakePublisher<PlayerDied> _diedPub;
        private FakePublisher<EntityHealthChanged> _healthPub;
        private FakePublisher<InvisibilityInterrupted> _invisibilityInterruptedPub;
        private FakeInputState _input;
        private HealthModel _sut;
        private GameObject go;

        [SetUp]
        public void SetUp()
        {
            _damagedPub = new FakePublisher<PlayerDamaged>();
            _diedPub = new FakePublisher<PlayerDied>();
            _healthPub = new FakePublisher<EntityHealthChanged>();
            _invisibilityInterruptedPub = new FakePublisher<InvisibilityInterrupted>();
            _input = new FakeInputState();
            _sut = new HealthModel(
                _damagedPub, _diedPub, _healthPub, _input,
                new LevelStateSnapshot(0, 0f),
                _invisibilityInterruptedPub,
                new InvisibilityTuning(10f, 5f, 0.6f, 10f, 30f, 60f, 80f));

            // Provide a transform (Die() reads _transform.position)
            go = new GameObject("FakePlayer");
            _sut.SetTransform(go.transform);
        }

        [Test]
        public void Damage_NonLethal_PublishesPlayerDamaged()
        {
            _sut.Damage(20f, Vector3.forward);

            Assert.That(_damagedPub.Messages, Has.Count.EqualTo(1));
            Assert.That(_invisibilityInterruptedPub.Messages, Has.Count.EqualTo(1));
            Assert.That(_invisibilityInterruptedPub.Messages[0].DrainPercent, Is.EqualTo(10f));
            Assert.That(_sut.CurrentHealth, Is.EqualTo(80f));
            Assert.That(_sut.IsDead, Is.False);
        }

        [Test]
        public void Damage_Lethal_PublishesPlayerDied()
        {
            _sut.Damage(150f, Vector3.forward);

            Assert.That(_diedPub.Messages, Has.Count.EqualTo(1));
            Assert.That(_sut.IsDead, Is.True);
        }

        [Test]
        public void Damage_WhileInvincible_DoesNothing()
        {
            _sut.IsInvincible = true;
            _sut.Damage(50f, Vector3.forward);

            Assert.That(_damagedPub.Messages, Has.Count.EqualTo(0));
            Assert.That(_invisibilityInterruptedPub.Messages, Is.Empty);
            Assert.That(_sut.CurrentHealth, Is.EqualTo(100f));
        }

        [Test] 
        public void Heal_CapsAtMaxHealth()
        {
            _sut.Damage(50f, Vector3.forward);
            _sut.Heal(100f);

            Assert.That(_sut.CurrentHealth, Is.EqualTo(100f));
        }

        [Test]
        public void ResetHealth_RestoresToMax()
        {
            _sut.Damage(75f, Vector3.forward);
            _sut.ResetHealth();

            Assert.That(_sut.CurrentHealth, Is.EqualTo(100f));
            Assert.That(_sut.IsInvincible, Is.False);
        }

        [Test]
        public void NullTransform_HealthOperationsAreIgnoredAndLogged()
        {
            _sut.SetTransform(null);

            LogAssert.Expect(LogType.Warning, "[Phase2 Verify] HealthModel.Damage ignored: transform is null.");
            _sut.Damage(10f, Vector3.zero);
            Assert.That(_sut.CurrentHealth, Is.EqualTo(100f));

            LogAssert.Expect(LogType.Warning, "[Phase2 Verify] HealthModel.Heal ignored: transform is null.");
            _sut.Heal(10f);
            Assert.That(_sut.CurrentHealth, Is.EqualTo(100f));

            _sut.RestoreState(0f, true);
            LogAssert.Expect(LogType.Warning, "[Phase2 Verify] HealthModel.Die ignored: transform is null.");
            _sut.Die();

            LogAssert.Expect(LogType.Warning, "[Phase2 Verify] HealthModel.ResetHealth ignored: transform is null.");
            _sut.ResetHealth();

            Assert.That(_sut.CurrentHealth, Is.EqualTo(0f));
            Assert.That(_sut.IsInvincible, Is.True);
            Assert.That(_damagedPub.Messages, Is.Empty);
            Assert.That(_diedPub.Messages, Is.Empty);
            Assert.That(_healthPub.Messages, Is.Empty);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(go);
        }
    }
}   