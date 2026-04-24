using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureResidualTests
    {
        private static ZoneControlCaptureResidualSO CreateSO(
            int residualsNeeded   = 5,
            int annihilatePerBot  = 2,
            int bonusPerResiduate = 3265)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureResidualSO>();
            typeof(ZoneControlCaptureResidualSO)
                .GetField("_residualsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, residualsNeeded);
            typeof(ZoneControlCaptureResidualSO)
                .GetField("_annihilatePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, annihilatePerBot);
            typeof(ZoneControlCaptureResidualSO)
                .GetField("_bonusPerResiduate", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerResiduate);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureResidualController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureResidualController>();
        }

        [Test]
        public void SO_FreshInstance_Residuals_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Residuals, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ResiduateCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ResiduateCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesResiduals()
        {
            var so = CreateSO(residualsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Residuals, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(residualsNeeded: 3, bonusPerResiduate: 3265);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(3265));
            Assert.That(so.ResiduateCount,  Is.EqualTo(1));
            Assert.That(so.Residuals,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(residualsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesResiduals()
        {
            var so = CreateSO(residualsNeeded: 5, annihilatePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Residuals, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(residualsNeeded: 5, annihilatePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Residuals, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ResiduateProgress_Clamped()
        {
            var so = CreateSO(residualsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ResiduateProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnResiduated_FiresEvent()
        {
            var so    = CreateSO(residualsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureResidualSO)
                .GetField("_onResiduated", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(residualsNeeded: 2, bonusPerResiduate: 3265);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Residuals,          Is.EqualTo(0));
            Assert.That(so.ResiduateCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleResiduations_Accumulate()
        {
            var so = CreateSO(residualsNeeded: 2, bonusPerResiduate: 3265);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ResiduateCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6530));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ResidualSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ResidualSO, Is.Null);
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
            typeof(ZoneControlCaptureResidualController)
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
