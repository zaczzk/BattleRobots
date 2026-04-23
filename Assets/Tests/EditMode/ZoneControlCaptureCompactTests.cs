using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCompactTests
    {
        private static ZoneControlCaptureCompactSO CreateSO(
            int factorsNeeded    = 4,
            int compressionPerBot = 1,
            int bonusPerCompact  = 3055)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCompactSO>();
            typeof(ZoneControlCaptureCompactSO)
                .GetField("_factorsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, factorsNeeded);
            typeof(ZoneControlCaptureCompactSO)
                .GetField("_compressionPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, compressionPerBot);
            typeof(ZoneControlCaptureCompactSO)
                .GetField("_bonusPerCompact", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCompact);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCompactController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCompactController>();
        }

        [Test]
        public void SO_FreshInstance_Factors_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Factors, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CompactCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CompactCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesFactors()
        {
            var so = CreateSO(factorsNeeded: 4);
            so.RecordPlayerCapture();
            Assert.That(so.Factors, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(factorsNeeded: 3, bonusPerCompact: 3055);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(3055));
            Assert.That(so.CompactCount,  Is.EqualTo(1));
            Assert.That(so.Factors,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(factorsNeeded: 4);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesFactors()
        {
            var so = CreateSO(factorsNeeded: 4, compressionPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Factors, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(factorsNeeded: 4, compressionPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Factors, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FactorProgress_Clamped()
        {
            var so = CreateSO(factorsNeeded: 4);
            so.RecordPlayerCapture();
            Assert.That(so.FactorProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCompactObjectFormed_FiresEvent()
        {
            var so    = CreateSO(factorsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCompactSO)
                .GetField("_onCompactObjectFormed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(factorsNeeded: 2, bonusPerCompact: 3055);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Factors,           Is.EqualTo(0));
            Assert.That(so.CompactCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCompact_Accumulate()
        {
            var so = CreateSO(factorsNeeded: 2, bonusPerCompact: 3055);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CompactCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6110));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CompactSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CompactSO, Is.Null);
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
            typeof(ZoneControlCaptureCompactController)
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
