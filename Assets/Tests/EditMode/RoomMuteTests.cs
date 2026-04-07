using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T063 — Room mute/unmute player.
    ///
    /// Coverage (10 cases):
    ///
    /// StubNetworkAdapter — MutePlayer
    ///   [01] MutePlayer_FiresOnPlayerMutedCallback_WithPlayerName
    ///   [02] MutePlayer_UnknownRoom_DoesNotFireCallback
    ///   [03] MutePlayer_AlreadyMuted_DoesNotFireCallbackAgain
    ///   [04] MutePlayer_IncreasesMuteCallCount
    ///   [05] IsMuted_ReturnsTrueAfterMute_FalseAfterUnmute
    ///
    /// StubNetworkAdapter — UnmutePlayer
    ///   [06] UnmutePlayer_FiresOnPlayerUnmutedCallback_WithPlayerName
    ///   [07] UnmutePlayer_NotMuted_DoesNotFireCallback
    ///   [08] UnmutePlayer_UnknownRoom_DoesNotFireCallback
    ///
    /// MutedPlayerUI
    ///   [09] MutedPlayerUI_ShowMuted_ShowsPanelAndSetsState
    ///   [10] MutedPlayerUI_ShowUnmuted_UpdatesStateAndLabel
    /// </summary>
    [TestFixture]
    public sealed class RoomMuteTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private StubNetworkAdapter _stub;
        private GameObject         _uiGo;
        private MutedPlayerUI      _mutedUI;
        private Text               _statusLabel;

        [SetUp]
        public void SetUp()
        {
            StubNetworkAdapter.ClearRooms();
            _stub = new StubNetworkAdapter();

            _uiGo        = new GameObject("MutedPlayerUI_Test");
            _statusLabel = new GameObject("StatusLabel").AddComponent<Text>();
            _statusLabel.transform.SetParent(_uiGo.transform, false);

            _mutedUI = _uiGo.AddComponent<MutedPlayerUI>();
            InjectField(_mutedUI, "_statusLabel", _statusLabel);
        }

        [TearDown]
        public void TearDown()
        {
            StubNetworkAdapter.ClearRooms();
            Object.DestroyImmediate(_uiGo);
        }

        // ── [01] MutePlayer fires OnPlayerMuted with the correct name ─────────

        [Test]
        public void MutePlayer_FiresOnPlayerMutedCallback_WithPlayerName()
        {
            _stub.HostPlayerName = "Alice";
            _stub.JoinPlayerName = "Bob";
            _stub.Host("AAAA");
            _stub.Join("AAAA");

            string mutedName = null;
            _stub.OnPlayerMuted = name => mutedName = name;

            _stub.MutePlayer("AAAA", "Bob");

            Assert.AreEqual("Bob", mutedName,
                "OnPlayerMuted must be invoked with the muted player's display name.");
        }

        // ── [02] MutePlayer for unknown room does not fire callback ───────────

        [Test]
        public void MutePlayer_UnknownRoom_DoesNotFireCallback()
        {
            bool callbackFired = false;
            _stub.OnPlayerMuted = _ => callbackFired = true;

            _stub.MutePlayer("ZZZZ", "Ghost");

            Assert.IsFalse(callbackFired,
                "OnPlayerMuted must NOT fire when the room does not exist.");
        }

        // ── [03] Muting an already-muted player does not re-fire callback ─────

        [Test]
        public void MutePlayer_AlreadyMuted_DoesNotFireCallbackAgain()
        {
            _stub.Host("BBBB");
            _stub.JoinPlayerName = "Carol";
            _stub.Join("BBBB");

            int callCount = 0;
            _stub.OnPlayerMuted = _ => callCount++;

            _stub.MutePlayer("BBBB", "Carol");
            _stub.MutePlayer("BBBB", "Carol"); // duplicate — no-op

            Assert.AreEqual(1, callCount,
                "OnPlayerMuted must fire exactly once even when MutePlayer is called twice.");
        }

        // ── [04] MuteCallCount increments on each MutePlayer call ────────────

        [Test]
        public void MutePlayer_IncreasesMuteCallCount()
        {
            _stub.Host("CCCC");
            _stub.JoinPlayerName = "Dave";
            _stub.Join("CCCC");

            Assert.AreEqual(0, _stub.MuteCallCount, "MuteCallCount must start at 0.");

            _stub.MutePlayer("CCCC", "Dave");
            Assert.AreEqual(1, _stub.MuteCallCount, "MuteCallCount must be 1 after one mute.");

            _stub.MutePlayer("ZZZZ", "Nobody"); // unknown room — still counts
            Assert.AreEqual(2, _stub.MuteCallCount,
                "MuteCallCount must still increment even for a no-op mute on an unknown room.");
        }

        // ── [05] IsMuted returns true after mute; false after unmute ──────────

        [Test]
        public void IsMuted_ReturnsTrueAfterMute_FalseAfterUnmute()
        {
            _stub.Host("DDDD");
            _stub.JoinPlayerName = "Eve";
            _stub.Join("DDDD");

            Assert.IsFalse(_stub.IsMuted("DDDD", "Eve"), "Eve must not be muted initially.");

            _stub.MutePlayer("DDDD", "Eve");
            Assert.IsTrue(_stub.IsMuted("DDDD", "Eve"), "Eve must be muted after MutePlayer.");

            _stub.UnmutePlayer("DDDD", "Eve");
            Assert.IsFalse(_stub.IsMuted("DDDD", "Eve"), "Eve must not be muted after UnmutePlayer.");
        }

        // ── [06] UnmutePlayer fires OnPlayerUnmuted with the correct name ─────

        [Test]
        public void UnmutePlayer_FiresOnPlayerUnmutedCallback_WithPlayerName()
        {
            _stub.Host("EEEE");
            _stub.JoinPlayerName = "Frank";
            _stub.Join("EEEE");

            _stub.MutePlayer("EEEE", "Frank");

            string unmutedName = null;
            _stub.OnPlayerUnmuted = name => unmutedName = name;

            _stub.UnmutePlayer("EEEE", "Frank");

            Assert.AreEqual("Frank", unmutedName,
                "OnPlayerUnmuted must be invoked with the unmuted player's display name.");
        }

        // ── [07] UnmutePlayer for a player who is not muted does not fire ─────

        [Test]
        public void UnmutePlayer_NotMuted_DoesNotFireCallback()
        {
            _stub.Host("FFFF");
            _stub.JoinPlayerName = "Grace";
            _stub.Join("FFFF");

            bool callbackFired = false;
            _stub.OnPlayerUnmuted = _ => callbackFired = true;

            _stub.UnmutePlayer("FFFF", "Grace"); // Grace was never muted

            Assert.IsFalse(callbackFired,
                "OnPlayerUnmuted must NOT fire if the player was not muted.");
        }

        // ── [08] UnmutePlayer for unknown room does not fire callback ─────────

        [Test]
        public void UnmutePlayer_UnknownRoom_DoesNotFireCallback()
        {
            bool callbackFired = false;
            _stub.OnPlayerUnmuted = _ => callbackFired = true;

            _stub.UnmutePlayer("ZZZZ", "Ghost");

            Assert.IsFalse(callbackFired,
                "OnPlayerUnmuted must NOT fire when the room does not exist.");
        }

        // ── [09] MutedPlayerUI.ShowMuted shows panel and sets state ───────────

        [Test]
        public void MutedPlayerUI_ShowMuted_ShowsPanelAndSetsState()
        {
            Assert.IsFalse(_mutedUI.IsVisible,   "IsVisible must be false by default.");
            Assert.IsFalse(_mutedUI.IsMuted,     "IsMuted must be false by default.");
            Assert.AreEqual(string.Empty, _mutedUI.LastPlayerName,
                "LastPlayerName must be empty before ShowMuted is called.");

            _mutedUI.ShowMuted("Hank");

            Assert.IsTrue(_mutedUI.IsVisible,    "IsVisible must be true after ShowMuted.");
            Assert.IsTrue(_mutedUI.IsMuted,      "IsMuted must be true after ShowMuted.");
            Assert.AreEqual("Hank", _mutedUI.LastPlayerName,
                "LastPlayerName must store the player name passed to ShowMuted.");
            Assert.IsNotEmpty(_statusLabel.text,
                "Status label must contain text after ShowMuted.");
        }

        // ── [10] MutedPlayerUI.ShowUnmuted updates state and label ───────────

        [Test]
        public void MutedPlayerUI_ShowUnmuted_UpdatesStateAndLabel()
        {
            _mutedUI.ShowMuted("Iris");
            Assert.IsTrue(_mutedUI.IsMuted, "Precondition: IsMuted must be true after ShowMuted.");

            _mutedUI.ShowUnmuted("Iris");

            Assert.IsTrue(_mutedUI.IsVisible,
                "IsVisible must remain true after ShowUnmuted (panel still shown briefly).");
            Assert.IsFalse(_mutedUI.IsMuted,
                "IsMuted must be false after ShowUnmuted.");
            Assert.AreEqual("Iris", _mutedUI.LastPlayerName,
                "LastPlayerName must be updated by ShowUnmuted.");
            Assert.IsNotEmpty(_statusLabel.text,
                "Status label must contain unmuted text after ShowUnmuted.");

            _mutedUI.Hide();
            Assert.IsFalse(_mutedUI.IsVisible, "IsVisible must be false after Hide.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void InjectField<TComponent, TValue>(
            TComponent target, string fieldName, TValue value)
            where TComponent : Component
        {
            System.Reflection.FieldInfo field =
                typeof(TComponent).GetField(
                    fieldName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);

            Assert.IsNotNull(field,
                $"Reflection: field '{fieldName}' not found on {typeof(TComponent).Name}.");

            field.SetValue(target, value);
        }
    }
}
