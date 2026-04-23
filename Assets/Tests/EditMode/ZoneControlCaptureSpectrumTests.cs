using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSpectrumTests
    {
        private static ZoneControlCaptureSpectrumSO CreateSO(
            int bandsNeeded        = 7,
            int collapsePerBot     = 2,
            int bonusPerResolution = 2635)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSpectrumSO>();
            typeof(ZoneControlCaptureSpectrumSO)
                .GetField("_bandsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bandsNeeded);
            typeof(ZoneControlCaptureSpectrumSO)
                .GetField("_collapsePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, collapsePerBot);
            typeof(ZoneControlCaptureSpectrumSO)
                .GetField("_bonusPerResolution", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerResolution);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSpectrumController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSpectrumController>();
        }

        [Test]
        public void SO_FreshInstance_Bands_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Bands, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ResolutionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ResolutionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesBands()
        {
            var so = CreateSO(bandsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Bands, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(bandsNeeded: 3, bonusPerResolution: 2635);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(2635));
            Assert.That(so.ResolutionCount,   Is.EqualTo(1));
            Assert.That(so.Bands,             Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(bandsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesBands()
        {
            var so = CreateSO(bandsNeeded: 7, collapsePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Bands, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(bandsNeeded: 7, collapsePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Bands, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BandProgress_Clamped()
        {
            var so = CreateSO(bandsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.BandProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSpectrumResolved_FiresEvent()
        {
            var so    = CreateSO(bandsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSpectrumSO)
                .GetField("_onSpectrumResolved", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(bandsNeeded: 2, bonusPerResolution: 2635);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Bands,             Is.EqualTo(0));
            Assert.That(so.ResolutionCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleResolutions_Accumulate()
        {
            var so = CreateSO(bandsNeeded: 2, bonusPerResolution: 2635);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ResolutionCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5270));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SpectrumSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SpectrumSO, Is.Null);
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
            typeof(ZoneControlCaptureSpectrumController)
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
