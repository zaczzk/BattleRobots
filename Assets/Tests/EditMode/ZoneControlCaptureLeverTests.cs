using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureLeverTests
    {
        private static ZoneControlCaptureLeverSO CreateSO(
            int liftsNeeded     = 4,
            int dropPerBot      = 1,
            int bonusPerFulcrum = 1135)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureLeverSO>();
            typeof(ZoneControlCaptureLeverSO)
                .GetField("_liftsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, liftsNeeded);
            typeof(ZoneControlCaptureLeverSO)
                .GetField("_dropPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dropPerBot);
            typeof(ZoneControlCaptureLeverSO)
                .GetField("_bonusPerFulcrum", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerFulcrum);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureLeverController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureLeverController>();
        }

        [Test]
        public void SO_FreshInstance_Lifts_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Lifts, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_FulcrumCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FulcrumCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesLifts()
        {
            var so = CreateSO(liftsNeeded: 4);
            so.RecordPlayerCapture();
            Assert.That(so.Lifts, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_LiftsAtThreshold()
        {
            var so    = CreateSO(liftsNeeded: 3, bonusPerFulcrum: 1135);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(1135));
            Assert.That(so.FulcrumCount,  Is.EqualTo(1));
            Assert.That(so.Lifts,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(liftsNeeded: 4);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesLifts()
        {
            var so = CreateSO(liftsNeeded: 4, dropPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Lifts, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(liftsNeeded: 4, dropPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Lifts, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LiftProgress_Clamped()
        {
            var so = CreateSO(liftsNeeded: 4);
            so.RecordPlayerCapture();
            Assert.That(so.LiftProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnLeverFulcrumed_FiresEvent()
        {
            var so    = CreateSO(liftsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureLeverSO)
                .GetField("_onLeverFulcrumed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(liftsNeeded: 2, bonusPerFulcrum: 1135);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Lifts,             Is.EqualTo(0));
            Assert.That(so.FulcrumCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleFulcrums_Accumulate()
        {
            var so = CreateSO(liftsNeeded: 2, bonusPerFulcrum: 1135);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.FulcrumCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2270));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_LeverSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.LeverSO, Is.Null);
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
            typeof(ZoneControlCaptureLeverController)
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
