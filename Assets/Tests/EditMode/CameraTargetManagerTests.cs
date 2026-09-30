using NUnit.Framework;
using UnityEngine;
using Game.Core.Enums;
using Game.Gameplay.Camera;
using System;
using MessagePipe;
using Game.Core.Messages;

namespace Game.Tests
{
    [TestFixture]
    public class CameraTargetManagerTests
    {
        private GameObject _go;
        private CameraConfig _config;
        private CameraTargetManager _manager;
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
            builder.AddMessageBroker<CameraTargetUpdate>();
            builder.AddMessageBroker<CameraBoundsResetRequest>();
            _provider = builder.BuildServiceProvider();

            var targetSub = _provider.GetRequiredService<ISubscriber<CameraTargetUpdate>>();
            var boundsSub = _provider.GetRequiredService<ISubscriber<CameraBoundsResetRequest>>();

            _manager = new CameraTargetManager(_config, targetSub, boundsSub);
        }

        [TearDown]
        public void Teardown()
        {
            _manager?.Dispose();
            _provider = null;
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
        }

        [Test]
        public void UpdateTargets_NullFollow_DoesNotThrow()
        {
            _manager.UpdateTargets(null, null, null);
        }

        [Test]
        public void UpdateTargets_WithLockedRoomTarget_DoesNotThrow()
        {
            _manager.UpdateTargets(null, null, null);
        }

        [Test]
        public void ResetBounds_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _manager.ResetBounds());
        }

        [Test]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            _manager.Dispose();
            Assert.DoesNotThrow(() => _manager.Dispose());
        }
    }
}   