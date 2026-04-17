using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T400: <see cref="ZoneControlRivalTrackerSO"/> and
    /// <see cref="ZoneControlRivalTrackerController"/>.
    ///
    /// ZoneControlRivalTrackerTests (12):
    ///   SO_FreshInstance_BothCounts_Zero                             x1
    ///   SO_FreshInstance_IsRivalLeading_False                        x1
    ///   SO_RecordPlayerCapture_IncrementsPlayerCount                 x1
    ///   SO_RecordRivalCapture_IncrementsRivalCount                   x1
    ///   SO_IsRivalLeading_TrueWhenRivalAhead                        x1
    ///   SO_LeadDelta_PositiveWhenPlayerAhead                         x1
    ///   SO_RecordRivalCapture_FiresOnRivalLeading_OnTransition       x1
    ///   SO_RecordPlayerCapture_FiresOnPlayerLeading_OnTransition     x1
    ///   SO_OnRivalLeading_Idempotent                                 x1
    ///   SO_Reset_ClearsAll                                           x1
    ///   Controller_FreshInstance_RivalTrackerSO_Null                 x1
    ///   Controller_Refresh_NullSO_HidesPanel                         x1
    /// </summary>
    public sealed class ZoneControlRivalTrackerTests
    {
        private static ZoneControlRivalTrackerSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlRivalTrackerSO>();

        private static ZoneControlRivalTrackerController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlRivalTrackerController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_BothCounts_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PlayerCaptures, Is.EqualTo(0));
            Assert.That(so.RivalCaptures,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsRivalLeading_False()
        {
            var so = CreateSO();
            Assert.That(so.IsRivalLeading, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IncrementsPlayerCount()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.PlayerCaptures, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordRivalCapture_IncrementsRivalCount()
        {
            var so = CreateSO();
            so.RecordRivalCapture();
            Assert.That(so.RivalCaptures, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_IsRivalLeading_TrueWhenRivalAhead()
        {
            var so = CreateSO();
            so.RecordRivalCapture();
            so.RecordRivalCapture();
            so.RecordPlayerCapture();
            Assert.That(so.IsRivalLeading, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LeadDelta_PositiveWhenPlayerAhead()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordRivalCapture();
            Assert.That(so.LeadDelta, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordRivalCapture_FiresOnRivalLeading_OnTransition()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlRivalTrackerSO)
                .GetField("_onRivalLeading", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecordRivalCapture();

            Assert.That(so.IsRivalLeading, Is.True);
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordPlayerCapture_FiresOnPlayerLeading_OnTransition()
        {
            var so       = CreateSO();
            var channelP = ScriptableObject.CreateInstance<VoidGameEvent>();
            var channelR = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlRivalTrackerSO)
                .GetField("_onPlayerLeading", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channelP);
            typeof(ZoneControlRivalTrackerSO)
                .GetField("_onRivalLeading", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channelR);

            int playerFired = 0;
            channelP.RegisterCallback(() => playerFired++);

            so.RecordRivalCapture();  // rival leads → fires onRivalLeading
            so.RecordPlayerCapture(); // tie → no event
            so.RecordPlayerCapture(); // player leads → fires onPlayerLeading

            Assert.That(so.IsRivalLeading, Is.False);
            Assert.That(playerFired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channelP);
            Object.DestroyImmediate(channelR);
        }

        [Test]
        public void SO_OnRivalLeading_Idempotent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlRivalTrackerSO)
                .GetField("_onRivalLeading", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecordRivalCapture(); // rival leads — fires once
            so.RecordRivalCapture(); // still rival leading — no re-fire

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordRivalCapture();
            so.RecordRivalCapture();
            so.Reset();
            Assert.That(so.PlayerCaptures, Is.EqualTo(0));
            Assert.That(so.RivalCaptures,  Is.EqualTo(0));
            Assert.That(so.IsRivalLeading, Is.False);
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_RivalTrackerSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RivalTrackerSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlRivalTrackerController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            panel.SetActive(true);
            ctrl.Refresh();

            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
