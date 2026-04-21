using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureScepterTests
    {
        private static ZoneControlCaptureScepterSO CreateSO(
            int gemsNeeded          = 5,
            int lossPerBot          = 1,
            int bonusPerInvestiture = 625)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureScepterSO>();
            typeof(ZoneControlCaptureScepterSO)
                .GetField("_gemsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, gemsNeeded);
            typeof(ZoneControlCaptureScepterSO)
                .GetField("_lossPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, lossPerBot);
            typeof(ZoneControlCaptureScepterSO)
                .GetField("_bonusPerInvestiture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerInvestiture);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureScepterController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureScepterController>();
        }

        [Test]
        public void SO_FreshInstance_Gems_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Gems, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_InvestitureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.InvestitureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesGems()
        {
            var so = CreateSO(gemsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Gems, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_InvestsAtThreshold()
        {
            var so    = CreateSO(gemsNeeded: 3, bonusPerInvestiture: 625);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(625));
            Assert.That(so.InvestitureCount,  Is.EqualTo(1));
            Assert.That(so.Gems,              Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(gemsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesGems()
        {
            var so = CreateSO(gemsNeeded: 5, lossPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Gems, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(gemsNeeded: 5, lossPerBot: 3);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Gems, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GemProgress_Clamped()
        {
            var so = CreateSO(gemsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.GemProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnScepterInvested_FiresEvent()
        {
            var so    = CreateSO(gemsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureScepterSO)
                .GetField("_onScepterInvested", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(gemsNeeded: 2, bonusPerInvestiture: 625);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Gems,              Is.EqualTo(0));
            Assert.That(so.InvestitureCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleInvestitures_Accumulate()
        {
            var so = CreateSO(gemsNeeded: 2, bonusPerInvestiture: 625);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.InvestitureCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1250));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ScepterSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ScepterSO, Is.Null);
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
            typeof(ZoneControlCaptureScepterController)
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
