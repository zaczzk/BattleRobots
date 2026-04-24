using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSpectralHomologyTests
    {
        private static ZoneControlCaptureSpectralHomologySO CreateSO(
            int stemsNeeded         = 6,
            int differentialPerBot  = 2,
            int bonusPerConvergence = 4000)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSpectralHomologySO>();
            typeof(ZoneControlCaptureSpectralHomologySO)
                .GetField("_stemsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, stemsNeeded);
            typeof(ZoneControlCaptureSpectralHomologySO)
                .GetField("_differentialPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, differentialPerBot);
            typeof(ZoneControlCaptureSpectralHomologySO)
                .GetField("_bonusPerConvergence", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerConvergence);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSpectralHomologyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSpectralHomologyController>();
        }

        [Test]
        public void SO_FreshInstance_Stems_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Stems, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ConvergenceCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ConvergenceCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesStems()
        {
            var so = CreateSO(stemsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Stems, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(stemsNeeded: 3, bonusPerConvergence: 4000);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(4000));
            Assert.That(so.ConvergenceCount,  Is.EqualTo(1));
            Assert.That(so.Stems,             Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(stemsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ReducesStems()
        {
            var so = CreateSO(stemsNeeded: 6, differentialPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Stems, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(stemsNeeded: 6, differentialPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Stems, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StemProgress_Clamped()
        {
            var so = CreateSO(stemsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.StemProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSpectralHomologyConverged_FiresEvent()
        {
            var so    = CreateSO(stemsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSpectralHomologySO)
                .GetField("_onSpectralHomologyConverged", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(stemsNeeded: 2, bonusPerConvergence: 4000);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Stems,             Is.EqualTo(0));
            Assert.That(so.ConvergenceCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleConvergences_Accumulate()
        {
            var so = CreateSO(stemsNeeded: 2, bonusPerConvergence: 4000);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ConvergenceCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(8000));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SpectralHomologySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SpectralHomologySO, Is.Null);
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
            typeof(ZoneControlCaptureSpectralHomologyController)
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
