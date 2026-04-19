using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T460: <see cref="ZoneControlEarlyBirdBonusSO"/> and
    /// <see cref="ZoneControlEarlyBirdBonusController"/>.
    ///
    /// ZoneControlEarlyBirdBonusTests (12):
    ///   SO_FreshInstance_PlayerEarlyCaptures_Zero                                  x1
    ///   SO_FreshInstance_EarlyComplete_False                                       x1
    ///   SO_RecordPlayerCapture_IncrementsCount                                     x1
    ///   SO_RecordBotCapture_SetsBotInterrupted                                     x1
    ///   SO_RecordPlayerCapture_BotAlreadyInterrupted_NoOp                          x1
    ///   SO_RecordBotCapture_AlreadyComplete_NoOp                                   x1
    ///   SO_RecordPlayerCapture_ReachesRequired_SetsEarlyComplete                   x1
    ///   SO_BotCaptureThenPlayerCapture_NoCompletion                                x1
    ///   SO_Reset_ClearsAll                                                         x1
    ///   Controller_FreshInstance_EarlyBirdSO_Null                                  x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                                  x1
    ///   Controller_Refresh_NullSO_HidesPanel                                       x1
    /// </summary>
    public sealed class ZoneControlEarlyBirdBonusTests
    {
        private static ZoneControlEarlyBirdBonusSO CreateSO(int required = 5, int bonus = 300)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlEarlyBirdBonusSO>();
            typeof(ZoneControlEarlyBirdBonusSO)
                .GetField("_requiredEarlyCaptures", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, required);
            typeof(ZoneControlEarlyBirdBonusSO)
                .GetField("_bonusOnEarlyDominance", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlEarlyBirdBonusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlEarlyBirdBonusController>();
        }

        [Test]
        public void SO_FreshInstance_PlayerEarlyCaptures_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PlayerEarlyCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_EarlyComplete_False()
        {
            var so = CreateSO();
            Assert.That(so.EarlyComplete, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IncrementsCount()
        {
            var so = CreateSO(required: 5);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.PlayerEarlyCaptures, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_SetsBotInterrupted()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            Assert.That(so.BotInterrupted, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BotAlreadyInterrupted_NoOp()
        {
            var so = CreateSO(required: 5);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.PlayerEarlyCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_AlreadyComplete_NoOp()
        {
            var so = CreateSO(required: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.BotInterrupted, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReachesRequired_SetsEarlyComplete()
        {
            var so = CreateSO(required: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.EarlyComplete, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BotCaptureThenPlayerCapture_NoCompletion()
        {
            var so = CreateSO(required: 3);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.EarlyComplete, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(required: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.PlayerEarlyCaptures, Is.EqualTo(0));
            Assert.That(so.EarlyComplete,       Is.False);
            Assert.That(so.BotInterrupted,      Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_EarlyBirdSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.EarlyBirdSO, Is.Null);
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
            typeof(ZoneControlEarlyBirdBonusController)
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
