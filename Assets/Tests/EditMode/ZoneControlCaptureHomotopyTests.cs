using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureHomotopyTests
    {
        private static ZoneControlCaptureHomotopySO CreateSO(
            int deformationsNeeded  = 6,
            int retractPerBot       = 1,
            int bonusPerDeformation = 3550)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureHomotopySO>();
            typeof(ZoneControlCaptureHomotopySO)
                .GetField("_deformationsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, deformationsNeeded);
            typeof(ZoneControlCaptureHomotopySO)
                .GetField("_retractPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, retractPerBot);
            typeof(ZoneControlCaptureHomotopySO)
                .GetField("_bonusPerDeformation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDeformation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureHomotopyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureHomotopyController>();
        }

        [Test]
        public void SO_FreshInstance_Deformations_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Deformations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_HomotopyCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.HomotopyCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesDeformations()
        {
            var so = CreateSO(deformationsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Deformations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(deformationsNeeded: 3, bonusPerDeformation: 3550);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,             Is.EqualTo(3550));
            Assert.That(so.HomotopyCount,  Is.EqualTo(1));
            Assert.That(so.Deformations,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(deformationsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RetractsDeformations()
        {
            var so = CreateSO(deformationsNeeded: 6, retractPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Deformations, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(deformationsNeeded: 6, retractPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Deformations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_HomotopyProgress_Clamped()
        {
            var so = CreateSO(deformationsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.HomotopyProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnHomotopyComplete_FiresEvent()
        {
            var so    = CreateSO(deformationsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureHomotopySO)
                .GetField("_onHomotopyComplete", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(deformationsNeeded: 2, bonusPerDeformation: 3550);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Deformations,      Is.EqualTo(0));
            Assert.That(so.HomotopyCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleHomotopies_Accumulate()
        {
            var so = CreateSO(deformationsNeeded: 2, bonusPerDeformation: 3550);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.HomotopyCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_HomotopySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.HomotopySO, Is.Null);
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
            typeof(ZoneControlCaptureHomotopyController)
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
