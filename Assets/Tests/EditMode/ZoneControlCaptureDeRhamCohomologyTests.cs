using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureDeRhamCohomologyTests
    {
        private static ZoneControlCaptureDeRhamCohomologySO CreateSO(
            int formsNeeded        = 7,
            int exactPerBot        = 2,
            int bonusPerIntegration = 3850)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureDeRhamCohomologySO>();
            typeof(ZoneControlCaptureDeRhamCohomologySO)
                .GetField("_formsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, formsNeeded);
            typeof(ZoneControlCaptureDeRhamCohomologySO)
                .GetField("_exactPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, exactPerBot);
            typeof(ZoneControlCaptureDeRhamCohomologySO)
                .GetField("_bonusPerIntegration", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerIntegration);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureDeRhamCohomologyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureDeRhamCohomologyController>();
        }

        [Test]
        public void SO_FreshInstance_Forms_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Forms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IntegrateCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.IntegrateCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesForms()
        {
            var so = CreateSO(formsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Forms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(formsNeeded: 3, bonusPerIntegration: 3850);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(3850));
            Assert.That(so.IntegrateCount,  Is.EqualTo(1));
            Assert.That(so.Forms,           Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(formsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesExactForms()
        {
            var so = CreateSO(formsNeeded: 7, exactPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Forms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(formsNeeded: 7, exactPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Forms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FormProgress_Clamped()
        {
            var so = CreateSO(formsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.FormProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnDeRhamCohomologyIntegrated_FiresEvent()
        {
            var so    = CreateSO(formsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureDeRhamCohomologySO)
                .GetField("_onDeRhamCohomologyIntegrated", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(formsNeeded: 2, bonusPerIntegration: 3850);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Forms,             Is.EqualTo(0));
            Assert.That(so.IntegrateCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleIntegrations_Accumulate()
        {
            var so = CreateSO(formsNeeded: 2, bonusPerIntegration: 3850);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.IntegrateCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7700));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_DeRhamSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.DeRhamSO, Is.Null);
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
            typeof(ZoneControlCaptureDeRhamCohomologyController)
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
