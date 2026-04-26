using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureIntersectionTypesTests
    {
        private static ZoneControlCaptureIntersectionTypesSO CreateSO(
            int witnessesNeeded     = 6,
            int typeConflictsPerBot = 1,
            int bonusPerIntersection = 5200)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureIntersectionTypesSO>();
            typeof(ZoneControlCaptureIntersectionTypesSO)
                .GetField("_witnessesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, witnessesNeeded);
            typeof(ZoneControlCaptureIntersectionTypesSO)
                .GetField("_typeConflictsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, typeConflictsPerBot);
            typeof(ZoneControlCaptureIntersectionTypesSO)
                .GetField("_bonusPerIntersection", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerIntersection);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureIntersectionTypesController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureIntersectionTypesController>();
        }

        [Test]
        public void SO_FreshInstance_Witnesses_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Witnesses, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IntersectionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.IntersectionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesWitnesses()
        {
            var so = CreateSO(witnessesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Witnesses, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(witnessesNeeded: 3, bonusPerIntersection: 5200);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(5200));
            Assert.That(so.IntersectionCount, Is.EqualTo(1));
            Assert.That(so.Witnesses,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(witnessesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesTypeConflicts()
        {
            var so = CreateSO(witnessesNeeded: 6, typeConflictsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Witnesses, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(witnessesNeeded: 6, typeConflictsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Witnesses, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_WitnessProgress_Clamped()
        {
            var so = CreateSO(witnessesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.WitnessProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnIntersectionTypesCompleted_FiresEvent()
        {
            var so    = CreateSO(witnessesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureIntersectionTypesSO)
                .GetField("_onIntersectionTypesCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(witnessesNeeded: 2, bonusPerIntersection: 5200);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Witnesses,        Is.EqualTo(0));
            Assert.That(so.IntersectionCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleIntersections_Accumulate()
        {
            var so = CreateSO(witnessesNeeded: 2, bonusPerIntersection: 5200);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.IntersectionCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(10400));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_IntersectionTypesSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.IntersectionTypesSO, Is.Null);
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
            typeof(ZoneControlCaptureIntersectionTypesController)
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
