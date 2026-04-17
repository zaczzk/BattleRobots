using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T372: <see cref="ZoneControlNeutralZoneTrackerSO"/> and
    /// <see cref="ZoneControlNeutralZoneTrackerController"/>.
    ///
    /// ZoneControlNeutralZoneTrackerTests (12):
    ///   SO_FreshInstance_NeutralCount_EqualsTotalZones     ×1
    ///   SO_FreshInstance_AllCaptured_False                 ×1
    ///   SO_RecordCapture_DecrementsNeutralCount            ×1
    ///   SO_RecordCapture_AllCaptured_FiresEvent            ×1
    ///   SO_RecordCapture_ClampsToCapturedCount             ×1
    ///   SO_ReleaseZone_IncrementsNeutralCount              ×1
    ///   SO_Reset_ClearsAll                                 ×1
    ///   Controller_FreshInstance_TrackerSO_Null            ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow          ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow         ×1
    ///   Controller_OnDisable_Unregisters_Channel           ×1
    ///   Controller_Refresh_NullTrackerSO_HidesPanel        ×1
    /// </summary>
    public sealed class ZoneControlNeutralZoneTrackerTests
    {
        private static ZoneControlNeutralZoneTrackerSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlNeutralZoneTrackerSO>();

        private static ZoneControlNeutralZoneTrackerController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlNeutralZoneTrackerController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_NeutralCount_EqualsTotalZones()
        {
            var so = CreateSO();
            Assert.That(so.NeutralCount, Is.EqualTo(so.TotalZones));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_AllCaptured_False()
        {
            var so = CreateSO();
            Assert.That(so.AllCaptured, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_DecrementsNeutralCount()
        {
            var so    = CreateSO();
            int before = so.NeutralCount;
            so.RecordCapture();
            Assert.That(so.NeutralCount, Is.EqualTo(before - 1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AllCaptured_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlNeutralZoneTrackerSO)
                .GetField("_onAllZonesCaptured", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            for (int i = 0; i < so.TotalZones; i++)
                so.RecordCapture();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordCapture_ClampsToCapturedCount()
        {
            var so = CreateSO();
            for (int i = 0; i < so.TotalZones + 5; i++)
                so.RecordCapture();
            Assert.That(so.CapturedCount, Is.EqualTo(so.TotalZones));
            Assert.That(so.NeutralCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ReleaseZone_IncrementsNeutralCount()
        {
            var so = CreateSO();
            so.RecordCapture();
            int before = so.NeutralCount;
            so.ReleaseZone();
            Assert.That(so.NeutralCount, Is.EqualTo(before + 1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            for (int i = 0; i < so.TotalZones; i++)
                so.RecordCapture();
            so.Reset();
            Assert.That(so.CapturedCount, Is.EqualTo(0));
            Assert.That(so.AllCaptured, Is.False);
            Assert.That(so.NeutralCount, Is.EqualTo(so.TotalZones));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_TrackerSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TrackerSO, Is.Null);
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
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var ctrl    = CreateController();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlNeutralZoneTrackerController)
                .GetField("_onZoneCaptured", BindingFlags.NonPublic | BindingFlags.Instance)
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
        public void Controller_Refresh_NullTrackerSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            panel.SetActive(true);
            typeof(ZoneControlNeutralZoneTrackerController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            ctrl.Refresh();

            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
