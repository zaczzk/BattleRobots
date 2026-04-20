using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureFluxTests
    {
        private static ZoneControlCaptureFluxSO CreateSO(float minGap = 3f, float maxGap = 20f, int bonusPerSecond = 15)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureFluxSO>();
            typeof(ZoneControlCaptureFluxSO)
                .GetField("_minGapSeconds", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, minGap);
            typeof(ZoneControlCaptureFluxSO)
                .GetField("_maxGapSeconds", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxGap);
            typeof(ZoneControlCaptureFluxSO)
                .GetField("_bonusPerSecond", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerSecond);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureFluxController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureFluxController>();
        }

        [Test]
        public void SO_FreshInstance_FluxCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FluxCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalBonusAwarded_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FirstCapture_ReturnsNoBonus()
        {
            var so    = CreateSO();
            int bonus = so.RecordPlayerCapture(0f);
            Assert.That(bonus, Is.EqualTo(0));
            Assert.That(so.FluxCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FirstCapture_SetsPriorCaptureFlag()
        {
            var so = CreateSO();
            so.RecordPlayerCapture(0f);
            Assert.That(so.HasPriorCapture, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TwoCaptures_NormalGap_ReturnsScaledBonus()
        {
            var so    = CreateSO(minGap: 3f, maxGap: 20f, bonusPerSecond: 10);
            so.RecordPlayerCapture(0f);
            int bonus = so.RecordPlayerCapture(10f);
            Assert.That(bonus, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TwoCaptures_BelowMinGap_ClampsToMin()
        {
            var so    = CreateSO(minGap: 5f, maxGap: 20f, bonusPerSecond: 10);
            so.RecordPlayerCapture(0f);
            int bonus = so.RecordPlayerCapture(1f);
            Assert.That(bonus, Is.EqualTo(50));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TwoCaptures_AboveMaxGap_ClampsToMax()
        {
            var so    = CreateSO(minGap: 3f, maxGap: 10f, bonusPerSecond: 10);
            so.RecordPlayerCapture(0f);
            int bonus = so.RecordPlayerCapture(50f);
            Assert.That(bonus, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ResetsPriorCapture()
        {
            var so = CreateSO();
            so.RecordPlayerCapture(0f);
            so.RecordBotCapture();
            Assert.That(so.HasPriorCapture, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ThenPlayerCapture_NoBonus()
        {
            var so = CreateSO(minGap: 3f, maxGap: 20f, bonusPerSecond: 10);
            so.RecordPlayerCapture(0f);
            so.RecordBotCapture();
            int bonus = so.RecordPlayerCapture(10f);
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FiresEvent()
        {
            var so    = CreateSO(minGap: 1f, maxGap: 20f, bonusPerSecond: 10);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureFluxSO)
                .GetField("_onFlux", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture(0f);
            so.RecordPlayerCapture(5f);
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(minGap: 3f, maxGap: 20f, bonusPerSecond: 10);
            so.RecordPlayerCapture(0f);
            so.RecordPlayerCapture(10f);
            so.Reset();
            Assert.That(so.FluxCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.HasPriorCapture, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FluxSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FluxSO, Is.Null);
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
            typeof(ZoneControlCaptureFluxController)
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
