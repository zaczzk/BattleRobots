using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T420: <see cref="ZoneControlCaptureAccuracySO"/> and
    /// <see cref="ZoneControlCaptureAccuracyController"/>.
    ///
    /// ZoneControlCaptureAccuracyTests (12):
    ///   SO_FreshInstance_Attempts_Zero                          x1
    ///   SO_FreshInstance_Successes_Zero                         x1
    ///   SO_FreshInstance_Accuracy_Zero                          x1
    ///   SO_RecordAttempt_IncrementsAttempts                     x1
    ///   SO_RecordSuccess_IncrementsSuccesses                     x1
    ///   SO_Accuracy_PerfectAccuracy_ReturnsOne                  x1
    ///   SO_Accuracy_PartialAccuracy_ComputesRatio               x1
    ///   SO_RecordSuccess_FiresEvent                             x1
    ///   SO_Reset_ClearsAll                                      x1
    ///   SO_Accuracy_NoAttempts_ReturnsZero                      x1
    ///   Controller_FreshInstance_AccuracySO_Null                x1
    ///   Controller_Refresh_NullSO_HidesPanel                    x1
    /// </summary>
    public sealed class ZoneControlCaptureAccuracyTests
    {
        private static ZoneControlCaptureAccuracySO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCaptureAccuracySO>();

        private static ZoneControlCaptureAccuracyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureAccuracyController>();
        }

        [Test]
        public void SO_FreshInstance_Attempts_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalAttempts, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_Successes_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalSuccesses, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_Accuracy_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Accuracy, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordAttempt_IncrementsAttempts()
        {
            var so = CreateSO();
            so.RecordAttempt();
            so.RecordAttempt();
            Assert.That(so.TotalAttempts, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordSuccess_IncrementsSuccesses()
        {
            var so = CreateSO();
            so.RecordAttempt();
            so.RecordSuccess();
            Assert.That(so.TotalSuccesses, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Accuracy_PerfectAccuracy_ReturnsOne()
        {
            var so = CreateSO();
            so.RecordAttempt();
            so.RecordSuccess();
            Assert.That(so.Accuracy, Is.EqualTo(1f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Accuracy_PartialAccuracy_ComputesRatio()
        {
            var so = CreateSO();
            so.RecordAttempt();
            so.RecordAttempt();
            so.RecordAttempt();
            so.RecordAttempt();
            so.RecordSuccess();
            so.RecordSuccess();
            // 2 successes / 4 attempts = 0.5
            Assert.That(so.Accuracy, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordSuccess_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureAccuracySO)
                .GetField("_onAccuracyChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecordAttempt();
            so.RecordSuccess();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordAttempt();
            so.RecordSuccess();
            so.Reset();
            Assert.That(so.TotalAttempts,  Is.EqualTo(0));
            Assert.That(so.TotalSuccesses, Is.EqualTo(0));
            Assert.That(so.Accuracy,       Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Accuracy_NoAttempts_ReturnsZero()
        {
            var so = CreateSO();
            so.RecordSuccess();
            // Success without attempt — accuracy still 0 due to no attempts
            Assert.That(so.TotalAttempts, Is.EqualTo(0));
            // Accuracy formula: attempts==0 → 0
            Assert.That(so.Accuracy, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_AccuracySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.AccuracySO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureAccuracyController)
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
