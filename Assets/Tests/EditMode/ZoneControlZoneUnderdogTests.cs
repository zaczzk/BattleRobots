using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T444: <see cref="ZoneControlZoneUnderdogSO"/> and
    /// <see cref="ZoneControlZoneUnderdogController"/>.
    ///
    /// ZoneControlZoneUnderdogTests (12):
    ///   SO_FreshInstance_UnderdogCount_Zero                           x1
    ///   SO_FreshInstance_IsUnderdog_False                             x1
    ///   SO_RecordPlayerCapture_WhenBotAhead_CountsUnderdog            x1
    ///   SO_RecordPlayerCapture_WhenEqual_NoUnderdog                   x1
    ///   SO_RecordPlayerCapture_WhenPlayerAhead_NoUnderdog             x1
    ///   SO_RecordBotCapture_IncrementsBotCaptures                     x1
    ///   SO_RecordPlayerCapture_FiresOnUnderdogBonus                   x1
    ///   SO_IsUnderdog_TrueWhenBotAhead                                x1
    ///   SO_Reset_ClearsAll                                            x1
    ///   Controller_FreshInstance_UnderdogSO_Null                     x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                     x1
    ///   Controller_Refresh_NullSO_HidesPanel                         x1
    /// </summary>
    public sealed class ZoneControlZoneUnderdogTests
    {
        private static ZoneControlZoneUnderdogSO CreateSO(int bonus = 125)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlZoneUnderdogSO>();
            typeof(ZoneControlZoneUnderdogSO)
                .GetField("_bonusPerUnderdogCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlZoneUnderdogController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlZoneUnderdogController>();
        }

        [Test]
        public void SO_FreshInstance_UnderdogCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.UnderdogCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsUnderdog_False()
        {
            var so = CreateSO();
            Assert.That(so.IsUnderdog, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_WhenBotAhead_CountsUnderdog()
        {
            var so = CreateSO(bonus: 125);
            so.RecordBotCapture();   // bot=1, player=0
            so.RecordBotCapture();   // bot=2, player=0
            so.RecordPlayerCapture(); // bot was ahead → underdog
            Assert.That(so.UnderdogCount,    Is.EqualTo(1));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(125));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_WhenEqual_NoUnderdog()
        {
            var so = CreateSO();
            so.RecordPlayerCapture(); // player=1, bot=0 → bot not ahead before capture (player was 0, bot 0)
            Assert.That(so.UnderdogCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_WhenPlayerAhead_NoUnderdog()
        {
            var so = CreateSO();
            so.RecordPlayerCapture(); // player=1
            so.RecordPlayerCapture(); // player=2, bot=0 → not underdog
            Assert.That(so.UnderdogCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IncrementsBotCaptures()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.BotCaptures, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FiresOnUnderdogBonus()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneUnderdogSO)
                .GetField("_onUnderdogBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordBotCapture();
            Assert.That(fired, Is.EqualTo(0));
            so.RecordPlayerCapture(); // bot was ahead → fires
            Assert.That(fired, Is.EqualTo(1));

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_IsUnderdog_TrueWhenBotAhead()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            Assert.That(so.IsUnderdog, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(bonus: 125);
            so.RecordBotCapture();
            so.RecordPlayerCapture(); // underdog
            so.Reset();
            Assert.That(so.UnderdogCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.PlayerCaptures,    Is.EqualTo(0));
            Assert.That(so.BotCaptures,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_UnderdogSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.UnderdogSO, Is.Null);
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
            typeof(ZoneControlZoneUnderdogController)
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
