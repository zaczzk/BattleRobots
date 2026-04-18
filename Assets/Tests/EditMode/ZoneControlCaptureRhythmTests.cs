using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T455: <see cref="ZoneControlCaptureRhythmSO"/> and
    /// <see cref="ZoneControlCaptureRhythmController"/>.
    ///
    /// ZoneControlCaptureRhythmTests (12):
    ///   SO_FreshInstance_RhythmStreak_Zero                                     x1
    ///   SO_FreshInstance_HasFirstCapture_False                                 x1
    ///   SO_RecordCapture_FirstCapture_SetsHasFirstCapture                      x1
    ///   SO_RecordCapture_WithinWindow_IncrementsStreak                         x1
    ///   SO_RecordCapture_OutsideWindow_ResetsStreak                            x1
    ///   SO_RecordCapture_AtTarget_FiresRhythm                                  x1
    ///   SO_RecordCapture_AtTarget_AccumulatesBonus                             x1
    ///   SO_RecordCapture_AtTarget_ResetsStreak                                 x1
    ///   SO_Reset_ClearsAll                                                     x1
    ///   Controller_FreshInstance_RhythmSO_Null                                x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                             x1
    ///   Controller_Refresh_NullSO_HidesPanel                                  x1
    /// </summary>
    public sealed class ZoneControlCaptureRhythmTests
    {
        private static ZoneControlCaptureRhythmSO CreateSO(
            float rhythmWindowSeconds  = 12f,
            int   rhythmStreakTarget   = 3,
            int   bonusPerRhythmStreak = 120)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureRhythmSO>();
            typeof(ZoneControlCaptureRhythmSO)
                .GetField("_rhythmWindowSeconds", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, rhythmWindowSeconds);
            typeof(ZoneControlCaptureRhythmSO)
                .GetField("_rhythmStreakTarget", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, rhythmStreakTarget);
            typeof(ZoneControlCaptureRhythmSO)
                .GetField("_bonusPerRhythmStreak", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerRhythmStreak);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureRhythmController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureRhythmController>();
        }

        [Test]
        public void SO_FreshInstance_RhythmStreak_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RhythmStreak, Is.EqualTo(0));
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
        public void SO_RecordCapture_FirstCapture_SetsHasFirstCapture()
        {
            var so = CreateSO();
            so.RecordCapture(0f);
            Assert.That(so.HasFirstCapture, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_WithinWindow_IncrementsStreak()
        {
            var so = CreateSO(rhythmWindowSeconds: 10f, rhythmStreakTarget: 5);
            so.RecordCapture(0f);
            so.RecordCapture(5f);
            Assert.That(so.RhythmStreak, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_OutsideWindow_ResetsStreak()
        {
            var so = CreateSO(rhythmWindowSeconds: 5f, rhythmStreakTarget: 5);
            so.RecordCapture(0f);
            so.RecordCapture(10f);
            Assert.That(so.RhythmStreak, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AtTarget_FiresRhythm()
        {
            var so = CreateSO(rhythmWindowSeconds: 10f, rhythmStreakTarget: 3, bonusPerRhythmStreak: 120);
            so.RecordCapture(0f);
            so.RecordCapture(3f);
            so.RecordCapture(6f);
            so.RecordCapture(9f);
            Assert.That(so.RhythmCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AtTarget_AccumulatesBonus()
        {
            var so = CreateSO(rhythmWindowSeconds: 10f, rhythmStreakTarget: 3, bonusPerRhythmStreak: 120);
            so.RecordCapture(0f);
            so.RecordCapture(3f);
            so.RecordCapture(6f);
            so.RecordCapture(9f);
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(120));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AtTarget_ResetsStreak()
        {
            var so = CreateSO(rhythmWindowSeconds: 10f, rhythmStreakTarget: 3);
            so.RecordCapture(0f);
            so.RecordCapture(3f);
            so.RecordCapture(6f);
            so.RecordCapture(9f);
            Assert.That(so.RhythmStreak, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(rhythmWindowSeconds: 10f, rhythmStreakTarget: 3);
            so.RecordCapture(0f);
            so.RecordCapture(3f);
            so.RecordCapture(6f);
            so.RecordCapture(9f);
            so.Reset();
            Assert.That(so.RhythmStreak,      Is.EqualTo(0));
            Assert.That(so.RhythmCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.HasFirstCapture,   Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_RhythmSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RhythmSO, Is.Null);
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
            typeof(ZoneControlCaptureRhythmController)
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
