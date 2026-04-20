using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePyramidTests
    {
        private static ZoneControlCapturePyramidSO CreateSO(
            int capturesPerTier = 3,
            int maxTiers        = 3,
            int bonusPerPyramid = 400)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePyramidSO>();
            typeof(ZoneControlCapturePyramidSO)
                .GetField("_capturesPerTier",  BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, capturesPerTier);
            typeof(ZoneControlCapturePyramidSO)
                .GetField("_maxTiers",         BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxTiers);
            typeof(ZoneControlCapturePyramidSO)
                .GetField("_bonusPerPyramid",  BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerPyramid);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePyramidController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePyramidController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentTier_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentTier, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowTierThreshold_ReturnsZero()
        {
            var so    = CreateSO(capturesPerTier: 3, maxTiers: 3);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_CompletingTier_AdvancesTier()
        {
            var so = CreateSO(capturesPerTier: 2, maxTiers: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CurrentTier,   Is.EqualTo(1));
            Assert.That(so.TierCaptures,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_CompletesPyramid_ReturnsBonusAndResets()
        {
            var so    = CreateSO(capturesPerTier: 1, maxTiers: 2, bonusPerPyramid: 400);
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(400));
            Assert.That(so.PyramidCount,  Is.EqualTo(1));
            Assert.That(so.CurrentTier,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_CompletesPyramid_FiresEvent()
        {
            var so    = CreateSO(capturesPerTier: 1, maxTiers: 1);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePyramidSO)
                .GetField("_onPyramidComplete", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordPlayerCapture_MultiplePyramids_CountAccumulates()
        {
            var so = CreateSO(capturesPerTier: 1, maxTiers: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.PyramidCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_CollapsesOneTier()
        {
            var so = CreateSO(capturesPerTier: 1, maxTiers: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CurrentTier,  Is.EqualTo(1));
            Assert.That(so.TierCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_WhenTierZero_ReducesTierCaptures()
        {
            var so = CreateSO(capturesPerTier: 4, maxTiers: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CurrentTier,  Is.EqualTo(0));
            Assert.That(so.TierCaptures, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_WhenEmpty_ClampsAtZero()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            Assert.That(so.CurrentTier,  Is.EqualTo(0));
            Assert.That(so.TierCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TierProgress_ReflectsCaptureRatio()
        {
            var so = CreateSO(capturesPerTier: 4, maxTiers: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.TierProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(capturesPerTier: 1, maxTiers: 1);
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.CurrentTier,       Is.EqualTo(0));
            Assert.That(so.TierCaptures,      Is.EqualTo(0));
            Assert.That(so.PyramidCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PyramidSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PyramidSO, Is.Null);
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
            typeof(ZoneControlCapturePyramidController)
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
