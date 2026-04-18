using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T441: <see cref="ZoneControlCaptureSurgeBonusSO"/> and
    /// <see cref="ZoneControlCaptureSurgeBonusController"/>.
    ///
    /// ZoneControlCaptureSurgeBonusTests (12):
    ///   SO_FreshInstance_SurgeTotal_Zero                            x1
    ///   SO_FreshInstance_TotalBonus_Zero                            x1
    ///   SO_RecordCapture_BelowSurgeTarget_NoSurge                  x1
    ///   SO_RecordCapture_AtSurgeTarget_WithinWindow_FiresSurge      x1
    ///   SO_RecordCapture_AtSurgeTarget_ClearsTimestamps             x1
    ///   SO_RecordCapture_OutsideWindow_DoesNotCountOld              x1
    ///   SO_RecordCapture_MultipleSurges_CountsAll                   x1
    ///   SO_RecordCapture_FiresOnSurgeBonus                          x1
    ///   SO_SurgeProgress_CalculatesCorrectly                        x1
    ///   SO_Reset_ClearsAll                                          x1
    ///   Controller_FreshInstance_SurgeBonusSO_Null                 x1
    ///   Controller_Refresh_NullSO_HidesPanel                       x1
    /// </summary>
    public sealed class ZoneControlCaptureSurgeBonusTests
    {
        private static ZoneControlCaptureSurgeBonusSO CreateSO(int count = 3, float window = 8f, int bonus = 200)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSurgeBonusSO>();
            typeof(ZoneControlCaptureSurgeBonusSO)
                .GetField("_surgeCount", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, count);
            typeof(ZoneControlCaptureSurgeBonusSO)
                .GetField("_surgeWindowSeconds", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, window);
            typeof(ZoneControlCaptureSurgeBonusSO)
                .GetField("_bonusPerSurge", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSurgeBonusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSurgeBonusController>();
        }

        [Test]
        public void SO_FreshInstance_SurgeTotal_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SurgeTotal, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalBonus_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_BelowSurgeTarget_NoSurge()
        {
            var so = CreateSO(count: 3, window: 10f);
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            Assert.That(so.SurgeTotal, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AtSurgeTarget_WithinWindow_FiresSurge()
        {
            var so = CreateSO(count: 3, window: 10f);
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            so.RecordCapture(2f);
            Assert.That(so.SurgeTotal, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AtSurgeTarget_ClearsTimestamps()
        {
            var so = CreateSO(count: 3, window: 10f);
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            so.RecordCapture(2f); // surge complete
            // After surge, progress resets to 0
            Assert.That(so.SurgeProgress, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_OutsideWindow_DoesNotCountOld()
        {
            var so = CreateSO(count: 3, window: 5f);
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            // Next capture at t=10 prunes timestamps at t=0 and t=1 (older than 10-5=5)
            so.RecordCapture(10f);
            Assert.That(so.SurgeTotal, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_MultipleSurges_CountsAll()
        {
            var so = CreateSO(count: 2, window: 10f, bonus: 100);
            so.RecordCapture(0f);
            so.RecordCapture(1f); // surge 1
            so.RecordCapture(2f);
            so.RecordCapture(3f); // surge 2
            Assert.That(so.SurgeTotal,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(200));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FiresOnSurgeBonus()
        {
            var so      = CreateSO(count: 2, window: 10f);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSurgeBonusSO)
                .GetField("_onSurgeBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordCapture(0f);
            Assert.That(fired, Is.EqualTo(0));
            so.RecordCapture(1f);
            Assert.That(fired, Is.EqualTo(1));

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_SurgeProgress_CalculatesCorrectly()
        {
            var so = CreateSO(count: 4, window: 10f);
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            // 2 of 4 captured → 0.5
            Assert.That(so.SurgeProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(count: 2, window: 10f, bonus: 100);
            so.RecordCapture(0f);
            so.RecordCapture(1f); // surge
            so.Reset();
            Assert.That(so.SurgeTotal,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.SurgeProgress,     Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SurgeBonusSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SurgeBonusSO, Is.Null);
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
            typeof(ZoneControlCaptureSurgeBonusController)
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
