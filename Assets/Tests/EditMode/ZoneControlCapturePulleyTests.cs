using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePulleyTests
    {
        private static ZoneControlCapturePulleySO CreateSO(
            int hoistsNeeded = 5,
            int slipPerBot   = 1,
            int bonusPerLift = 1105)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePulleySO>();
            typeof(ZoneControlCapturePulleySO)
                .GetField("_hoistsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, hoistsNeeded);
            typeof(ZoneControlCapturePulleySO)
                .GetField("_slipPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, slipPerBot);
            typeof(ZoneControlCapturePulleySO)
                .GetField("_bonusPerLift", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerLift);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePulleyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePulleyController>();
        }

        [Test]
        public void SO_FreshInstance_Hoists_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Hoists, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_LiftCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LiftCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesHoists()
        {
            var so = CreateSO(hoistsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Hoists, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_HoistsAtThreshold()
        {
            var so    = CreateSO(hoistsNeeded: 3, bonusPerLift: 1105);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(1105));
            Assert.That(so.LiftCount,   Is.EqualTo(1));
            Assert.That(so.Hoists,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(hoistsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesHoists()
        {
            var so = CreateSO(hoistsNeeded: 5, slipPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Hoists, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(hoistsNeeded: 5, slipPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Hoists, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_HoistProgress_Clamped()
        {
            var so = CreateSO(hoistsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.HoistProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnPulleyLifted_FiresEvent()
        {
            var so    = CreateSO(hoistsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePulleySO)
                .GetField("_onPulleyLifted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(hoistsNeeded: 2, bonusPerLift: 1105);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Hoists,            Is.EqualTo(0));
            Assert.That(so.LiftCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleLifts_Accumulate()
        {
            var so = CreateSO(hoistsNeeded: 2, bonusPerLift: 1105);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.LiftCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2210));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PulleySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PulleySO, Is.Null);
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
            typeof(ZoneControlCapturePulleyController)
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
