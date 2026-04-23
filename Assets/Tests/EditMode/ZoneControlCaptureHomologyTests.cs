using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureHomologyTests
    {
        private static ZoneControlCaptureHomologySO CreateSO(
            int cyclesNeeded    = 6,
            int tearPerBot      = 2,
            int bonusPerHomology = 2620)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureHomologySO>();
            typeof(ZoneControlCaptureHomologySO)
                .GetField("_cyclesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, cyclesNeeded);
            typeof(ZoneControlCaptureHomologySO)
                .GetField("_tearPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, tearPerBot);
            typeof(ZoneControlCaptureHomologySO)
                .GetField("_bonusPerHomology", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerHomology);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureHomologyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureHomologyController>();
        }

        [Test]
        public void SO_FreshInstance_Cycles_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Cycles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_HomologyCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.HomologyCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesCycles()
        {
            var so = CreateSO(cyclesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Cycles, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(cyclesNeeded: 3, bonusPerHomology: 2620);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(2620));
            Assert.That(so.HomologyCount,    Is.EqualTo(1));
            Assert.That(so.Cycles,           Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(cyclesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesCycles()
        {
            var so = CreateSO(cyclesNeeded: 6, tearPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Cycles, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(cyclesNeeded: 6, tearPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Cycles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CycleProgress_Clamped()
        {
            var so = CreateSO(cyclesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.CycleProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnHomologyFormed_FiresEvent()
        {
            var so    = CreateSO(cyclesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureHomologySO)
                .GetField("_onHomologyFormed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(cyclesNeeded: 2, bonusPerHomology: 2620);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Cycles,            Is.EqualTo(0));
            Assert.That(so.HomologyCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleHomologies_Accumulate()
        {
            var so = CreateSO(cyclesNeeded: 2, bonusPerHomology: 2620);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.HomologyCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5240));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_HomologySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.HomologySO, Is.Null);
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
            typeof(ZoneControlCaptureHomologyController)
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
