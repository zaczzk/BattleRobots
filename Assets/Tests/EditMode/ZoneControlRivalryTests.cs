using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for <see cref="ZoneControlRivalrySO"/> and
    /// <see cref="ZoneControlRivalryController"/>.
    /// </summary>
    public sealed class ZoneControlRivalryTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static ZoneControlRivalrySO CreateRivalrySO() =>
            ScriptableObject.CreateInstance<ZoneControlRivalrySO>();

        private static ZoneControlRivalryController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlRivalryController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_PlayerWins_Zero()
        {
            var so = CreateRivalrySO();
            Assert.That(so.PlayerWins, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BotWins_Zero()
        {
            var so = CreateRivalrySO();
            Assert.That(so.BotWins, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordResult_PlayerWin_IncrementsPlayerWins()
        {
            var so = CreateRivalrySO();
            so.RecordResult(true);
            Assert.That(so.PlayerWins, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordResult_BotWin_IncrementsBotWins()
        {
            var so = CreateRivalrySO();
            so.RecordResult(false);
            Assert.That(so.BotWins, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_WinsDelta_PlayerLeading_IsPositive()
        {
            var so = CreateRivalrySO();
            so.RecordResult(true);
            so.RecordResult(true);
            so.RecordResult(false);
            Assert.That(so.WinsDelta, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RivalryDescription_PlayerLeading()
        {
            var so = CreateRivalrySO();
            so.RecordResult(true);
            so.RecordResult(true);
            so.RecordResult(false);
            Assert.That(so.RivalryDescription(), Does.StartWith("You lead"));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RivalryDescription_Tied()
        {
            var so = CreateRivalrySO();
            so.RecordResult(true);
            so.RecordResult(false);
            Assert.That(so.RivalryDescription(), Is.EqualTo("Tied"));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RivalryDescription_BotLeading()
        {
            var so = CreateRivalrySO();
            so.RecordResult(false);
            so.RecordResult(false);
            so.RecordResult(true);
            Assert.That(so.RivalryDescription(), Does.StartWith("Bot leads"));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateRivalrySO();
            so.RecordResult(true);
            so.RecordResult(false);
            so.Reset();
            Assert.That(so.PlayerWins, Is.EqualTo(0));
            Assert.That(so.BotWins,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_RivalrySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RivalrySO, Is.Null);
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
        public void Controller_Refresh_NullRivalrySO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            panel.SetActive(true);
            typeof(ZoneControlRivalryController)
                .GetField("_panel",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.False);

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
