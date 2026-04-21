using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCondenserTests
    {
        private static ZoneControlCaptureCondenserSO CreateSO(
            int platesNeeded       = 6,
            int dischargePerBot    = 2,
            int bonusPerCondensation = 1360)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCondenserSO>();
            typeof(ZoneControlCaptureCondenserSO)
                .GetField("_platesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, platesNeeded);
            typeof(ZoneControlCaptureCondenserSO)
                .GetField("_dischargePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dischargePerBot);
            typeof(ZoneControlCaptureCondenserSO)
                .GetField("_bonusPerCondensation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCondensation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCondenserController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCondenserController>();
        }

        [Test]
        public void SO_FreshInstance_Plates_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Plates, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ChargeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ChargeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesPlates()
        {
            var so = CreateSO(platesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Plates, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_PlatesAtThreshold()
        {
            var so    = CreateSO(platesNeeded: 3, bonusPerCondensation: 1360);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(1360));
            Assert.That(so.ChargeCount, Is.EqualTo(1));
            Assert.That(so.Plates,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(platesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesPlates()
        {
            var so = CreateSO(platesNeeded: 6, dischargePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Plates, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(platesNeeded: 6, dischargePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Plates, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PlateProgress_Clamped()
        {
            var so = CreateSO(platesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.PlateProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCondenserCharged_FiresEvent()
        {
            var so    = CreateSO(platesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCondenserSO)
                .GetField("_onCondenserCharged", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(platesNeeded: 2, bonusPerCondensation: 1360);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Plates,            Is.EqualTo(0));
            Assert.That(so.ChargeCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCharges_Accumulate()
        {
            var so = CreateSO(platesNeeded: 2, bonusPerCondensation: 1360);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ChargeCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2720));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CondenserSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CondenserSO, Is.Null);
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
            typeof(ZoneControlCaptureCondenserController)
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
