using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureInfinityToposTests
    {
        private static ZoneControlCaptureInfinityToposSO CreateSO(
            int descentConditionsNeeded = 5,
            int breakPerBot             = 1,
            int bonusPerDescend         = 3715)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureInfinityToposSO>();
            typeof(ZoneControlCaptureInfinityToposSO)
                .GetField("_descentConditionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, descentConditionsNeeded);
            typeof(ZoneControlCaptureInfinityToposSO)
                .GetField("_breakPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, breakPerBot);
            typeof(ZoneControlCaptureInfinityToposSO)
                .GetField("_bonusPerDescend", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDescend);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureInfinityToposController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureInfinityToposController>();
        }

        [Test]
        public void SO_FreshInstance_DescentConditions_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DescentConditions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DescendCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DescendCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesDescentConditions()
        {
            var so = CreateSO(descentConditionsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.DescentConditions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(descentConditionsNeeded: 3, bonusPerDescend: 3715);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(3715));
            Assert.That(so.DescendCount,     Is.EqualTo(1));
            Assert.That(so.DescentConditions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(descentConditionsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_BreaksSheafAxioms()
        {
            var so = CreateSO(descentConditionsNeeded: 5, breakPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.DescentConditions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(descentConditionsNeeded: 5, breakPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.DescentConditions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_DescentProgress_Clamped()
        {
            var so = CreateSO(descentConditionsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.DescentProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnInfinityToposDescended_FiresEvent()
        {
            var so    = CreateSO(descentConditionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureInfinityToposSO)
                .GetField("_onInfinityToposDescended", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(descentConditionsNeeded: 2, bonusPerDescend: 3715);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.DescentConditions,  Is.EqualTo(0));
            Assert.That(so.DescendCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDescendings_Accumulate()
        {
            var so = CreateSO(descentConditionsNeeded: 2, bonusPerDescend: 3715);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DescendCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7430));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_InfinityToposSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.InfinityToposSO, Is.Null);
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
            typeof(ZoneControlCaptureInfinityToposController)
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
