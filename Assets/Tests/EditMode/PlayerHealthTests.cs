using UnityEngine;
using Game.Gameplay.Player;
using Game.Core.Messages;
using NUnit.Framework;
using Game.Core;

namespace Game.Tests
{
    [TestFixture]
    public class PlayerHealthTests
    {
        private FakePublisher<PlayerDamaged> _damagedPub;
        private FakePublisher<PlayerDied> _diedPub;
        private FakePublisher<EntityHealthChanged> _healthPub;
        private FakeInputState _input;
        private HealthModel _sut;

        [SetUp]
		public void SetUp()
		{
			_damagedPub = new FakePublisher<PlayerDamaged>();
			_diedPub = new FakePublisher<PlayerDied>();
			_healthPub = new FakePublisher<EntityHealthChanged>();
			_input = new FakeInputState();
			_sut = new HealthModel(
				_damagedPub, _diedPub, _healthPub, _input,
				new LevelStateSnapshot(0, 0f));
		}

        [Test]
        public void Damage_NonLethal_PublishesPlayerDamaged()
        {
            _sut.Damage(20f, Vector3.forward);

            Assert.That(_damagedPub.Messages, Has.Count.EqualTo(1));
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
    }
}   