using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureNetTests
    {
        private static ZoneControlCaptureNetSO CreateSO(
            int termsNeeded        = 6,
            int scatterPerBot      = 1,
            int bonusPerConvergence = 3400)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureNetSO>();
            typeof(ZoneControlCaptureNetSO)
                .GetField("_termsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, termsNeeded);
            typeof(ZoneControlCaptureNetSO)
                .GetField("_scatterPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, scatterPerBot);
            typeof(ZoneControlCaptureNetSO)
                .GetField("_bonusPerConvergence", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerConvergence);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureNetController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureNetController>();
        }

        [Test]
        public void SO_FreshInstance_Terms_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Terms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ConvergenceCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ConvergenceCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesTerms()
        {
            var so = CreateSO(termsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Terms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(termsNeeded: 3, bonusPerConvergence: 3400);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(3400));
            Assert.That(so.ConvergenceCount,  Is.EqualTo(1));
            Assert.That(so.Terms,             Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(termsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ScattersTerms()
        {
            var so = CreateSO(termsNeeded: 6, scatterPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Terms, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(termsNeeded: 6, scatterPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Terms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_NetProgress_Clamped()
        {
            var so = CreateSO(termsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.NetProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnNetConverged_FiresEvent()
        {
            var so    = CreateSO(termsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureNetSO)
                .GetField("_onNetConverged", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(termsNeeded: 2, bonusPerConvergence: 3400);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Terms,             Is.EqualTo(0));
            Assert.That(so.ConvergenceCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleConvergences_Accumulate()
        {
            var so = CreateSO(termsNeeded: 2, bonusPerConvergence: 3400);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ConvergenceCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6800));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_NetSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.NetSO, Is.Null);
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
            typeof(ZoneControlCaptureNetController)
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
