using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureOrbitTests
    {
        private static ZoneControlCaptureOrbitSO CreateSO(int target = 5, int drain = 1, int bonus = 250)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureOrbitSO>();
            typeof(ZoneControlCaptureOrbitSO)
                .GetField("_orbitTarget", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, target);
            typeof(ZoneControlCaptureOrbitSO)
                .GetField("_drainPerBotCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, drain);
            typeof(ZoneControlCaptureOrbitSO)
                .GetField("_bonusPerOrbit", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureOrbitController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureOrbitController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentOrbit_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentOrbit, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_OrbitCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.OrbitCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IncrementsOrbit()
        {
            var so = CreateSO(target: 5);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CurrentOrbit, Is.EqualTo(2));
            Assert.That(so.OrbitCount,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_TriggersOrbit_AtTarget()
        {
            var so = CreateSO(target: 3, bonus: 200);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.OrbitCount,        Is.EqualTo(1));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(200));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ResetsOrbit_AfterTrigger()
        {
            var so = CreateSO(target: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CurrentOrbit, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DrainsOrbit()
        {
            var so = CreateSO(target: 5, drain: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CurrentOrbit, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsAtZero()
        {
            var so = CreateSO(target: 5, drain: 3);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.CurrentOrbit, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OrbitProgress_ScalesCorrectly()
        {
            var so = CreateSO(target: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.OrbitProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(target: 2, bonus: 100);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.CurrentOrbit,      Is.EqualTo(0));
            Assert.That(so.OrbitCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_OrbitSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.OrbitSO, Is.Null);
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
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            ctrl.gameObject.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureOrbitController)
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
