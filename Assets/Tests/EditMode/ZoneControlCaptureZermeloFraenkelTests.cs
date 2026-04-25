using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureZermeloFraenkelTests
    {
        private static ZoneControlCaptureZermeloFraenkelSO CreateSO(
            int verifiedAxiomsNeeded         = 7,
            int paradoxInconsistenciesPerBot = 2,
            int bonusPerAxiomSet             = 4795)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureZermeloFraenkelSO>();
            typeof(ZoneControlCaptureZermeloFraenkelSO)
                .GetField("_verifiedAxiomsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, verifiedAxiomsNeeded);
            typeof(ZoneControlCaptureZermeloFraenkelSO)
                .GetField("_paradoxInconsistenciesPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, paradoxInconsistenciesPerBot);
            typeof(ZoneControlCaptureZermeloFraenkelSO)
                .GetField("_bonusPerAxiomSet", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerAxiomSet);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureZermeloFraenkelController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureZermeloFraenkelController>();
        }

        [Test]
        public void SO_FreshInstance_VerifiedAxioms_Zero()
        {
            var so = CreateSO();
            Assert.That(so.VerifiedAxioms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_AxiomSetCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.AxiomSetCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesAxioms()
        {
            var so = CreateSO(verifiedAxiomsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.VerifiedAxioms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(verifiedAxiomsNeeded: 3, bonusPerAxiomSet: 4795);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(4795));
            Assert.That(so.AxiomSetCount, Is.EqualTo(1));
            Assert.That(so.VerifiedAxioms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(verifiedAxiomsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesInconsistencies()
        {
            var so = CreateSO(verifiedAxiomsNeeded: 7, paradoxInconsistenciesPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.VerifiedAxioms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(verifiedAxiomsNeeded: 7, paradoxInconsistenciesPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.VerifiedAxioms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_VerifiedAxiomProgress_Clamped()
        {
            var so = CreateSO(verifiedAxiomsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.VerifiedAxiomProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnZermeloFraenkelVerified_FiresEvent()
        {
            var so    = CreateSO(verifiedAxiomsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureZermeloFraenkelSO)
                .GetField("_onZermeloFraenkelVerified", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(verifiedAxiomsNeeded: 2, bonusPerAxiomSet: 4795);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.VerifiedAxioms,    Is.EqualTo(0));
            Assert.That(so.AxiomSetCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleAxiomSets_Accumulate()
        {
            var so = CreateSO(verifiedAxiomsNeeded: 2, bonusPerAxiomSet: 4795);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.AxiomSetCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9590));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ZermeloFraenkelSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ZermeloFraenkelSO, Is.Null);
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
            typeof(ZoneControlCaptureZermeloFraenkelController)
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
