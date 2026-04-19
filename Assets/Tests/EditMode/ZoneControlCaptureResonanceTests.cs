using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T472: <see cref="ZoneControlCaptureResonanceSO"/> and
    /// <see cref="ZoneControlCaptureResonanceController"/>.
    ///
    /// ZoneControlCaptureResonanceTests (12):
    ///   SO_FreshInstance_ResonanceCount_Zero                                x1
    ///   SO_FreshInstance_ResonanceProgress_Zero                             x1
    ///   SO_RecordCapture_AccumulatesWithinWindow                            x1
    ///   SO_RecordCapture_FiresResonanceOnTarget                             x1
    ///   SO_RecordCapture_ClearsTimestampsAfterResonance                     x1
    ///   SO_RecordCapture_PrunesOldTimestamps                                x1
    ///   SO_ResonanceProgress_FillsTowardTarget                              x1
    ///   SO_TotalBonusAwarded_AccumulatesCorrectly                           x1
    ///   SO_Reset_ClearsState                                                x1
    ///   Controller_FreshInstance_ResonanceSO_Null                          x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                           x1
    ///   Controller_Refresh_NullSO_HidesPanel                                x1
    /// </summary>
    public sealed class ZoneControlCaptureResonanceTests
    {
        private static ZoneControlCaptureResonanceSO CreateSO(
            float window = 4f, int target = 4, int bonus = 175)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureResonanceSO>();
            var t  = typeof(ZoneControlCaptureResonanceSO);
            t.GetField("_resonanceWindow",   BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, window);
            t.GetField("_resonanceTarget",   BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, target);
            t.GetField("_bonusPerResonance", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureResonanceController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureResonanceController>();
        }

        [Test]
        public void SO_FreshInstance_ResonanceCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ResonanceCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ResonanceProgress_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ResonanceProgress, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AccumulatesWithinWindow()
        {
            var so = CreateSO(window: 4f, target: 4);
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            so.RecordCapture(2f);
            Assert.That(so.CaptureCount, Is.EqualTo(3));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FiresResonanceOnTarget()
        {
            var so        = CreateSO(window: 10f, target: 3);
            int fireCount = 0;
            var evt       = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureResonanceSO)
                .GetField("_onResonance", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fireCount++);
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            so.RecordCapture(2f);
            Assert.That(fireCount, Is.EqualTo(1));
            Assert.That(so.ResonanceCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordCapture_ClearsTimestampsAfterResonance()
        {
            var so = CreateSO(window: 10f, target: 3);
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            so.RecordCapture(2f);
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_PrunesOldTimestamps()
        {
            var so = CreateSO(window: 3f, target: 4);
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            so.RecordCapture(5f);
            Assert.That(so.CaptureCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ResonanceProgress_FillsTowardTarget()
        {
            var so = CreateSO(window: 10f, target: 4);
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            Assert.That(so.ResonanceProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TotalBonusAwarded_AccumulatesCorrectly()
        {
            var so = CreateSO(window: 10f, target: 2, bonus: 100);
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            so.RecordCapture(2f);
            so.RecordCapture(3f);
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(200));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsState()
        {
            var so = CreateSO(window: 10f, target: 3);
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            so.RecordCapture(2f);
            so.Reset();
            Assert.That(so.ResonanceCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.CaptureCount,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ResonanceSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ResonanceSO, Is.Null);
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
            typeof(ZoneControlCaptureResonanceController)
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
