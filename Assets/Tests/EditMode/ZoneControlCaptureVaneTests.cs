using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureVaneTests
    {
        private static ZoneControlCaptureVaneSO CreateSO(
            int spinsNeeded      = 4,
            int brakePerBot      = 1,
            int bonusPerRotation = 910)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureVaneSO>();
            typeof(ZoneControlCaptureVaneSO)
                .GetField("_spinsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, spinsNeeded);
            typeof(ZoneControlCaptureVaneSO)
                .GetField("_brakePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, brakePerBot);
            typeof(ZoneControlCaptureVaneSO)
                .GetField("_bonusPerRotation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerRotation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureVaneController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureVaneController>();
        }

        [Test]
        public void SO_FreshInstance_Spins_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Spins, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_RotationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RotationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesSpins()
        {
            var so = CreateSO(spinsNeeded: 4);
            so.RecordPlayerCapture();
            Assert.That(so.Spins, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_SpinsAtThreshold()
        {
            var so    = CreateSO(spinsNeeded: 3, bonusPerRotation: 910);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(910));
            Assert.That(so.RotationCount,   Is.EqualTo(1));
            Assert.That(so.Spins,           Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(spinsNeeded: 4);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_BrakesSpins()
        {
            var so = CreateSO(spinsNeeded: 4, brakePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Spins, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(spinsNeeded: 4, brakePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Spins, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SpinProgress_Clamped()
        {
            var so = CreateSO(spinsNeeded: 4);
            so.RecordPlayerCapture();
            Assert.That(so.SpinProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnVaneSpun_FiresEvent()
        {
            var so    = CreateSO(spinsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureVaneSO)
                .GetField("_onVaneSpun", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(spinsNeeded: 2, bonusPerRotation: 910);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Spins,             Is.EqualTo(0));
            Assert.That(so.RotationCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleRotations_Accumulate()
        {
            var so = CreateSO(spinsNeeded: 2, bonusPerRotation: 910);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.RotationCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1820));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_VaneSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.VaneSO, Is.Null);
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
            typeof(ZoneControlCaptureVaneController)
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
