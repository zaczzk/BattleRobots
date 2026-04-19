using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureOverflowTests
    {
        private static ZoneControlCaptureOverflowSO CreateSO(int target = 10, int bonus = 30)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureOverflowSO>();
            typeof(ZoneControlCaptureOverflowSO)
                .GetField("_overflowTarget", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, target);
            typeof(ZoneControlCaptureOverflowSO)
                .GetField("_bonusPerOverflow", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureOverflowController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureOverflowController>();
        }

        [Test]
        public void SO_FreshInstance_OverflowCaptures_Zero()
        {
            var so = CreateSO();
            Assert.That(so.OverflowCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BelowTarget_NoOverflow()
        {
            var so = CreateSO(target: 5);
            for (int i = 0; i < 5; i++) so.RecordCapture();
            Assert.That(so.OverflowCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AtTarget_NoOverflow()
        {
            var so = CreateSO(target: 5);
            for (int i = 0; i < 5; i++) so.RecordCapture();
            Assert.That(so.HasOverflowed, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AboveTarget_OverflowCaptureIncremented()
        {
            var so = CreateSO(target: 3);
            for (int i = 0; i < 4; i++) so.RecordCapture();
            Assert.That(so.OverflowCaptures, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Overflow_ReturnsBonusPerCapture()
        {
            var so = CreateSO(target: 1, bonus: 50);
            so.RecordCapture();
            int result = so.RecordCapture();
            Assert.That(result, Is.EqualTo(50));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Overflow_AccumulatesTotal()
        {
            var so = CreateSO(target: 2, bonus: 40);
            for (int i = 0; i < 4; i++) so.RecordCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(80));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Overflow_FiresEvent_OnFirstOverflow()
        {
            var so    = CreateSO(target: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureOverflowSO)
                .GetField("_onOverflow", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            for (int i = 0; i < 3; i++) so.RecordCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Overflow_HasOverflowed_True()
        {
            var so = CreateSO(target: 1);
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.HasOverflowed, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(target: 1);
            so.RecordCapture();
            so.RecordCapture();
            so.Reset();
            Assert.That(so.TotalCaptures,    Is.EqualTo(0));
            Assert.That(so.OverflowCaptures, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.HasOverflowed,    Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_OverflowSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.OverflowSO, Is.Null);
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
            typeof(ZoneControlCaptureOverflowController)
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
