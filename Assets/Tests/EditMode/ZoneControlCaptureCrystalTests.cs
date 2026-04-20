using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCrystalTests
    {
        private static ZoneControlCaptureCrystalSO CreateSO(
            int capturesForShatter  = 5,
            int cracksPerBotCapture = 1,
            int bonusPerShatter     = 320)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCrystalSO>();
            typeof(ZoneControlCaptureCrystalSO)
                .GetField("_capturesForShatter",  BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, capturesForShatter);
            typeof(ZoneControlCaptureCrystalSO)
                .GetField("_cracksPerBotCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, cracksPerBotCapture);
            typeof(ZoneControlCaptureCrystalSO)
                .GetField("_bonusPerShatter",     BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerShatter);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCrystalController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCrystalController>();
        }

        [Test]
        public void SO_FreshInstance_CrystalGrowth_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CrystalGrowth, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowThreshold_ReturnsZero()
        {
            var so    = CreateSO(capturesForShatter: 4);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowThreshold_IncreasesGrowth()
        {
            var so = CreateSO(capturesForShatter: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CrystalGrowth, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReachesThreshold_ReturnsBonus()
        {
            var so    = CreateSO(capturesForShatter: 3, bonusPerShatter: 320);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(320));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Shatter_ResetsGrowth()
        {
            var so = CreateSO(capturesForShatter: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CrystalGrowth, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Shatter_FiresEvent()
        {
            var so    = CreateSO(capturesForShatter: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCrystalSO)
                .GetField("_onShatter", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordBotCapture_DecrementsGrowth()
        {
            var so = CreateSO(capturesForShatter: 6, cracksPerBotCapture: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CrystalGrowth, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsGrowthAtZero()
        {
            var so = CreateSO(cracksPerBotCapture: 1);
            so.RecordBotCapture();
            Assert.That(so.CrystalGrowth, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CrystalProgress_ReflectsGrowthRatio()
        {
            var so = CreateSO(capturesForShatter: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CrystalProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ShatterCount_IncrementsOnShatter()
        {
            var so = CreateSO(capturesForShatter: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.ShatterCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(capturesForShatter: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.CrystalGrowth,     Is.EqualTo(0));
            Assert.That(so.ShatterCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CrystalSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CrystalSO, Is.Null);
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
            typeof(ZoneControlCaptureCrystalController)
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
