using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureAnabelianGeometryTests
    {
        private static ZoneControlCaptureAnabelianGeometrySO CreateSO(
            int fundamentalGroupDataNeeded = 5,
            int outerAutomorphismsPerBot   = 1,
            int bonusPerReconstruction     = 4270)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureAnabelianGeometrySO>();
            typeof(ZoneControlCaptureAnabelianGeometrySO)
                .GetField("_fundamentalGroupDataNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, fundamentalGroupDataNeeded);
            typeof(ZoneControlCaptureAnabelianGeometrySO)
                .GetField("_outerAutomorphismsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, outerAutomorphismsPerBot);
            typeof(ZoneControlCaptureAnabelianGeometrySO)
                .GetField("_bonusPerReconstruction", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerReconstruction);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureAnabelianGeometryController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureAnabelianGeometryController>();
        }

        [Test]
        public void SO_FreshInstance_FundamentalGroupData_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FundamentalGroupData, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ReconstructionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ReconstructionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesFundamentalGroupData()
        {
            var so = CreateSO(fundamentalGroupDataNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.FundamentalGroupData, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(fundamentalGroupDataNeeded: 3, bonusPerReconstruction: 4270);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                   Is.EqualTo(4270));
            Assert.That(so.ReconstructionCount,  Is.EqualTo(1));
            Assert.That(so.FundamentalGroupData, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(fundamentalGroupDataNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesOuterAutomorphisms()
        {
            var so = CreateSO(fundamentalGroupDataNeeded: 5, outerAutomorphismsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.FundamentalGroupData, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(fundamentalGroupDataNeeded: 5, outerAutomorphismsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.FundamentalGroupData, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FundamentalGroupProgress_Clamped()
        {
            var so = CreateSO(fundamentalGroupDataNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.FundamentalGroupProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnAnabelianGeometryReconstructed_FiresEvent()
        {
            var so    = CreateSO(fundamentalGroupDataNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureAnabelianGeometrySO)
                .GetField("_onAnabelianGeometryReconstructed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(fundamentalGroupDataNeeded: 2, bonusPerReconstruction: 4270);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.FundamentalGroupData, Is.EqualTo(0));
            Assert.That(so.ReconstructionCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleReconstructions_Accumulate()
        {
            var so = CreateSO(fundamentalGroupDataNeeded: 2, bonusPerReconstruction: 4270);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ReconstructionCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,    Is.EqualTo(8540));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_AnabelianGeometrySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.AnabelianGeometrySO, Is.Null);
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
            typeof(ZoneControlCaptureAnabelianGeometryController)
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
