using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T430: <see cref="ZoneControlCaptureBalanceSO"/> and
    /// <see cref="ZoneControlCaptureBalanceController"/>.
    ///
    /// ZoneControlCaptureBalanceTests (12):
    ///   SO_FreshInstance_Balance_Zero                              x1
    ///   SO_FreshInstance_PlayerCaptures_Zero                       x1
    ///   SO_RecordPlayerCapture_IncrementsBalance                   x1
    ///   SO_RecordBotRecapture_DecrementsBalance                    x1
    ///   SO_RecordPlayerCapture_FiresBalancePositiveEvent           x1
    ///   SO_RecordBotRecapture_FiresBalanceNegativeEvent            x1
    ///   SO_EvaluateBalance_Idempotent_NoDoublefire                 x1
    ///   SO_Reset_ClearsAll                                         x1
    ///   SO_IsPositive_TrueWhenPlayerLeads                          x1
    ///   SO_Balance_NegativeWhenBotLeads                            x1
    ///   Controller_FreshInstance_BalanceSO_Null                    x1
    ///   Controller_Refresh_NullSO_HidesPanel                      x1
    /// </summary>
    public sealed class ZoneControlCaptureBalanceTests
    {
        private static ZoneControlCaptureBalanceSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCaptureBalanceSO>();

        private static ZoneControlCaptureBalanceController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureBalanceController>();
        }

        [Test]
        public void SO_FreshInstance_Balance_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Balance, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_PlayerCaptures_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PlayerCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IncrementsBalance()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            Assert.That(so.Balance,         Is.EqualTo(1));
            Assert.That(so.PlayerCaptures,  Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotRecapture_DecrementsBalance()
        {
            var so = CreateSO();
            so.RecordBotRecapture();
            Assert.That(so.Balance,        Is.EqualTo(-1));
            Assert.That(so.BotRecaptures,  Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FiresBalancePositiveEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureBalanceSO)
                .GetField("_onBalancePositive", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordBotRecapture_FiresBalanceNegativeEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureBalanceSO)
                .GetField("_onBalanceNegative", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            // First bot recapture → balance = -1 (negative transition from 0)
            so.RecordBotRecapture();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_EvaluateBalance_Idempotent_NoDoublefire()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureBalanceSO)
                .GetField("_onBalancePositive", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecordPlayerCapture(); // transition → fires
            so.RecordPlayerCapture(); // stays positive → no fire
            so.RecordPlayerCapture(); // stays positive → no fire

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotRecapture();
            so.Reset();
            Assert.That(so.Balance,        Is.EqualTo(0));
            Assert.That(so.PlayerCaptures, Is.EqualTo(0));
            Assert.That(so.BotRecaptures,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_IsPositive_TrueWhenPlayerLeads()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            Assert.That(so.IsPositive, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Balance_NegativeWhenBotLeads()
        {
            var so = CreateSO();
            so.RecordBotRecapture();
            so.RecordBotRecapture();
            Assert.That(so.Balance, Is.LessThan(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_BalanceSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BalanceSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureBalanceController)
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
