using NUnit.Framework;
using UnityEngine;
using Game.Core.Data;
using Game.Core.Enums;
using System.Collections.Generic;
using System;

namespace Game.Tests
{
    /// <summary>
    /// EditMode tests for dialogue data and state enum.
    /// Full controller behavior tests require PlayMode.
    /// </summary>
    [TestFixture]
    public class DialogueSystemTests
    {
        private DialogueDataSO _dialogue;

        [SetUp]
        public void SetUp()
        {
            _dialogue = ScriptableObject.CreateInstance<DialogueDataSO>();
            _dialogue.speakerName = "Test NPC";
            _dialogue.paragraphs = new[] { "Hello.", "World.", "Goodbye." };
            _dialogue.typeSpeed = 12f;
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_dialogue);
        }

        // ── DialogueDataSO ──

        [Test]
        public void DialogueDataSO_HasSpeakerName()
        {
            Assert.AreEqual("Test NPC", _dialogue.speakerName);
        }

        [Test]
        public void DialogueDataSO_HasParagraphs()
        {
            Assert.AreEqual(3, _dialogue.paragraphs.Length);
            Assert.AreEqual("Hello.", _dialogue.paragraphs[0]);
            Assert.AreEqual("World.", _dialogue.paragraphs[1]);
            Assert.AreEqual("Goodbye.", _dialogue.paragraphs[2]);
        }

        [Test]
        public void DialogueDataSO_HasTypeSpeed()
        {
            Assert.AreEqual(12f, _dialogue.typeSpeed);
        }

        [Test]
        public void DialogueDataSO_DefaultTypeSpeed_IsPositive()
        {
            var fresh = ScriptableObject.CreateInstance<DialogueDataSO>();
            Assert.Greater(fresh.typeSpeed, 0f);
            UnityEngine.Object.DestroyImmediate(fresh);
        }

        // ── DialogueState Enum ──

        [Test]
        public void DialogueState_HasExpectedValues()
        {
            Assert.AreEqual(DialogueState.Idle, (DialogueState)0);
            Assert.AreEqual(DialogueState.Opening, (DialogueState)1);
            Assert.AreEqual(DialogueState.Typing, (DialogueState)2);
            Assert.AreEqual(DialogueState.Waiting, (DialogueState)3);
            Assert.AreEqual(DialogueState.Closing, (DialogueState)4);
        }

        [Test]
		public void DialogueState_HasFiveValues()
		{
			Assert.AreEqual(5, Enum.GetValues(typeof(DialogueState)).Length);
		}
        // ── SeenDialoguesSaveData ──

        [Test]
        public void SeenDialoguesSaveData_RoundTrip()
        {
            var data = new SeenDialoguesSaveData
            {
                Ids = new List<string> { "RoyalGuard_Intro", "Merchant_Hello" }
            };

            Assert.AreEqual("SeenDialogues", data.SaveId);
            Assert.AreEqual(2, data.Ids.Count);
            Assert.Contains("RoyalGuard_Intro", data.Ids);
        }
    }
}   