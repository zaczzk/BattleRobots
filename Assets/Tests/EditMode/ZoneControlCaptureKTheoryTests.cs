using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureKTheoryTests
    {
        private static ZoneControlCaptureKTheorySO CreateSO(
            int bundlesNeeded         = 7,
            int exactSeqPerBot        = 2,
            int bonusPerClassification = 4075)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureKTheorySO>();
            typeof(ZoneControlCaptureKTheorySO)
                .GetField("_bundlesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bundlesNeeded);
            typeof(ZoneControlCaptureKTheorySO)
                .GetField("_exactSeqPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, exactSeqPerBot);
            typeof(ZoneControlCaptureKTheorySO)
                .GetField("_bonusPerClassification", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerClassification);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureKTheoryController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureKTheoryController>();
        }

        [Test]
        public void SO_FreshInstance_Bundles_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Bundles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ClassificationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ClassificationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesBundles()
        {
            var so = CreateSO(bundlesNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Bundles, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(bundlesNeeded: 3, bonusPerClassification: 4075);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                    Is.EqualTo(4075));
            Assert.That(so.ClassificationCount,   Is.EqualTo(1));
            Assert.That(so.Bundles,               Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(bundlesNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesExactSequences()
        {
            var so = CreateSO(bundlesNeeded: 7, exactSeqPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Bundles, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(bundlesNeeded: 7, exactSeqPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Bundles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BundleProgress_Clamped()
        {
            var so = CreateSO(bundlesNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.BundleProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnKTheoryClassified_FiresEvent()
        {
            var so    = CreateSO(bundlesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureKTheorySO)
                .GetField("_onKTheoryClassified", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(bundlesNeeded: 2, bonusPerClassification: 4075);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Bundles,              Is.EqualTo(0));
            Assert.That(so.ClassificationCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleClassifications_Accumulate()
        {
            var so = CreateSO(bundlesNeeded: 2, bonusPerClassification: 4075);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ClassificationCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(8150));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_KTheorySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.KTheorySO, Is.Null);
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
            typeof(ZoneControlCaptureKTheoryController)
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
