using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T456: <see cref="ZoneControlTurboCaptureSO"/> and
    /// <see cref="ZoneControlTurboCaptureController"/>.
    ///
    /// ZoneControlTurboCaptureTests (12):
    ///   SO_FreshInstance_TurboCount_Zero                                       x1
    ///   SO_FreshInstance_TotalCaptures_Zero                                    x1
    ///   SO_RecordCapture_IncrementsCaptures                                    x1
    ///   SO_RecordCapture_BelowInterval_NoTurbo                                 x1
    ///   SO_RecordCapture_AtInterval_TriggersTurbo                              x1
    ///   SO_RecordCapture_AtInterval_AwardsBonus                                x1
    ///   SO_NextTurboIn_DecreasesWithCaptures                                   x1
    ///   SO_NextTurboIn_ResetsAfterTurbo                                        x1
    ///   SO_Reset_ClearsAll                                                     x1
    ///   Controller_FreshInstance_TurboSO_Null                                  x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                              x1
    ///   Controller_Refresh_NullSO_HidesPanel                                   x1
    /// </summary>
    public sealed class ZoneControlTurboCaptureTests
    {
        private static ZoneControlTurboCaptureSO CreateSO(
            int turboInterval = 5,
            int bonusPerTurbo = 200)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlTurboCaptureSO>();
            typeof(ZoneControlTurboCaptureSO)
                .GetField("_turboInterval", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, turboInterval);
            typeof(ZoneControlTurboCaptureSO)
                .GetField("_bonusPerTurbo", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerTurbo);
            so.Reset();
            return so;
        }

        private static ZoneControlTurboCaptureController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlTurboCaptureController>();
        }

        [Test]
        public void SO_FreshInstance_TurboCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TurboCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalCaptures_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_IncrementsCaptures()
        {
            var so = CreateSO();
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.TotalCaptures, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_BelowInterval_NoTurbo()
        {
            var so = CreateSO(turboInterval: 5);
            so.RecordCapture();
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.TurboCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AtInterval_TriggersTurbo()
        {
            var so = CreateSO(turboInterval: 5);
            for (int i = 0; i < 5; i++) so.RecordCapture();
            Assert.That(so.TurboCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AtInterval_AwardsBonus()
        {
            var so = CreateSO(turboInterval: 5, bonusPerTurbo: 200);
            for (int i = 0; i < 5; i++) so.RecordCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(200));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_NextTurboIn_DecreasesWithCaptures()
        {
            var so = CreateSO(turboInterval: 5);
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.NextTurboIn, Is.EqualTo(3));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_NextTurboIn_ResetsAfterTurbo()
        {
            var so = CreateSO(turboInterval: 5);
            for (int i = 0; i < 5; i++) so.RecordCapture();
            Assert.That(so.NextTurboIn, Is.EqualTo(5));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(turboInterval: 5, bonusPerTurbo: 200);
            for (int i = 0; i < 5; i++) so.RecordCapture();
            so.Reset();
            Assert.That(so.TotalCaptures,    Is.EqualTo(0));
            Assert.That(so.TurboCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TurboSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TurboSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(true));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlTurboCaptureController)
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
