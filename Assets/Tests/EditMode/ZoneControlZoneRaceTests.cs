using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T379: <see cref="ZoneControlZoneRaceSO"/> and
    /// <see cref="ZoneControlZoneRaceController"/>.
    ///
    /// ZoneControlZoneRaceTests (12):
    ///   SO_FreshInstance_IsRaceComplete_False                    ×1
    ///   SO_FreshInstance_PlayerCaptures_Zero                     ×1
    ///   SO_AddPlayerCapture_IncrementsCount                      ×1
    ///   SO_AddPlayerCapture_AtTarget_PlayerWins                  ×1
    ///   SO_AddPlayerCapture_FiresOnPlayerWon                     ×1
    ///   SO_AddBotCapture_AtTarget_BotWins                        ×1
    ///   SO_AddPlayerCapture_Idempotent_WhenComplete              ×1
    ///   SO_Reset_ClearsAll                                       ×1
    ///   Controller_FreshInstance_RaceSO_Null                     ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow               ×1
    ///   Controller_Refresh_NullRaceSO_HidesPanel                 ×1
    /// </summary>
    public sealed class ZoneControlZoneRaceTests
    {
        private static ZoneControlZoneRaceSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlZoneRaceSO>();

        private static ZoneControlZoneRaceController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlZoneRaceController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsRaceComplete_False()
        {
            var so = CreateSO();
            Assert.That(so.IsRaceComplete, Is.False);
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
        public void SO_AddPlayerCapture_IncrementsCount()
        {
            var so = CreateSO();
            so.AddPlayerCapture();
            Assert.That(so.PlayerCaptures, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddPlayerCapture_AtTarget_PlayerWins()
        {
            var so = CreateSO();
            for (int i = 0; i < so.CaptureTarget; i++)
                so.AddPlayerCapture();

            Assert.That(so.IsRaceComplete, Is.True);
            Assert.That(so.PlayerWon,      Is.True);
            Assert.That(so.BotWon,         Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddPlayerCapture_FiresOnPlayerWon()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneRaceSO)
                .GetField("_onPlayerWon", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            for (int i = 0; i < so.CaptureTarget; i++)
                so.AddPlayerCapture();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_AddBotCapture_AtTarget_BotWins()
        {
            var so = CreateSO();
            for (int i = 0; i < so.CaptureTarget; i++)
                so.AddBotCapture();

            Assert.That(so.IsRaceComplete, Is.True);
            Assert.That(so.BotWon,         Is.True);
            Assert.That(so.PlayerWon,      Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddPlayerCapture_Idempotent_WhenComplete()
        {
            var so = CreateSO();
            for (int i = 0; i < so.CaptureTarget; i++)
                so.AddPlayerCapture();

            int countAfterWin = so.PlayerCaptures;
            so.AddPlayerCapture();
            Assert.That(so.PlayerCaptures, Is.EqualTo(countAfterWin));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            for (int i = 0; i < so.CaptureTarget; i++)
                so.AddPlayerCapture();

            so.Reset();

            Assert.That(so.IsRaceComplete, Is.False);
            Assert.That(so.PlayerCaptures, Is.EqualTo(0));
            Assert.That(so.BotCaptures,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_RaceSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RaceSO, Is.Null);
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
            ctrl.gameObject.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullRaceSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlZoneRaceController)
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
