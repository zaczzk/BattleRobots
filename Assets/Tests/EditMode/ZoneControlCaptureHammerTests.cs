using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureHammerTests
    {
        private static ZoneControlCaptureHammerSO CreateSO(
            int strikesNeeded  = 5,
            int coolPerBot     = 1,
            int bonusPerForge  = 880)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureHammerSO>();
            typeof(ZoneControlCaptureHammerSO)
                .GetField("_strikesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, strikesNeeded);
            typeof(ZoneControlCaptureHammerSO)
                .GetField("_coolPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, coolPerBot);
            typeof(ZoneControlCaptureHammerSO)
                .GetField("_bonusPerForge", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerForge);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureHammerController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureHammerController>();
        }

        [Test]
        public void SO_FreshInstance_Strikes_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Strikes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ForgeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ForgeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesStrikes()
        {
            var so = CreateSO(strikesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Strikes, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ForgesAtThreshold()
        {
            var so    = CreateSO(strikesNeeded: 3, bonusPerForge: 880);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(880));
            Assert.That(so.ForgeCount,  Is.EqualTo(1));
            Assert.That(so.Strikes,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(strikesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_CoolsStrikes()
        {
            var so = CreateSO(strikesNeeded: 5, coolPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Strikes, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(strikesNeeded: 5, coolPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Strikes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StrikeProgress_Clamped()
        {
            var so = CreateSO(strikesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.StrikeProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnHammerForged_FiresEvent()
        {
            var so    = CreateSO(strikesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureHammerSO)
                .GetField("_onHammerForged", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(strikesNeeded: 2, bonusPerForge: 880);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Strikes,           Is.EqualTo(0));
            Assert.That(so.ForgeCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleForges_Accumulate()
        {
            var so = CreateSO(strikesNeeded: 2, bonusPerForge: 880);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ForgeCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1760));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_HammerSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.HammerSO, Is.Null);
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
            typeof(ZoneControlCaptureHammerController)
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
