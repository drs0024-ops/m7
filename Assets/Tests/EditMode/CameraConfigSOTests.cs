using UnityEngine;
using Game.Core.Enums;
using Game.Gameplay.Camera;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>
    /// Tests for CameraConfigSO data-driven rig lookup and filtering.
    /// </summary>
    [TestFixture]
    public class CameraConfigSOTests
    {
        private CameraConfig _config;
        private GameObject _go;


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
            // ... other rigs
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void GetRig_ReturnsCorrectEntry_ByMode()
        {
            var rig = _config.GetRig(CameraMode.NoYFollow);
            Assert.AreEqual(CameraMode.NoYFollow, rig.mode);
            Assert.IsTrue(rig.isFollowCam);
        }

        [Test]
        public void GetRig_ReturnsDefault_ForUnknownMode()
        {
            var unknownMode = (CameraMode)999;
            var rig = _config.GetRig(unknownMode);

            // Struct can't be null — verify it's the default (all refs null):
            Assert.IsNull(rig.camera);
            Assert.IsNull(rig.composer);
            Assert.IsNull(rig.confiner);
            Assert.IsFalse(rig.isFollowCam);
        }

        [Test]
        public void FindRig_ReturnsNull_ForUnknownMode()
        {
            var unknownMode = (CameraMode)999;
            var result = _config.FindRig(unknownMode);
            Assert.IsNull(result);
        }

        [Test]
        public void FindRig_ReturnsEntry_ForKnownMode()
        {
            var result = _config.FindRig(CameraMode.LockedRoom);
            Assert.IsNotNull(result);
            Assert.AreEqual(CameraMode.LockedRoom, result.Value.mode);
            Assert.IsFalse(result.Value.isFollowCam);
        }

        [Test]
        public void GetFollowRigs_ReturnsOnlyFollowCams()
        {
            var followRigs = _config.GetFollowRigs();
            Assert.AreEqual(2, followRigs.Count);
            Assert.IsTrue(followRigs[0].isFollowCam);
            Assert.IsTrue(followRigs[1].isFollowCam);
        }

        [Test]
        public void GetNonFollowRigs_ReturnsOnlyNonFollowCams()
        {
            var nonFollowRigs = _config.GetNonFollowRigs();
            Assert.AreEqual(1, nonFollowRigs.Count);
            Assert.IsFalse(nonFollowRigs[0].isFollowCam);
            Assert.AreEqual(CameraMode.LockedRoom, nonFollowRigs[0].mode);
        }

        [Test]
        public void SetActiveComposer_UpdatesProperty()
        {
            Assert.IsNull(_config.ActiveComposer);
            _config.SetActiveComposer(null);
            Assert.IsNull(_config.ActiveComposer);
        }   
    }
}   