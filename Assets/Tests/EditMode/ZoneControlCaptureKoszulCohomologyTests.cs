using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureKoszulCohomologyTests
    {
        private static ZoneControlCaptureKoszulCohomologySO CreateSO(
            int complexesNeeded    = 6,
            int syzygyPerBot       = 2,
            int bonusPerResolution = 3940)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureKoszulCohomologySO>();
            typeof(ZoneControlCaptureKoszulCohomologySO)
                .GetField("_complexesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, complexesNeeded);
            typeof(ZoneControlCaptureKoszulCohomologySO)
                .GetField("_syzygyPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, syzygyPerBot);
            typeof(ZoneControlCaptureKoszulCohomologySO)
                .GetField("_bonusPerResolution", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerResolution);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureKoszulCohomologyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureKoszulCohomologyController>();
        }

        [Test]
        public void SO_FreshInstance_Complexes_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Complexes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ResolveCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ResolveCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesComplexes()
        {
            var so = CreateSO(complexesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Complexes, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(complexesNeeded: 3, bonusPerResolution: 3940);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(3940));
            Assert.That(so.ResolveCount,  Is.EqualTo(1));
            Assert.That(so.Complexes,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(complexesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesSyzygies()
        {
            var so = CreateSO(complexesNeeded: 6, syzygyPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Complexes, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(complexesNeeded: 6, syzygyPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Complexes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComplexProgress_Clamped()
        {
            var so = CreateSO(complexesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ComplexProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnKoszulCohomologyResolved_FiresEvent()
        {
            var so    = CreateSO(complexesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureKoszulCohomologySO)
                .GetField("_onKoszulCohomologyResolved", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(complexesNeeded: 2, bonusPerResolution: 3940);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Complexes,         Is.EqualTo(0));
            Assert.That(so.ResolveCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleResolutions_Accumulate()
        {
            var so = CreateSO(complexesNeeded: 2, bonusPerResolution: 3940);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ResolveCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7880));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_KoszulSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.KoszulSO, Is.Null);
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
            typeof(ZoneControlCaptureKoszulCohomologyController)
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
