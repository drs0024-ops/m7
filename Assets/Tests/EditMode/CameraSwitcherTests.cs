using NUnit.Framework;
using UnityEngine;
using Game.Core.Enums;
using Game.Core.Messages;
using Game.Gameplay.Camera;
using MessagePipe;
using System;

namespace Game.Tests
{
    [TestFixture]
    public class CameraSwitcherTests
    {
        private GameObject _go;
        private CameraConfig _config;
        private CameraSwitcher _switcher;
        private IServiceProvider _provider; 

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestCameraConfig");
            _config = _go.AddComponent<CameraConfig>();
            _config.rigs.Clear();
            _config.rigs.Add(new CameraRigEntry
            {
                mode = CameraMode.CenterFollow,
                camera = null,
                composer = null,
                confiner = null,
                isFollowCam = true
            });

            var builder = new BuiltinContainerBuilder();
            builder.AddMessagePipe();
            builder.AddMessageBroker<CameraSwitchRequest>();
            builder.AddMessageBroker<CameraModeActive>();
            _provider = builder.BuildServiceProvider();

            var switchSub = _provider.GetRequiredService<ISubscriber<CameraSwitchRequest>>();
            var modePublisher = _provider.GetRequiredService<IPublisher<CameraModeActive>>();

            _switcher = new CameraSwitcher(_config, switchSub, modePublisher);
        }

        [TearDown]
        public void Teardown()
        {
            _provider = null;  // drop reference; GC handles it
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
        }

        [Test]
        public void SwitchTo_UnknownMode_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _switcher.SwitchTo((CameraMode)999));
        }

        [Test]
        public void SwitchTo_AfterDispose_DoesNotThrow()
        {
            _switcher.Dispose();
            Assert.DoesNotThrow(() => _switcher.SwitchTo(CameraMode.CenterFollow));
        }

        [Test]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            _switcher.Dispose();
            Assert.DoesNotThrow(() => _switcher.Dispose());
        }
    }
}   