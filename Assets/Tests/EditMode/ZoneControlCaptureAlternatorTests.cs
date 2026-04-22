using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureAlternatorTests
    {
        private static ZoneControlCaptureAlternatorSO CreateSO(
            int rotationsNeeded = 5,
            int dragPerBot      = 1,
            int bonusPerCycle   = 1465)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureAlternatorSO>();
            typeof(ZoneControlCaptureAlternatorSO)
                .GetField("_rotationsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, rotationsNeeded);
            typeof(ZoneControlCaptureAlternatorSO)
                .GetField("_dragPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dragPerBot);
            typeof(ZoneControlCaptureAlternatorSO)
                .GetField("_bonusPerCycle", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCycle);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureAlternatorController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureAlternatorController>();
        }

        [Test]
        public void SO_FreshInstance_Rotations_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Rotations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CycleCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CycleCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesRotations()
        {
            var so = CreateSO(rotationsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Rotations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_RotationsAtThreshold()
        {
            var so    = CreateSO(rotationsNeeded: 3, bonusPerCycle: 1465);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,         Is.EqualTo(1465));
            Assert.That(so.CycleCount, Is.EqualTo(1));
            Assert.That(so.Rotations,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(rotationsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesRotations()
        {
            var so = CreateSO(rotationsNeeded: 5, dragPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Rotations, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(rotationsNeeded: 5, dragPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Rotations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RotationProgress_Clamped()
        {
            var so = CreateSO(rotationsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.RotationProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnAlternatorCycled_FiresEvent()
        {
            var so    = CreateSO(rotationsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureAlternatorSO)
                .GetField("_onAlternatorCycled", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(rotationsNeeded: 2, bonusPerCycle: 1465);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Rotations,         Is.EqualTo(0));
            Assert.That(so.CycleCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCycles_Accumulate()
        {
            var so = CreateSO(rotationsNeeded: 2, bonusPerCycle: 1465);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CycleCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2930));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_AlternatorSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.AlternatorSO, Is.Null);
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
            typeof(ZoneControlCaptureAlternatorController)
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
