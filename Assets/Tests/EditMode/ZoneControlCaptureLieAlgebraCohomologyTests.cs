using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureLieAlgebraCohomologyTests
    {
        private static ZoneControlCaptureLieAlgebraCohomologySO CreateSO(
            int chainsNeeded      = 6,
            int boundaryPerBot    = 2,
            int bonusPerReduction = 3880)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureLieAlgebraCohomologySO>();
            typeof(ZoneControlCaptureLieAlgebraCohomologySO)
                .GetField("_chainsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, chainsNeeded);
            typeof(ZoneControlCaptureLieAlgebraCohomologySO)
                .GetField("_boundaryPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, boundaryPerBot);
            typeof(ZoneControlCaptureLieAlgebraCohomologySO)
                .GetField("_bonusPerReduction", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerReduction);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureLieAlgebraCohomologyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureLieAlgebraCohomologyController>();
        }

        [Test]
        public void SO_FreshInstance_Chains_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Chains, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ReduceCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ReduceCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesChains()
        {
            var so = CreateSO(chainsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Chains, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(chainsNeeded: 3, bonusPerReduction: 3880);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(3880));
            Assert.That(so.ReduceCount,  Is.EqualTo(1));
            Assert.That(so.Chains,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(chainsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesBoundaries()
        {
            var so = CreateSO(chainsNeeded: 6, boundaryPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Chains, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(chainsNeeded: 6, boundaryPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Chains, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ChainProgress_Clamped()
        {
            var so = CreateSO(chainsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ChainProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnLieAlgebraCohomologyReduced_FiresEvent()
        {
            var so    = CreateSO(chainsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureLieAlgebraCohomologySO)
                .GetField("_onLieAlgebraCohomologyReduced", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(chainsNeeded: 2, bonusPerReduction: 3880);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Chains,            Is.EqualTo(0));
            Assert.That(so.ReduceCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleReductions_Accumulate()
        {
            var so = CreateSO(chainsNeeded: 2, bonusPerReduction: 3880);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ReduceCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7760));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_LieAlgebraSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.LieAlgebraSO, Is.Null);
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
            typeof(ZoneControlCaptureLieAlgebraCohomologyController)
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
