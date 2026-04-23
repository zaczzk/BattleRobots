using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureDualTests
    {
        private static ZoneControlCaptureDualSO CreateSO(
            int reversalsNeeded = 5,
            int flipPerBot      = 1,
            int bonusPerDual    = 2950)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureDualSO>();
            typeof(ZoneControlCaptureDualSO)
                .GetField("_reversalsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, reversalsNeeded);
            typeof(ZoneControlCaptureDualSO)
                .GetField("_flipPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, flipPerBot);
            typeof(ZoneControlCaptureDualSO)
                .GetField("_bonusPerDual", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDual);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureDualController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureDualController>();
        }

        [Test]
        public void SO_FreshInstance_Reversals_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Reversals, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DualCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DualCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesReversals()
        {
            var so = CreateSO(reversalsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Reversals, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(reversalsNeeded: 3, bonusPerDual: 2950);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,        Is.EqualTo(2950));
            Assert.That(so.DualCount, Is.EqualTo(1));
            Assert.That(so.Reversals, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(reversalsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesReversals()
        {
            var so = CreateSO(reversalsNeeded: 5, flipPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Reversals, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(reversalsNeeded: 5, flipPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Reversals, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ReversalProgress_Clamped()
        {
            var so = CreateSO(reversalsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ReversalProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnDualFormed_FiresEvent()
        {
            var so    = CreateSO(reversalsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureDualSO)
                .GetField("_onDualFormed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(reversalsNeeded: 2, bonusPerDual: 2950);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Reversals,         Is.EqualTo(0));
            Assert.That(so.DualCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDuals_Accumulate()
        {
            var so = CreateSO(reversalsNeeded: 2, bonusPerDual: 2950);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DualCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5900));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_DualSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.DualSO, Is.Null);
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
            typeof(ZoneControlCaptureDualController)
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
