using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureAltarTests
    {
        private static ZoneControlCaptureAltarSO CreateSO(
            int capturesNeeded      = 5,
            int desecrationPerBot   = 1,
            int bonusPerConsecration = 500)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureAltarSO>();
            typeof(ZoneControlCaptureAltarSO)
                .GetField("_capturesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, capturesNeeded);
            typeof(ZoneControlCaptureAltarSO)
                .GetField("_desecrationPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, desecrationPerBot);
            typeof(ZoneControlCaptureAltarSO)
                .GetField("_bonusPerConsecration", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerConsecration);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureAltarController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureAltarController>();
        }

        [Test]
        public void SO_FreshInstance_Offerings_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Offerings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ConsecrationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ConsecrationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesOfferings()
        {
            var so = CreateSO(capturesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Offerings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ConsecratesAtThreshold()
        {
            var so    = CreateSO(capturesNeeded: 3, bonusPerConsecration: 500);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(500));
            Assert.That(so.ConsecrationCount, Is.EqualTo(1));
            Assert.That(so.Offerings,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileGathering()
        {
            var so    = CreateSO(capturesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DesecratesOfferings()
        {
            var so = CreateSO(capturesNeeded: 5, desecrationPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Offerings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(capturesNeeded: 5, desecrationPerBot: 3);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Offerings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OfferingProgress_Clamped()
        {
            var so = CreateSO(capturesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.OfferingProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnAltarConsecrated_FiresEvent()
        {
            var so    = CreateSO(capturesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureAltarSO)
                .GetField("_onAltarConsecrated", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(capturesNeeded: 2, bonusPerConsecration: 500);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Offerings,          Is.EqualTo(0));
            Assert.That(so.ConsecrationCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleConsecrations_Accumulate()
        {
            var so = CreateSO(capturesNeeded: 2, bonusPerConsecration: 500);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ConsecrationCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(1000));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_AltarSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.AltarSO, Is.Null);
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
            typeof(ZoneControlCaptureAltarController)
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
