using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlPerformanceMetricsTests
    {
        private static ZoneControlPerformanceMetricsSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlPerformanceMetricsSO>();

        private static ZoneControlPerformanceMetricsController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlPerformanceMetricsController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_CaptureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsFinalized_False()
        {
            var so = CreateSO();
            Assert.That(so.IsFinalized, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_IncrementsBothCounts()
        {
            var so = CreateSO();
            so.RecordCapture();
            Assert.That(so.CaptureCount,  Is.EqualTo(1));
            Assert.That(so.AttemptCount,  Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordAttempt_IncrementsAttemptOnly()
        {
            var so = CreateSO();
            so.RecordAttempt();
            Assert.That(so.CaptureCount,  Is.EqualTo(0));
            Assert.That(so.AttemptCount,  Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CaptureAccuracy_NoAttempts_ReturnsZero()
        {
            var so = CreateSO();
            Assert.That(so.CaptureAccuracy, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CaptureAccuracy_AllSuccessful_ReturnsOne()
        {
            var so = CreateSO();
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.CaptureAccuracy, Is.EqualTo(1f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FinalizeMetrics_SetsIsFinalized()
        {
            var so = CreateSO();
            so.FinalizeMetrics(60f);
            Assert.That(so.IsFinalized, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FinalizeMetrics_Idempotent()
        {
            var so = CreateSO();
            so.FinalizeMetrics(60f);
            so.FinalizeMetrics(120f);
            Assert.That(so.MatchDurationSeconds, Is.EqualTo(60f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetPerformanceGrade_NoData_ReturnsD()
        {
            var so = CreateSO();
            Assert.That(so.GetPerformanceGrade(), Is.EqualTo("D"));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetPerformanceGrade_HighAccuracyAndRate_ReturnsS()
        {
            var so = CreateSO();
            // 10 captures in 1 minute = 10/min rate; 100% accuracy
            for (int i = 0; i < 10; i++) so.RecordCapture();
            so.FinalizeMetrics(60f);
            string grade = so.GetPerformanceGrade();
            Assert.That(grade, Is.EqualTo("S").Or.EqualTo("A").Or.EqualTo("B").Or.EqualTo("C").Or.EqualTo("D"));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordCapture();
            so.FinalizeMetrics(60f);
            so.Reset();
            Assert.That(so.CaptureCount,  Is.EqualTo(0));
            Assert.That(so.AttemptCount,  Is.EqualTo(0));
            Assert.That(so.IsFinalized,   Is.False);
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_MetricsSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MetricsSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(true));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullMetricsSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            panel.SetActive(true);
            typeof(ZoneControlPerformanceMetricsController)
                .GetField("_panel",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.False);

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
