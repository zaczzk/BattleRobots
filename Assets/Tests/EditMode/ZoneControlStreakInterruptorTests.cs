using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T454: <see cref="ZoneControlStreakInterruptorSO"/> and
    /// <see cref="ZoneControlStreakInterruptorController"/>.
    ///
    /// ZoneControlStreakInterruptorTests (12):
    ///   SO_FreshInstance_InterruptCount_Zero                                   x1
    ///   SO_FreshInstance_BotStreak_Zero                                        x1
    ///   SO_RecordBotCapture_IncrementsBotStreak                                x1
    ///   SO_RecordPlayerCapture_BelowThreshold_NoInterrupt                      x1
    ///   SO_RecordPlayerCapture_AtThreshold_CountsInterrupt                     x1
    ///   SO_RecordPlayerCapture_ResetsBotStreak                                 x1
    ///   SO_RecordPlayerCapture_AwardsTotalBonus                                x1
    ///   SO_Reset_ClearsAll                                                     x1
    ///   Controller_FreshInstance_InterruptorSO_Null                            x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                              x1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                             x1
    ///   Controller_Refresh_NullSO_HidesPanel                                   x1
    /// </summary>
    public sealed class ZoneControlStreakInterruptorTests
    {
        private static ZoneControlStreakInterruptorSO CreateSO(
            int interruptThreshold = 2,
            int bonusPerInterrupt  = 100)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlStreakInterruptorSO>();
            typeof(ZoneControlStreakInterruptorSO)
                .GetField("_interruptThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, interruptThreshold);
            typeof(ZoneControlStreakInterruptorSO)
                .GetField("_bonusPerInterrupt", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerInterrupt);
            so.Reset();
            return so;
        }

        private static ZoneControlStreakInterruptorController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlStreakInterruptorController>();
        }

        [Test]
        public void SO_FreshInstance_InterruptCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.InterruptCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BotStreak_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BotStreak, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IncrementsBotStreak()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.BotStreak, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowThreshold_NoInterrupt()
        {
            var so = CreateSO(interruptThreshold: 3);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.InterruptCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_CountsInterrupt()
        {
            var so = CreateSO(interruptThreshold: 2);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.InterruptCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ResetsBotStreak()
        {
            var so = CreateSO(interruptThreshold: 2);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.BotStreak, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AwardsTotalBonus()
        {
            var so = CreateSO(interruptThreshold: 2, bonusPerInterrupt: 100);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(interruptThreshold: 2, bonusPerInterrupt: 100);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.BotStreak,        Is.EqualTo(0));
            Assert.That(so.InterruptCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_InterruptorSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.InterruptorSO, Is.Null);
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
            typeof(ZoneControlStreakInterruptorController)
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
