using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Game.Gameplay.Camera;

namespace Game.Tests
{
    [TestFixture]
    public class CameraTargetManagerTests
    {
        private GameObject _go;
        private CameraTargetManager _manager;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestCameraTarget");
            _manager = _go.AddComponent<CameraTargetManager>();

            var initializedField = typeof(CameraTargetManager).GetField(
                "_initialized",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(initializedField, Is.Not.Null);
            initializedField.SetValue(_manager, true);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
        }

        [Test]
        public void SetTarget_ValidTransform_UpdatesActiveTarget()
        {
            var target = new GameObject("FakeTarget");
            _manager.SetTarget(target.transform);

            Assert.That(_manager.ActiveTarget, Is.EqualTo(target.transform));
            UnityEngine.Object.DestroyImmediate(target);
        }

        [Test]
        public void SetTarget_Null_IsNoOp()
        {
            var target = new GameObject("FakeTarget");
            _manager.SetTarget(target.transform);
            _manager.SetTarget(null);

            Assert.That(_manager.ActiveTarget, Is.EqualTo(target.transform));
            UnityEngine.Object.DestroyImmediate(target);
        }

        [Test]
        public void SetTarget_SecondCall_ReplacesFirst()
        {
            var target1 = new GameObject("Target1");
            var target2 = new GameObject("Target2");

            _manager.SetTarget(target1.transform);
            _manager.SetTarget(target2.transform);

            Assert.That(_manager.ActiveTarget, Is.EqualTo(target2.transform));
            UnityEngine.Object.DestroyImmediate(target1);
            UnityEngine.Object.DestroyImmediate(target2);
        }

        [Test]
        public void ActiveTarget_Default_IsNull()
        {
            Assert.That(_manager.ActiveTarget, Is.Null);
        }

        [Test]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            _manager.Dispose();
            Assert.DoesNotThrow(() => _manager.Dispose());
        }

        [Test]
        public void SetTarget_AfterDispose_DoesNotThrow()
        {
            _manager.Dispose();
            var target = new GameObject("FakeTarget");
            Assert.DoesNotThrow(() => _manager.SetTarget(target.transform));
            UnityEngine.Object.DestroyImmediate(target);
        }
    }
}   