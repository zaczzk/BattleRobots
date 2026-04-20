using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureLanternTests
    {
        private static ZoneControlCaptureLanternSO CreateSO(
            int lanternsNeeded       = 5,
            int bonusPerIllumination = 375)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureLanternSO>();
            typeof(ZoneControlCaptureLanternSO)
                .GetField("_lanternsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, lanternsNeeded);
            typeof(ZoneControlCaptureLanternSO)
                .GetField("_bonusPerIllumination", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerIllumination);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureLanternController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureLanternController>();
        }

        [Test]
        public void SO_FreshInstance_IlluminationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.IlluminationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowThreshold_ReturnsZero()
        {
            var so    = CreateSO(lanternsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_Illuminates()
        {
            var so = CreateSO(lanternsNeeded: 3);
            for (int i = 0; i < 3; i++) so.RecordPlayerCapture();
            Assert.That(so.IlluminationCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Illuminate_ReturnsBonus()
        {
            var so = CreateSO(lanternsNeeded: 2, bonusPerIllumination: 375);
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(375));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Illuminate_ResetsLitLanterns()
        {
            var so = CreateSO(lanternsNeeded: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.LitLanterns, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ReducesLitLanterns()
        {
            var so = CreateSO(lanternsNeeded: 5);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.LitLanterns, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_AtZero_ClampsToZero()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            Assert.That(so.LitLanterns, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LanternProgress_Clamped()
        {
            var so = CreateSO(lanternsNeeded: 4);
            Assert.That(so.LanternProgress, Is.InRange(0f, 1f));
            so.RecordPlayerCapture();
            Assert.That(so.LanternProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnIlluminated_FiresEvent()
        {
            var so    = CreateSO(lanternsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureLanternSO)
                .GetField("_onIlluminated", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(lanternsNeeded: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.LitLanterns,       Is.EqualTo(0));
            Assert.That(so.IlluminationCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleIlluminations_Accumulate()
        {
            var so = CreateSO(lanternsNeeded: 2);
            for (int i = 0; i < 6; i++) so.RecordPlayerCapture();
            Assert.That(so.IlluminationCount, Is.EqualTo(3));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TotalBonusAwarded_AccumulatesAcrossIlluminations()
        {
            var so = CreateSO(lanternsNeeded: 2, bonusPerIllumination: 100);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(200));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_LanternSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.LanternSO, Is.Null);
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
            typeof(ZoneControlCaptureLanternController)
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
