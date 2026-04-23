using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureContravariantTests
    {
        private static ZoneControlCaptureContravariantSO CreateSO(
            int contrasNeeded     = 7,
            int reversePerBot     = 2,
            int bonusPerContramap = 2410)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureContravariantSO>();
            typeof(ZoneControlCaptureContravariantSO)
                .GetField("_contrasNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, contrasNeeded);
            typeof(ZoneControlCaptureContravariantSO)
                .GetField("_reversePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, reversePerBot);
            typeof(ZoneControlCaptureContravariantSO)
                .GetField("_bonusPerContramap", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerContramap);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureContravariantController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureContravariantController>();
        }

        [Test]
        public void SO_FreshInstance_Contras_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Contras, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ContramapCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ContramapCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesContras()
        {
            var so = CreateSO(contrasNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Contras, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(contrasNeeded: 3, bonusPerContramap: 2410);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(2410));
            Assert.That(so.ContramapCount,    Is.EqualTo(1));
            Assert.That(so.Contras,           Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(contrasNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesContras()
        {
            var so = CreateSO(contrasNeeded: 7, reversePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Contras, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(contrasNeeded: 7, reversePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Contras, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ContraProgress_Clamped()
        {
            var so = CreateSO(contrasNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.ContraProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnContramapped_FiresEvent()
        {
            var so    = CreateSO(contrasNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureContravariantSO)
                .GetField("_onContramapped", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(contrasNeeded: 2, bonusPerContramap: 2410);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Contras,           Is.EqualTo(0));
            Assert.That(so.ContramapCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleContramaps_Accumulate()
        {
            var so = CreateSO(contrasNeeded: 2, bonusPerContramap: 2410);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ContramapCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4820));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ContravariantSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ContravariantSO, Is.Null);
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
            typeof(ZoneControlCaptureContravariantController)
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
