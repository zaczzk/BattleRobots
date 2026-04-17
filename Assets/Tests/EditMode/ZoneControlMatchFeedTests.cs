using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for <see cref="ZoneControlMatchFeedSO"/> and
    /// <see cref="ZoneControlMatchFeedController"/>.
    /// </summary>
    public sealed class ZoneControlMatchFeedTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static ZoneControlMatchFeedSO CreateFeedSO() =>
            ScriptableObject.CreateInstance<ZoneControlMatchFeedSO>();

        private static ZoneControlMatchFeedController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlMatchFeedController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_EntryCount_Zero()
        {
            var so = CreateFeedSO();
            Assert.That(so.EntryCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddEntry_IncrementsCount()
        {
            var so = CreateFeedSO();
            so.AddEntry(1f, ZoneControlFeedEventType.ZoneCaptured, "Zone captured");
            Assert.That(so.EntryCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddEntry_EvictsOldestWhenFull()
        {
            var so = CreateFeedSO();
            // Fill to default max (10)
            for (int i = 0; i < 10; i++)
                so.AddEntry(i, ZoneControlFeedEventType.ZoneCaptured, $"Msg {i}");
            Assert.That(so.EntryCount, Is.EqualTo(10));

            so.AddEntry(10f, ZoneControlFeedEventType.BotCapture, "Extra");
            Assert.That(so.EntryCount, Is.EqualTo(10));
            Assert.That(so.Entries[so.EntryCount - 1].Message, Is.EqualTo("Extra"));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddEntry_NullMessage_StoredAsEmpty()
        {
            var so = CreateFeedSO();
            so.AddEntry(0f, ZoneControlFeedEventType.ZoneCaptured, null);
            Assert.That(so.Entries[0].Message, Is.EqualTo(string.Empty));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateFeedSO();
            so.AddEntry(0f, ZoneControlFeedEventType.VictoryAchieved, "Win");
            so.Reset();
            Assert.That(so.EntryCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_FeedSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FeedSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(true));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channels()
        {
            var ctrl    = CreateController();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            var field   = typeof(ZoneControlMatchFeedController).GetField(
                "_onFeedUpdated", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(ctrl, channel);

            ctrl.gameObject.SetActive(true);

            int count = 0;
            channel.RegisterCallback(() => count++);
            ctrl.gameObject.SetActive(false);
            channel.Raise();

            Assert.That(count, Is.EqualTo(1));
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Controller_HandleMatchStarted_ResetsFeed()
        {
            var ctrl = CreateController();
            var so   = CreateFeedSO();
            var field = typeof(ZoneControlMatchFeedController).GetField(
                "_feedSO", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(ctrl, so);

            so.AddEntry(0f, ZoneControlFeedEventType.ZoneCaptured, "test");
            Assert.That(so.EntryCount, Is.EqualTo(1));

            ctrl.HandleMatchStarted();
            Assert.That(so.EntryCount, Is.EqualTo(0));

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_HandleZoneCaptured_AddsEntry()
        {
            var ctrl = CreateController();
            var so   = CreateFeedSO();
            var field = typeof(ZoneControlMatchFeedController).GetField(
                "_feedSO", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(ctrl, so);

            ctrl.HandleZoneCaptured();
            Assert.That(so.EntryCount, Is.EqualTo(1));
            Assert.That(so.Entries[0].Type, Is.EqualTo(ZoneControlFeedEventType.ZoneCaptured));

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_Refresh_NullFeedSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            panel.SetActive(true);
            var panelField = typeof(ZoneControlMatchFeedController).GetField(
                "_panel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            panelField.SetValue(ctrl, panel);

            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.False);

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
