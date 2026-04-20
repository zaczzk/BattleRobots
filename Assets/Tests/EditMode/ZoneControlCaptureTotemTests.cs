using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureTotemTests
    {
        private static ZoneControlCaptureTotemSO CreateSO(
            int ringsNeeded   = 5,
            int ringsPerTopple = 2,
            int bonusPerTotem  = 490)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureTotemSO>();
            typeof(ZoneControlCaptureTotemSO)
                .GetField("_ringsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, ringsNeeded);
            typeof(ZoneControlCaptureTotemSO)
                .GetField("_ringsPerTopple", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, ringsPerTopple);
            typeof(ZoneControlCaptureTotemSO)
                .GetField("_bonusPerTotem", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerTotem);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureTotemController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureTotemController>();
        }

        [Test]
        public void SO_FreshInstance_Rings_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Rings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotemCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotemCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesRings()
        {
            var so = CreateSO(ringsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Rings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_RaisesAtThreshold()
        {
            var so    = CreateSO(ringsNeeded: 3, bonusPerTotem: 490);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(490));
            Assert.That(so.TotemCount,  Is.EqualTo(1));
            Assert.That(so.Rings,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileStacking()
        {
            var so    = CreateSO(ringsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_TopplesRings()
        {
            var so = CreateSO(ringsNeeded: 5, ringsPerTopple: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Rings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(ringsNeeded: 5, ringsPerTopple: 4);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Rings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RingProgress_Clamped()
        {
            var so = CreateSO(ringsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.RingProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnTotemRaised_FiresEvent()
        {
            var so    = CreateSO(ringsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureTotemSO)
                .GetField("_onTotemRaised", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(ringsNeeded: 2, bonusPerTotem: 490);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Rings,             Is.EqualTo(0));
            Assert.That(so.TotemCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleTotems_Accumulate()
        {
            var so = CreateSO(ringsNeeded: 2, bonusPerTotem: 490);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.TotemCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(980));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TotemSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TotemSO, Is.Null);
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
            typeof(ZoneControlCaptureTotemController)
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
