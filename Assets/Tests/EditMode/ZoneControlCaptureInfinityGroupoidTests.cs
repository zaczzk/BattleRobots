using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureInfinityGroupoidTests
    {
        private static ZoneControlCaptureInfinityGroupoidSO CreateSO(
            int hornFillingsNeeded       = 5,
            int degenerateSimplicesPerBot = 1,
            int bonusPerFill              = 4165)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureInfinityGroupoidSO>();
            typeof(ZoneControlCaptureInfinityGroupoidSO)
                .GetField("_hornFillingsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, hornFillingsNeeded);
            typeof(ZoneControlCaptureInfinityGroupoidSO)
                .GetField("_degenerateSimplicesPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, degenerateSimplicesPerBot);
            typeof(ZoneControlCaptureInfinityGroupoidSO)
                .GetField("_bonusPerFill", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerFill);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureInfinityGroupoidController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureInfinityGroupoidController>();
        }

        [Test]
        public void SO_FreshInstance_HornFillings_Zero()
        {
            var so = CreateSO();
            Assert.That(so.HornFillings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_FillCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FillCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesHornFillings()
        {
            var so = CreateSO(hornFillingsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.HornFillings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(hornFillingsNeeded: 3, bonusPerFill: 4165);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(4165));
            Assert.That(so.FillCount,   Is.EqualTo(1));
            Assert.That(so.HornFillings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(hornFillingsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesDegenerateSimplices()
        {
            var so = CreateSO(hornFillingsNeeded: 5, degenerateSimplicesPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.HornFillings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(hornFillingsNeeded: 5, degenerateSimplicesPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.HornFillings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_HornFillingProgress_Clamped()
        {
            var so = CreateSO(hornFillingsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.HornFillingProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnInfinityGroupoidFilled_FiresEvent()
        {
            var so    = CreateSO(hornFillingsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureInfinityGroupoidSO)
                .GetField("_onInfinityGroupoidFilled", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(hornFillingsNeeded: 2, bonusPerFill: 4165);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.HornFillings,      Is.EqualTo(0));
            Assert.That(so.FillCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleFills_Accumulate()
        {
            var so = CreateSO(hornFillingsNeeded: 2, bonusPerFill: 4165);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.FillCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(8330));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_InfinityGroupoidSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.InfinityGroupoidSO, Is.Null);
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
            typeof(ZoneControlCaptureInfinityGroupoidController)
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
