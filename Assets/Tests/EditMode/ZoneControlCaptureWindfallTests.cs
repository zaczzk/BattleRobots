using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T458: <see cref="ZoneControlCaptureWindfallSO"/> and
    /// <see cref="ZoneControlCaptureWindfallController"/>.
    ///
    /// ZoneControlCaptureWindfallTests (12):
    ///   SO_FreshInstance_CurrentStreak_Zero                                        x1
    ///   SO_FreshInstance_WindfallCount_Zero                                        x1
    ///   SO_RecordPlayerCapture_IncrementsStreak                                    x1
    ///   SO_RecordBotCapture_ResetsStreak                                           x1
    ///   SO_RecordPlayerCapture_BelowThreshold_NoWindfall                           x1
    ///   SO_RecordPlayerCapture_ReachesThreshold_TriggersWindfall                   x1
    ///   SO_RecordPlayerCapture_AfterWindfall_ResetsStreak                          x1
    ///   SO_RecordPlayerCapture_AccumulatesTotalBonus                               x1
    ///   SO_Reset_ClearsAll                                                         x1
    ///   Controller_FreshInstance_WindfallSO_Null                                   x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                                  x1
    ///   Controller_Refresh_NullSO_HidesPanel                                       x1
    /// </summary>
    public sealed class ZoneControlCaptureWindfallTests
    {
        private static ZoneControlCaptureWindfallSO CreateSO(int requiredStreak = 3, int bonusPerWindfall = 200)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureWindfallSO>();
            typeof(ZoneControlCaptureWindfallSO)
                .GetField("_requiredStreak", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, requiredStreak);
            typeof(ZoneControlCaptureWindfallSO)
                .GetField("_bonusPerWindfall", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerWindfall);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureWindfallController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureWindfallController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentStreak_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentStreak, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_WindfallCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.WindfallCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IncrementsStreak()
        {
            var so = CreateSO(requiredStreak: 5);
            so.RecordPlayerCapture();
            Assert.That(so.CurrentStreak, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ResetsStreak()
        {
            var so = CreateSO(requiredStreak: 5);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CurrentStreak, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowThreshold_NoWindfall()
        {
            var so = CreateSO(requiredStreak: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.WindfallCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReachesThreshold_TriggersWindfall()
        {
            var so = CreateSO(requiredStreak: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.WindfallCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AfterWindfall_ResetsStreak()
        {
            var so = CreateSO(requiredStreak: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CurrentStreak, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesTotalBonus()
        {
            var so = CreateSO(requiredStreak: 3, bonusPerWindfall: 200);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(200));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(requiredStreak: 3, bonusPerWindfall: 200);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.CurrentStreak,     Is.EqualTo(0));
            Assert.That(so.WindfallCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_WindfallSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.WindfallSO, Is.Null);
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
            typeof(ZoneControlCaptureWindfallController)
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
