using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T398: <see cref="ZoneControlCaptureStreakBonusSO"/> and
    /// <see cref="ZoneControlCaptureStreakBonusController"/>.
    ///
    /// ZoneControlCaptureStreakBonusTests (12):
    ///   SO_FreshInstance_CurrentStreak_Zero                          x1
    ///   SO_FreshInstance_TotalBonusAwarded_Zero                      x1
    ///   SO_RecordCapture_IncrementsStreak                            x1
    ///   SO_RecordCapture_ClampedAtMaxStreak                          x1
    ///   SO_RecordCapture_FiresOnStreakBonus                          x1
    ///   SO_RecordCapture_ReturnsEscalatingBonus                      x1
    ///   SO_BreakStreak_ResetsStreakAndLastBonus                      x1
    ///   SO_Reset_ClearsAll                                           x1
    ///   Controller_FreshInstance_StreakBonusSO_Null                  x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                    x1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                   x1
    ///   Controller_Refresh_NullSO_HidesPanel                         x1
    /// </summary>
    public sealed class ZoneControlCaptureStreakBonusTests
    {
        private static ZoneControlCaptureStreakBonusSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCaptureStreakBonusSO>();

        private static ZoneControlCaptureStreakBonusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureStreakBonusController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_CurrentStreak_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentStreak, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalBonusAwarded_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_IncrementsStreak()
        {
            var so = CreateSO();
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.CurrentStreak, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_ClampedAtMaxStreak()
        {
            var so = CreateSO();
            for (int i = 0; i < so.MaxStreak + 5; i++)
                so.RecordCapture();
            Assert.That(so.CurrentStreak, Is.EqualTo(so.MaxStreak));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FiresOnStreakBonus()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureStreakBonusSO)
                .GetField("_onStreakBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecordCapture();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordCapture_ReturnsEscalatingBonus()
        {
            var so    = CreateSO();
            int first = so.RecordCapture();
            int second = so.RecordCapture();
            Assert.That(second, Is.GreaterThanOrEqualTo(first));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BreakStreak_ResetsStreakAndLastBonus()
        {
            var so = CreateSO();
            so.RecordCapture();
            so.RecordCapture();
            so.BreakStreak();
            Assert.That(so.CurrentStreak,    Is.EqualTo(0));
            Assert.That(so.LastBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordCapture();
            so.RecordCapture();
            so.Reset();
            Assert.That(so.CurrentStreak,     Is.EqualTo(0));
            Assert.That(so.LastBonusAwarded,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_StreakBonusSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.StreakBonusSO, Is.Null);
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
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureStreakBonusController)
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
