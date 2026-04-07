using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T046 — Room join-failure UI feedback.
    ///
    /// Coverage (12 cases):
    ///
    /// StringGameEvent — basic channel contract
    ///   [01] StringGameEvent_Raise_NoListeners_DoesNotThrow
    ///   [02] StringGameEvent_Raise_NullValue_DoesNotThrow
    ///   [03] StringGameEvent_Raise_EmptyString_DoesNotThrow
    ///
    /// JoinFailureUI — state management
    ///   [04] JoinFailureUI_DefaultState_IsHidden
    ///   [05] JoinFailureUI_DefaultState_LastReasonIsEmpty
    ///   [06] JoinFailureUI_ShowFailure_MakesVisible
    ///   [07] JoinFailureUI_ShowFailure_SetsLastReason
    ///   [08] JoinFailureUI_Hide_AfterShow_MakesHidden
    ///   [09] JoinFailureUI_ShowFailure_EmptyReason_StillShowsPanel
    ///   [10] JoinFailureUI_ShowFailure_MultipleCallbacks_UpdatesReason
    ///
    /// Integration pattern (adapter → UI)
    ///   [11] AdapterPattern_JoinNonExistentRoom_ShowsFailureUI
    ///   [12] AdapterPattern_JoinFailedCallback_NullUI_DoesNotThrow
    /// </summary>
    [TestFixture]
    public sealed class JoinFailureTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private StubNetworkAdapter _stub;
        private StringGameEvent    _failEvent;
        private GameObject         _uiGO;
        private JoinFailureUI      _ui;

        [SetUp]
        public void SetUp()
        {
            StubNetworkAdapter.ClearRooms();
            _stub      = new StubNetworkAdapter();
            _failEvent = ScriptableObject.CreateInstance<StringGameEvent>();
            _uiGO      = new GameObject("JoinFailureUI");
            _ui        = _uiGO.AddComponent<JoinFailureUI>();
        }

        [TearDown]
        public void TearDown()
        {
            StubNetworkAdapter.ClearRooms();
            Object.DestroyImmediate(_failEvent);
            Object.DestroyImmediate(_uiGO);
        }

        // ── [01] StringGameEvent — no listeners, no throw ─────────────────────

        [Test]
        public void StringGameEvent_Raise_NoListeners_DoesNotThrow()
        {
            Assert.DoesNotThrow(
                () => _failEvent.Raise("Room not found."),
                "StringGameEvent.Raise must not throw even when no listeners are registered.");
        }

        // ── [02] StringGameEvent — null payload doesn't throw ─────────────────

        [Test]
        public void StringGameEvent_Raise_NullValue_DoesNotThrow()
        {
            Assert.DoesNotThrow(
                () => _failEvent.Raise(null),
                "StringGameEvent.Raise must not throw when passed a null string.");
        }

        // ── [03] StringGameEvent — empty string doesn't throw ─────────────────

        [Test]
        public void StringGameEvent_Raise_EmptyString_DoesNotThrow()
        {
            Assert.DoesNotThrow(
                () => _failEvent.Raise(string.Empty),
                "StringGameEvent.Raise must not throw when passed an empty string.");
        }

        // ── [04] JoinFailureUI — hidden by default ────────────────────────────

        [Test]
        public void JoinFailureUI_DefaultState_IsHidden()
        {
            Assert.IsFalse(_ui.IsVisible,
                "JoinFailureUI must start hidden (IsVisible == false).");
        }

        // ── [05] JoinFailureUI — LastReason empty by default ─────────────────

        [Test]
        public void JoinFailureUI_DefaultState_LastReasonIsEmpty()
        {
            Assert.AreEqual(string.Empty, _ui.LastReason,
                "JoinFailureUI.LastReason must be empty before any failure is shown.");
        }

        // ── [06] JoinFailureUI — ShowFailure makes panel visible ──────────────

        [Test]
        public void JoinFailureUI_ShowFailure_MakesVisible()
        {
            _ui.ShowFailure("Room 'ABCD' not found.");

            Assert.IsTrue(_ui.IsVisible,
                "IsVisible must be true after ShowFailure is called.");
        }

        // ── [07] JoinFailureUI — ShowFailure captures reason ─────────────────

        [Test]
        public void JoinFailureUI_ShowFailure_SetsLastReason()
        {
            const string expected = "Room 'XYZW' is full (2/2).";
            _ui.ShowFailure(expected);

            Assert.AreEqual(expected, _ui.LastReason,
                "LastReason must match the string passed to ShowFailure.");
        }

        // ── [08] JoinFailureUI — Hide after Show makes panel hidden ───────────

        [Test]
        public void JoinFailureUI_Hide_AfterShow_MakesHidden()
        {
            _ui.ShowFailure("Some error.");
            _ui.Hide();

            Assert.IsFalse(_ui.IsVisible,
                "IsVisible must be false after Hide() is called.");
        }

        // ── [09] JoinFailureUI — empty reason still shows the panel ──────────

        [Test]
        public void JoinFailureUI_ShowFailure_EmptyReason_StillShowsPanel()
        {
            _ui.ShowFailure(string.Empty);

            Assert.IsTrue(_ui.IsVisible,
                "ShowFailure with an empty reason string must still make the panel visible.");
            Assert.AreEqual(string.Empty, _ui.LastReason,
                "LastReason must be empty when ShowFailure is passed an empty string.");
        }

        // ── [10] JoinFailureUI — successive calls update the reason ───────────

        [Test]
        public void JoinFailureUI_ShowFailure_MultipleCallbacks_UpdatesReason()
        {
            _ui.ShowFailure("First failure.");
            _ui.ShowFailure("Second failure.");

            Assert.AreEqual("Second failure.", _ui.LastReason,
                "LastReason must reflect the most recent call to ShowFailure.");
            Assert.IsTrue(_ui.IsVisible,
                "IsVisible must remain true after a second ShowFailure call.");
        }

        // ── [11] Integration: adapter join-fail → UI.ShowFailure ─────────────

        [Test]
        public void AdapterPattern_JoinNonExistentRoom_ShowsFailureUI()
        {
            // Wire the adapter's failure callback directly to the UI
            // (simulating what happens when NetworkEventBridge raises StringGameEvent
            //  and a StringGameEventListener routes it to JoinFailureUI.ShowFailure).
            _stub.OnRoomJoinFailed = reason => _ui.ShowFailure(reason);

            // Joining a room that was never hosted triggers OnRoomJoinFailed.
            _stub.Join("FAKE");

            Assert.IsTrue(_ui.IsVisible,
                "JoinFailureUI must be visible after the adapter fires OnRoomJoinFailed.");
            StringAssert.Contains("not found", _ui.LastReason,
                "The failure reason must mention that the room was not found.");
        }

        // ── [12] Integration: null UI reference does not throw ────────────────

        [Test]
        public void AdapterPattern_JoinFailedCallback_NullUI_DoesNotThrow()
        {
            JoinFailureUI nullUI = null;

            // Simulate the bridge callback with a null UI (null-safe guard must prevent crash).
            _stub.OnRoomJoinFailed = reason => nullUI?.ShowFailure(reason);

            Assert.DoesNotThrow(
                () => _stub.Join("FAKE"),
                "Adapter callback must not throw even when the target JoinFailureUI is null.");
        }
    }
}
