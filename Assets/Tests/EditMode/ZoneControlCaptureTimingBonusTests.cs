using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureTimingBonusTests
    {
        private static ZoneControlCaptureTimingBonusSO CreateSO(
            float targetGap = 5f, float tolerance = 1f, int bonus = 100)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureTimingBonusSO>();
            typeof(ZoneControlCaptureTimingBonusSO)
                .GetField("_targetGap",      BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, targetGap);
            typeof(ZoneControlCaptureTimingBonusSO)
                .GetField("_tolerance",      BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, tolerance);
            typeof(ZoneControlCaptureTimingBonusSO)
                .GetField("_bonusPerOnTime", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureTimingBonusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureTimingBonusController>();
        }

        [Test]
        public void SO_FreshInstance_OnTimeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.OnTimeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FirstCapture_SetsFirst_NoBonus()
        {
            var so = CreateSO();
            so.RecordCapture(0f);
            Assert.That(so.OnTimeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SecondCapture_OnTime_IncrementsCount()
        {
            var so = CreateSO(targetGap: 5f, tolerance: 1f);
            so.RecordCapture(0f);
            so.RecordCapture(5f);
            Assert.That(so.OnTimeCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SecondCapture_WithinTolerance_IncrementsCount()
        {
            var so = CreateSO(targetGap: 5f, tolerance: 1f);
            so.RecordCapture(0f);
            so.RecordCapture(5.8f);
            Assert.That(so.OnTimeCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SecondCapture_OutsideTolerance_NoBonus()
        {
            var so = CreateSO(targetGap: 5f, tolerance: 1f);
            so.RecordCapture(0f);
            so.RecordCapture(8f);
            Assert.That(so.OnTimeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleOnTime_AccumulatesBonus()
        {
            var so = CreateSO(targetGap: 5f, tolerance: 1f, bonus: 100);
            so.RecordCapture(0f);
            so.RecordCapture(5f);
            so.RecordCapture(10f);
            Assert.That(so.OnTimeCount,      Is.EqualTo(2));
            Assert.That(so.TotalTimingBonus, Is.EqualTo(200));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsState()
        {
            var so = CreateSO();
            so.RecordCapture(0f);
            so.RecordCapture(5f);
            so.Reset();
            Assert.That(so.OnTimeCount,      Is.EqualTo(0));
            Assert.That(so.TotalTimingBonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AfterReset_NewFirstCapture_NoBonus()
        {
            var so = CreateSO(targetGap: 5f, tolerance: 1f);
            so.RecordCapture(0f);
            so.RecordCapture(5f);
            so.Reset();
            so.RecordCapture(100f);
            Assert.That(so.OnTimeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EarlyCapture_BelowTolerance_NoBonus()
        {
            var so = CreateSO(targetGap: 5f, tolerance: 1f);
            so.RecordCapture(0f);
            so.RecordCapture(3.9f);
            Assert.That(so.OnTimeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TimingBonusSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TimingBonusSO, Is.Null);
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
            typeof(ZoneControlCaptureTimingBonusController)
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
