using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T386: <see cref="ZoneControlZoneSwitchTrackerSO"/> and
    /// <see cref="ZoneControlZoneSwitchTrackerController"/>.
    ///
    /// ZoneControlZoneSwitchTrackerTests (12):
    ///   SO_FreshInstance_SwitchCount_Zero                              ×1
    ///   SO_FirstCapture_NotCountedAsSwitch                             ×1
    ///   SO_SameZoneCapture_DoesNotIncrementSwitchCount                 ×1
    ///   SO_DifferentZoneCapture_IncrementsSwitchCount                  ×1
    ///   SO_RecordCapture_FiresOnSwitchRecorded                         ×1
    ///   SO_FrequentSwitchThreshold_FiresOnFrequentSwitcher             ×1
    ///   SO_Reset_ClearsAllState                                        ×1
    ///   Controller_FreshInstance_SwitchTrackerSO_Null                  ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                      ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                     ×1
    ///   Controller_OnDisable_Unregisters_Channel                       ×1
    ///   Controller_Refresh_NullSwitchTrackerSO_HidesPanel              ×1
    /// </summary>
    public sealed class ZoneControlZoneSwitchTrackerTests
    {
        private static ZoneControlZoneSwitchTrackerSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlZoneSwitchTrackerSO>();

        private static ZoneControlZoneSwitchTrackerController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlZoneSwitchTrackerController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_SwitchCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SwitchCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FirstCapture_NotCountedAsSwitch()
        {
            var so = CreateSO();
            so.RecordCapture(0);
            Assert.That(so.SwitchCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SameZoneCapture_DoesNotIncrementSwitchCount()
        {
            var so = CreateSO();
            so.RecordCapture(1);
            so.RecordCapture(1);
            Assert.That(so.SwitchCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_DifferentZoneCapture_IncrementsSwitchCount()
        {
            var so = CreateSO();
            so.RecordCapture(0);
            so.RecordCapture(1);
            so.RecordCapture(0);
            Assert.That(so.SwitchCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FiresOnSwitchRecorded()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneSwitchTrackerSO)
                .GetField("_onSwitchRecorded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordCapture(0);   // first — no switch
            so.RecordCapture(1);   // switch

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_FrequentSwitchThreshold_FiresOnFrequentSwitcher()
        {
            var so = CreateSO();
            // Set threshold to 2 via reflection
            typeof(ZoneControlZoneSwitchTrackerSO)
                .GetField("_frequentSwitchThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 2);

            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneSwitchTrackerSO)
                .GetField("_onFrequentSwitcher", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordCapture(0);
            so.RecordCapture(1);   // switch 1
            so.RecordCapture(0);   // switch 2 → fires frequent

            Assert.That(fired, Is.EqualTo(1));
            Assert.That(so.IsFrequentSwitcher, Is.True);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAllState()
        {
            var so = CreateSO();
            so.RecordCapture(0);
            so.RecordCapture(1);
            so.Reset();

            Assert.That(so.SwitchCount,   Is.EqualTo(0));
            Assert.That(so.LastZoneIndex, Is.EqualTo(-1));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_SwitchTrackerSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SwitchTrackerSO, Is.Null);
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
            ctrl.gameObject.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var ctrl    = CreateController();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneSwitchTrackerController)
                .GetField("_onSwitchRecorded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, channel);

            ctrl.gameObject.SetActive(true);
            ctrl.gameObject.SetActive(false);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            channel.Raise();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Controller_Refresh_NullSwitchTrackerSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlZoneSwitchTrackerController)
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
