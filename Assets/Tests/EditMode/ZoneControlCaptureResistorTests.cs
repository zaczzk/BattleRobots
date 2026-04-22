using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureResistorTests
    {
        private static ZoneControlCaptureResistorSO CreateSO(
            int ohmsNeeded   = 7,
            int shuntPerBot  = 2,
            int bonusPerBlock = 1555)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureResistorSO>();
            typeof(ZoneControlCaptureResistorSO)
                .GetField("_ohmsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, ohmsNeeded);
            typeof(ZoneControlCaptureResistorSO)
                .GetField("_shuntPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, shuntPerBot);
            typeof(ZoneControlCaptureResistorSO)
                .GetField("_bonusPerBlock", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerBlock);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureResistorController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureResistorController>();
        }

        [Test]
        public void SO_FreshInstance_Ohms_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Ohms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BlockCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BlockCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesOhms()
        {
            var so = CreateSO(ohmsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Ohms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_OhmsAtThreshold()
        {
            var so    = CreateSO(ohmsNeeded: 3, bonusPerBlock: 1555);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(1555));
            Assert.That(so.BlockCount, Is.EqualTo(1));
            Assert.That(so.Ohms,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(ohmsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ShuntsOhms()
        {
            var so = CreateSO(ohmsNeeded: 7, shuntPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Ohms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(ohmsNeeded: 7, shuntPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Ohms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OhmProgress_Clamped()
        {
            var so = CreateSO(ohmsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.OhmProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnResistorBlocked_FiresEvent()
        {
            var so    = CreateSO(ohmsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureResistorSO)
                .GetField("_onResistorBlocked", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(ohmsNeeded: 2, bonusPerBlock: 1555);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Ohms,              Is.EqualTo(0));
            Assert.That(so.BlockCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleBlocks_Accumulate()
        {
            var so = CreateSO(ohmsNeeded: 2, bonusPerBlock: 1555);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.BlockCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3110));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ResistorSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ResistorSO, Is.Null);
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
            typeof(ZoneControlCaptureResistorController)
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
