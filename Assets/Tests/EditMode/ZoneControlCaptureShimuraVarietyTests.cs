using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureShimuraVarietyTests
    {
        private static ZoneControlCaptureShimuraVarietySO CreateSO(
            int cmPointsNeeded        = 5,
            int badPrimesPerBot        = 1,
            int bonusPerUniformization = 4420)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureShimuraVarietySO>();
            typeof(ZoneControlCaptureShimuraVarietySO)
                .GetField("_cmPointsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, cmPointsNeeded);
            typeof(ZoneControlCaptureShimuraVarietySO)
                .GetField("_badPrimesPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, badPrimesPerBot);
            typeof(ZoneControlCaptureShimuraVarietySO)
                .GetField("_bonusPerUniformization", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerUniformization);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureShimuraVarietyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureShimuraVarietyController>();
        }

        [Test]
        public void SO_FreshInstance_CMPoints_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CMPoints, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_UniformizationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.UniformizationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesCMPoints()
        {
            var so = CreateSO(cmPointsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.CMPoints, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(cmPointsNeeded: 3, bonusPerUniformization: 4420);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                  Is.EqualTo(4420));
            Assert.That(so.UniformizationCount, Is.EqualTo(1));
            Assert.That(so.CMPoints,            Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(cmPointsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesBadPrimes()
        {
            var so = CreateSO(cmPointsNeeded: 5, badPrimesPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CMPoints, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(cmPointsNeeded: 5, badPrimesPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CMPoints, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CMPointProgress_Clamped()
        {
            var so = CreateSO(cmPointsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.CMPointProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnShimuraVarietyUniformized_FiresEvent()
        {
            var so    = CreateSO(cmPointsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureShimuraVarietySO)
                .GetField("_onShimuraVarietyUniformized", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(cmPointsNeeded: 2, bonusPerUniformization: 4420);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.CMPoints,            Is.EqualTo(0));
            Assert.That(so.UniformizationCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleUniformizations_Accumulate()
        {
            var so = CreateSO(cmPointsNeeded: 2, bonusPerUniformization: 4420);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.UniformizationCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(8840));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ShimuraVarietySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ShimuraVarietySO, Is.Null);
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
            typeof(ZoneControlCaptureShimuraVarietyController)
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
