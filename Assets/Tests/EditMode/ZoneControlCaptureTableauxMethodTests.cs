using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureTableauxMethodTests
    {
        private static ZoneControlCaptureTableauxMethodSO CreateSO(
            int closedBranchesNeeded = 6,
            int openBranchesPerBot   = 1,
            int bonusPerClosure      = 4990)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureTableauxMethodSO>();
            typeof(ZoneControlCaptureTableauxMethodSO)
                .GetField("_closedBranchesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, closedBranchesNeeded);
            typeof(ZoneControlCaptureTableauxMethodSO)
                .GetField("_openBranchesPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, openBranchesPerBot);
            typeof(ZoneControlCaptureTableauxMethodSO)
                .GetField("_bonusPerClosure", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerClosure);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureTableauxMethodController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureTableauxMethodController>();
        }

        [Test]
        public void SO_FreshInstance_ClosedBranches_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ClosedBranches, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ClosureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ClosureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesBranches()
        {
            var so = CreateSO(closedBranchesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ClosedBranches, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(closedBranchesNeeded: 3, bonusPerClosure: 4990);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(4990));
            Assert.That(so.ClosureCount,    Is.EqualTo(1));
            Assert.That(so.ClosedBranches,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(closedBranchesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesOpenBranches()
        {
            var so = CreateSO(closedBranchesNeeded: 6, openBranchesPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ClosedBranches, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(closedBranchesNeeded: 6, openBranchesPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ClosedBranches, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ClosedBranchProgress_Clamped()
        {
            var so = CreateSO(closedBranchesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ClosedBranchProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnTableauxMethodCompleted_FiresEvent()
        {
            var so    = CreateSO(closedBranchesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureTableauxMethodSO)
                .GetField("_onTableauxMethodCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(closedBranchesNeeded: 2, bonusPerClosure: 4990);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.ClosedBranches,    Is.EqualTo(0));
            Assert.That(so.ClosureCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleClosures_Accumulate()
        {
            var so = CreateSO(closedBranchesNeeded: 2, bonusPerClosure: 4990);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ClosureCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9980));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TableauxMethodSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TableauxMethodSO, Is.Null);
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
            typeof(ZoneControlCaptureTableauxMethodController)
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
