using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureTurbineTests
    {
        private static ZoneControlCaptureTurbineSO CreateSO(
            int bladesNeeded = 6,
            int stallPerBot  = 2,
            int bonusPerSpin = 1300)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureTurbineSO>();
            typeof(ZoneControlCaptureTurbineSO)
                .GetField("_bladesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bladesNeeded);
            typeof(ZoneControlCaptureTurbineSO)
                .GetField("_stallPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, stallPerBot);
            typeof(ZoneControlCaptureTurbineSO)
                .GetField("_bonusPerSpin", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerSpin);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureTurbineController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureTurbineController>();
        }

        [Test]
        public void SO_FreshInstance_Blades_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Blades, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_SpinCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SpinCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesBlades()
        {
            var so = CreateSO(bladesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Blades, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BladesAtThreshold()
        {
            var so    = CreateSO(bladesNeeded: 3, bonusPerSpin: 1300);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(1300));
            Assert.That(so.SpinCount,   Is.EqualTo(1));
            Assert.That(so.Blades,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(bladesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesBlades()
        {
            var so = CreateSO(bladesNeeded: 6, stallPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Blades, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(bladesNeeded: 6, stallPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Blades, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BladeProgress_Clamped()
        {
            var so = CreateSO(bladesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.BladeProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnTurbineSpun_FiresEvent()
        {
            var so    = CreateSO(bladesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureTurbineSO)
                .GetField("_onTurbineSpun", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(bladesNeeded: 2, bonusPerSpin: 1300);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Blades,            Is.EqualTo(0));
            Assert.That(so.SpinCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleSpins_Accumulate()
        {
            var so = CreateSO(bladesNeeded: 2, bonusPerSpin: 1300);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.SpinCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2600));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TurbineSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TurbineSO, Is.Null);
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
            typeof(ZoneControlCaptureTurbineController)
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
