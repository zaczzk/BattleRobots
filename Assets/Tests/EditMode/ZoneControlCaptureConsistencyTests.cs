using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T443: <see cref="ZoneControlCaptureConsistencySO"/> and
    /// <see cref="ZoneControlCaptureConsistencyController"/>.
    ///
    /// ZoneControlCaptureConsistencyTests (12):
    ///   SO_FreshInstance_GapCount_Zero                                x1
    ///   SO_FreshInstance_HasFirstCapture_False                        x1
    ///   SO_RecordCapture_FirstCapture_NoGapRecorded                   x1
    ///   SO_RecordCapture_SecondCapture_RecordsGap                     x1
    ///   SO_RecordCapture_BelowThreshold_CountsConsistent              x1
    ///   SO_RecordCapture_AboveThreshold_NoConsistent                  x1
    ///   SO_AverageGap_CalculatesCorrectly                             x1
    ///   SO_RecordCapture_FiresOnConsistentCapture                     x1
    ///   SO_Reset_ClearsAll                                            x1
    ///   Controller_FreshInstance_ConsistencySO_Null                  x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                     x1
    ///   Controller_Refresh_NullSO_HidesPanel                         x1
    /// </summary>
    public sealed class ZoneControlCaptureConsistencyTests
    {
        private static ZoneControlCaptureConsistencySO CreateSO(float threshold = 8f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureConsistencySO>();
            typeof(ZoneControlCaptureConsistencySO)
                .GetField("_consistencyThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, threshold);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureConsistencyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureConsistencyController>();
        }

        [Test]
        public void SO_FreshInstance_GapCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.GapCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_HasFirstCapture_False()
        {
            var so = CreateSO();
            Assert.That(so.HasFirstCapture, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FirstCapture_NoGapRecorded()
        {
            var so = CreateSO(threshold: 8f);
            so.RecordCapture(5f);
            Assert.That(so.GapCount,         Is.EqualTo(0));
            Assert.That(so.HasFirstCapture,  Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_SecondCapture_RecordsGap()
        {
            var so = CreateSO(threshold: 8f);
            so.RecordCapture(0f);
            so.RecordCapture(6f);
            Assert.That(so.GapCount,     Is.EqualTo(1));
            Assert.That(so.TotalGapTime, Is.EqualTo(6f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_BelowThreshold_CountsConsistent()
        {
            var so = CreateSO(threshold: 8f);
            so.RecordCapture(0f);
            so.RecordCapture(5f); // gap = 5 < 8
            Assert.That(so.ConsistentCaptures, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AboveThreshold_NoConsistent()
        {
            var so = CreateSO(threshold: 8f);
            so.RecordCapture(0f);
            so.RecordCapture(10f); // gap = 10 >= 8
            Assert.That(so.ConsistentCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AverageGap_CalculatesCorrectly()
        {
            var so = CreateSO(threshold: 20f);
            so.RecordCapture(0f);
            so.RecordCapture(4f);  // gap 4
            so.RecordCapture(10f); // gap 6 → average = (4+6)/2 = 5
            Assert.That(so.AverageGap, Is.EqualTo(5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FiresOnConsistentCapture()
        {
            var so      = CreateSO(threshold: 8f);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureConsistencySO)
                .GetField("_onConsistentCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordCapture(0f);
            Assert.That(fired, Is.EqualTo(0));
            so.RecordCapture(3f); // gap 3 < 8
            Assert.That(fired, Is.EqualTo(1));

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(threshold: 20f);
            so.RecordCapture(0f);
            so.RecordCapture(5f);
            so.Reset();
            Assert.That(so.GapCount,           Is.EqualTo(0));
            Assert.That(so.TotalGapTime,       Is.EqualTo(0f).Within(0.001f));
            Assert.That(so.ConsistentCaptures, Is.EqualTo(0));
            Assert.That(so.HasFirstCapture,    Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ConsistencySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ConsistencySO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureConsistencyController)
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
