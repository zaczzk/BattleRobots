using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePvsNPTests
    {
        private static ZoneControlCapturePvsNPSO CreateSO(
            int reductionsNeeded   = 5,
            int oracleBarriersPerBot = 1,
            int bonusPerReduction  = 4570)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePvsNPSO>();
            typeof(ZoneControlCapturePvsNPSO)
                .GetField("_reductionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, reductionsNeeded);
            typeof(ZoneControlCapturePvsNPSO)
                .GetField("_oracleBarriersPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, oracleBarriersPerBot);
            typeof(ZoneControlCapturePvsNPSO)
                .GetField("_bonusPerReduction", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerReduction);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePvsNPController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePvsNPController>();
        }

        [Test]
        public void SO_FreshInstance_Reductions_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Reductions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ReductionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ReductionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesReductions()
        {
            var so = CreateSO(reductionsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Reductions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(reductionsNeeded: 3, bonusPerReduction: 4570);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(4570));
            Assert.That(so.ReductionCount,  Is.EqualTo(1));
            Assert.That(so.Reductions,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(reductionsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesOracleBarriers()
        {
            var so = CreateSO(reductionsNeeded: 5, oracleBarriersPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Reductions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(reductionsNeeded: 5, oracleBarriersPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Reductions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ReductionProgress_Clamped()
        {
            var so = CreateSO(reductionsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ReductionProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnPvsNPReduced_FiresEvent()
        {
            var so    = CreateSO(reductionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePvsNPSO)
                .GetField("_onPvsNPReduced", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(reductionsNeeded: 2, bonusPerReduction: 4570);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Reductions,        Is.EqualTo(0));
            Assert.That(so.ReductionCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleReductions_Accumulate()
        {
            var so = CreateSO(reductionsNeeded: 2, bonusPerReduction: 4570);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ReductionCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9140));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PvsNPSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PvsNPSO, Is.Null);
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
            typeof(ZoneControlCapturePvsNPController)
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
