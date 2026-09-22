using Game.Gameplay.Save;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Verifies save versioning and migration: old saves load, data is preserved,
    /// new keys default gracefully, and current-version saves skip migration.
    /// </summary>
    [TestFixture]
    public class SaveMigrationTests
    {
        private const int CurrentVersion = 1;

        #region Helpers

        /// <summary>
        /// Creates a v0 save (no "version" field) with known data.
        /// </summary>
        private static SaveDataContainer CreateV0Save()
        {
            string json = "{\"SaveDataChunks\":[" +
                "{\"Key\":\"LevelProgression\",\"Value\":\"{\\\"levelName\\\":\\\"Level_01\\\"}\"}," +
                "{\"Key\":\"VideoPlayer\",\"Value\":\"{\\\"HasPlayed\\\":true}\"}," +
                "{\"Key\":\"AudioSettings\",\"Value\":\"{\\\"Master\\\":1.0,\\\"Music\\\":0.7,\\\"SFX\\\":1.0}\"}" +
                "]}";
            return JsonUtility.FromJson<SaveDataContainer>(json);
        }

        /// <summary>
        /// Creates a v1 save (with version field) with known data.
        /// </summary>
        private static SaveDataContainer CreateV1Save()
        {
            var container = new SaveDataContainer { version = 1 };
            container.SetValue("LevelProgression", "{\"levelName\":\"Level_03\"}");
            container.SetValue("VideoPlayer", "{\"HasPlayed\":true}");
            container.SetValue("AudioSettings", "{\"Master\":1.0,\"Music\":0.5,\"SFX\":0.8}");
            return container;
        }

        #endregion

        #region Tests

        [Test]
        public void V0Save_Deserializes_WithVersionZero()
        {
            var data = CreateV0Save();
            Assert.That(data.version, Is.EqualTo(0));
        }

        [Test]
        public void Migrate_V0ToV1_BumpsVersion()
        {
            var data = CreateV0Save();
            SaveMigration.Migrate(data, data.version, CurrentVersion);
            Assert.That(data.version, Is.EqualTo(1));
        }

        [Test]
        public void Migrate_PreservesAllOriginalKeys()
        {
            var data = new SaveDataContainer();
            data.SetValue("LevelProgression", "{\"levelName\":\"Level_01\"}");
            data.SetValue("VideoPlayer", "{\"HasPlayed\":true}");
            data.SetValue("AudioSettings", "{\"Master\":1.0,\"Music\":0.7,\"SFX\":1.0}");
            data.RebuildDictionary();

            // Simulate v0: strip version (it's already 0 by default)
            Assert.That(data.version, Is.EqualTo(0));

            // Migrate
            SaveMigration.Migrate(data, data.version, CurrentVersion);
            data.RebuildDictionary();

            // Verify all keys survived
            string level, video, audio;
            Assert.That(data.TryGetValue("LevelProgression", out level), Is.True);
            Assert.That(level, Does.Contain("Level_01"));

            Assert.That(data.TryGetValue("VideoPlayer", out video), Is.True);
            Assert.That(video, Does.Contain("HasPlayed"));

            Assert.That(data.TryGetValue("AudioSettings", out audio), Is.True);
            Assert.That(audio, Does.Contain("Master"));
        }

        [Test]
        public void Migrate_MissingNewKey_ReturnsFalseGracefully()
        {
            var data = CreateV0Save();
            SaveMigration.Migrate(data, data.version, CurrentVersion);
            data.RebuildDictionary();

            // A key that doesn't exist in v0 or v1 should return false, not throw
            string value;
            Assert.That(data.TryGetValue("NonExistentKey", out value), Is.False);
            Assert.That(value, Is.Null);
        }

        [Test]
        public void RoundTrip_V1_SerializeDeserialize_DataIntact()
        {
            var original = new SaveDataContainer { version = 1 };
            original.SetValue("LevelProgression", "{\"levelName\":\"Level_03\"}");
            original.SetValue("VideoPlayer", "{\"HasPlayed\":true}");

            string json = JsonUtility.ToJson(original);

            // Verify serialized JSON contains our data
            Assert.That(json, Does.Contain("\"version\":1"));
            Assert.That(json, Does.Contain("LevelProgression"));
            Assert.That(json, Does.Contain("Level_03"));
            Assert.That(json, Does.Contain("VideoPlayer"));

            // Deserialize and verify version survived
            var loaded = JsonUtility.FromJson<SaveDataContainer>(json);
            Assert.That(loaded.version, Is.EqualTo(1));

            // Verify chunks list was populated
            Assert.That(loaded.SaveDataChunks, Is.Not.Null);
            Assert.That(loaded.SaveDataChunks.Count, Is.GreaterThanOrEqualTo(2));

            // Rebuild and verify keys are accessible
            loaded.RebuildDictionary();
            string level;
            Assert.That(loaded.TryGetValue("LevelProgression", out level), Is.True);
            Assert.That(level, Does.Contain("Level_03"));
        }   

        [Test]
        public void CurrentVersionSave_DoesNotTriggerMigration()
        {
            var data = CreateV1Save();
            data.RebuildDictionary();

            // Simulate what SaveManager.LoadAsync does:
            // if (data.version < CurrentSaveVersion) → Migrate
            bool shouldMigrate = data.version < CurrentVersion;
            Assert.That(shouldMigrate, Is.False,
                "A v1 save should NOT trigger migration when CurrentSaveVersion is 1");

            // Data should be unchanged
            string level;
            Assert.That(data.TryGetValue("LevelProgression", out level), Is.True);
            Assert.That(level, Is.EqualTo("{\"levelName\":\"Level_03\"}"));
        }

        #endregion
    }
}   