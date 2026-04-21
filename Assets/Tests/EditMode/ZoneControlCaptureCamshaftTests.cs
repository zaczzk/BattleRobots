using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCamshaftTests
    {
        private static ZoneControlCaptureCamshaftSO CreateSO(
            int lobesNeeded      = 5,
            int wearPerBot       = 1,
            int bonusPerRotation = 1225)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCamshaftSO>();
            typeof(ZoneControlCaptureCamshaftSO)
                .GetField("_lobesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, lobesNeeded);
            typeof(ZoneControlCaptureCamshaftSO)
                .GetField("_wearPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, wearPerBot);
            typeof(ZoneControlCaptureCamshaftSO)
                .GetField("_bonusPerRotation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerRotation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCamshaftController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCamshaftController>();
        }

        [Test]
        public void SO_FreshInstance_Lobes_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Lobes, Is.EqualTo(0));
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
        public void SO_RecordPlayerCapture_AccumulatesLobes()
        {
            var so = CreateSO(lobesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Lobes, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_LobesAtThreshold()
        {
            var so    = CreateSO(lobesNeeded: 3, bonusPerRotation: 1225);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,             Is.EqualTo(1225));
            Assert.That(so.RotationCount, Is.EqualTo(1));
            Assert.That(so.Lobes,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(lobesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesLobes()
        {
            var so = CreateSO(lobesNeeded: 5, wearPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Lobes, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(lobesNeeded: 5, wearPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Lobes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LobeProgress_Clamped()
        {
            var so = CreateSO(lobesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.LobeProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCamshaftRotated_FiresEvent()
        {
            var so    = CreateSO(lobesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCamshaftSO)
                .GetField("_onCamshaftRotated", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(lobesNeeded: 2, bonusPerRotation: 1225);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Lobes,             Is.EqualTo(0));
            Assert.That(so.RotationCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleRotations_Accumulate()
        {
            var so = CreateSO(lobesNeeded: 2, bonusPerRotation: 1225);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.RotationCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2450));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CamshaftSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CamshaftSO, Is.Null);
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
            typeof(ZoneControlCaptureCamshaftController)
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
