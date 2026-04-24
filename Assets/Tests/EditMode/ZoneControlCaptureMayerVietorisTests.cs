using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureMayerVietorisTests
    {
        private static ZoneControlCaptureMayerVietorisSO CreateSO(
            int patchesNeeded  = 5,
            int collapsePerBot = 1,
            int bonusPerStitch = 3955)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureMayerVietorisSO>();
            typeof(ZoneControlCaptureMayerVietorisSO)
                .GetField("_patchesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, patchesNeeded);
            typeof(ZoneControlCaptureMayerVietorisSO)
                .GetField("_collapsePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, collapsePerBot);
            typeof(ZoneControlCaptureMayerVietorisSO)
                .GetField("_bonusPerStitch", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerStitch);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureMayerVietorisController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureMayerVietorisController>();
        }

        [Test]
        public void SO_FreshInstance_Patches_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Patches, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_StitchCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.StitchCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesPatches()
        {
            var so = CreateSO(patchesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Patches, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(patchesNeeded: 3, bonusPerStitch: 3955);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(3955));
            Assert.That(so.StitchCount,   Is.EqualTo(1));
            Assert.That(so.Patches,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(patchesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_CollapsesPatches()
        {
            var so = CreateSO(patchesNeeded: 5, collapsePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Patches, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(patchesNeeded: 5, collapsePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Patches, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PatchProgress_Clamped()
        {
            var so = CreateSO(patchesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.PatchProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnMayerVietorisStitched_FiresEvent()
        {
            var so    = CreateSO(patchesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMayerVietorisSO)
                .GetField("_onMayerVietorisStitched", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(patchesNeeded: 2, bonusPerStitch: 3955);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Patches,           Is.EqualTo(0));
            Assert.That(so.StitchCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleStitches_Accumulate()
        {
            var so = CreateSO(patchesNeeded: 2, bonusPerStitch: 3955);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.StitchCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7910));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_MayerSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MayerSO, Is.Null);
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
            typeof(ZoneControlCaptureMayerVietorisController)
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
