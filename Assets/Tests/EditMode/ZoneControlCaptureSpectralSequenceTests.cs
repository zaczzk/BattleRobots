using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSpectralSequenceTests
    {
        private static ZoneControlCaptureSpectralSequenceSO CreateSO(
            int pagesNeeded         = 5,
            int differentialsPerBot = 1,
            int bonusPerConvergence = 3745)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSpectralSequenceSO>();
            typeof(ZoneControlCaptureSpectralSequenceSO)
                .GetField("_pagesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, pagesNeeded);
            typeof(ZoneControlCaptureSpectralSequenceSO)
                .GetField("_differentialsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, differentialsPerBot);
            typeof(ZoneControlCaptureSpectralSequenceSO)
                .GetField("_bonusPerConvergence", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerConvergence);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSpectralSequenceController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSpectralSequenceController>();
        }

        [Test]
        public void SO_FreshInstance_Pages_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Pages, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ConvergeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ConvergeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesPages()
        {
            var so = CreateSO(pagesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Pages, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(pagesNeeded: 3, bonusPerConvergence: 3745);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(3745));
            Assert.That(so.ConvergeCount, Is.EqualTo(1));
            Assert.That(so.Pages,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(pagesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesDifferentials()
        {
            var so = CreateSO(pagesNeeded: 5, differentialsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Pages, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(pagesNeeded: 5, differentialsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Pages, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PageProgress_Clamped()
        {
            var so = CreateSO(pagesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.PageProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSpectralSequenceConverged_FiresEvent()
        {
            var so    = CreateSO(pagesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSpectralSequenceSO)
                .GetField("_onSpectralSequenceConverged", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(pagesNeeded: 2, bonusPerConvergence: 3745);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Pages,             Is.EqualTo(0));
            Assert.That(so.ConvergeCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleConvergences_Accumulate()
        {
            var so = CreateSO(pagesNeeded: 2, bonusPerConvergence: 3745);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ConvergeCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7490));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SpectralSequenceSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SpectralSequenceSO, Is.Null);
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
            typeof(ZoneControlCaptureSpectralSequenceController)
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
