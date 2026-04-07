using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T066 — Notification system
    /// (NotificationSO ring-buffer + NotificationToastUI).
    ///
    /// Coverage (22 cases):
    ///
    /// NotificationSO — default state
    ///   [01] DefaultState_Count_IsZero
    ///   [02] DefaultState_GetAll_IsEmpty
    ///
    /// NotificationSO — Post validation
    ///   [03] Post_NullMessage_IsIgnored
    ///   [04] Post_EmptyMessage_IsIgnored
    ///   [05] Post_WhitespaceOnly_IsIgnored
    ///
    /// NotificationSO — Post behaviour
    ///   [06] Post_ValidMessage_IncreasesCount
    ///   [07] Post_ValidMessage_IsInGetAll
    ///   [08] Post_SetsCorrectNotificationKind
    ///   [09] Post_MultipleMessages_GetAll_OrderedOldestFirst
    ///   [10] Post_TimestampTicks_IsPositive
    ///
    /// NotificationSO — ring-buffer capacity
    ///   [11] Post_AtCapacity_CountStaysAtCapacity
    ///   [12] Post_AtCapacity_OldestDropped
    ///   [13] Post_PastCapacity_NewestEntryPresent
    ///
    /// NotificationSO — Clear
    ///   [14] Clear_ResetsCountToZero
    ///   [15] Clear_EmptiesGetAll
    ///
    /// NotificationSO — GetRecent
    ///   [16] GetRecent_ReturnsNewestFirst
    ///   [17] GetRecent_LimitsByMaxCount
    ///   [18] GetRecent_ZeroMax_ReturnsEmpty
    ///
    /// NotificationSO — event channels
    ///   [19] Post_RaisesVoidEvent
    ///   [20] Post_RaisesStringEvent_WithMessage
    ///
    /// NotificationToastUI
    ///   [21] ShowToast_SetsLabelText
    ///   [22] ShowToast_MakesPanel_Visible
    /// </summary>
    [TestFixture]
    public sealed class NotificationTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private NotificationSO _so;

        [SetUp]
        public void SetUp()
        {
            _so = ScriptableObject.CreateInstance<NotificationSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_so);
        }

        // ── [01] Default state — Count ────────────────────────────────────────

        [Test]
        public void DefaultState_Count_IsZero()
        {
            Assert.AreEqual(0, _so.Count);
        }

        // ── [02] Default state — GetAll ───────────────────────────────────────

        [Test]
        public void DefaultState_GetAll_IsEmpty()
        {
            IReadOnlyList<NotificationEntry> all = _so.GetAll();
            Assert.IsNotNull(all);
            Assert.AreEqual(0, all.Count);
        }

        // ── [03] Post — null message is ignored ───────────────────────────────

        [Test]
        public void Post_NullMessage_IsIgnored()
        {
            _so.Post(NotificationKind.Generic, null);
            Assert.AreEqual(0, _so.Count);
        }

        // ── [04] Post — empty message is ignored ──────────────────────────────

        [Test]
        public void Post_EmptyMessage_IsIgnored()
        {
            _so.Post(NotificationKind.Generic, string.Empty);
            Assert.AreEqual(0, _so.Count);
        }

        // ── [05] Post — whitespace-only message is ignored ────────────────────

        [Test]
        public void Post_WhitespaceOnly_IsIgnored()
        {
            _so.Post(NotificationKind.Generic, "   ");
            Assert.AreEqual(0, _so.Count);
        }

        // ── [06] Post — valid message increases Count ─────────────────────────

        [Test]
        public void Post_ValidMessage_IncreasesCount()
        {
            _so.Post(NotificationKind.FriendRequest, "Alice sent you a friend request.");
            Assert.AreEqual(1, _so.Count);
        }

        // ── [07] Post — valid message appears in GetAll ───────────────────────

        [Test]
        public void Post_ValidMessage_IsInGetAll()
        {
            _so.Post(NotificationKind.FriendRequest, "Alice sent you a friend request.");

            IReadOnlyList<NotificationEntry> all = _so.GetAll();
            Assert.AreEqual(1, all.Count);
            Assert.AreEqual("Alice sent you a friend request.", all[0].Message);
        }

        // ── [08] Post — sets correct NotificationKind ─────────────────────────

        [Test]
        public void Post_SetsCorrectNotificationKind()
        {
            _so.Post(NotificationKind.Kick, "You were kicked from the room.");

            NotificationEntry entry = _so.GetAll()[0];
            Assert.AreEqual(NotificationKind.Kick, entry.Kind);
        }

        // ── [09] Post — GetAll returns oldest first ───────────────────────────

        [Test]
        public void Post_MultipleMessages_GetAll_OrderedOldestFirst()
        {
            _so.Post(NotificationKind.Generic,       "A");
            _so.Post(NotificationKind.ChatMention,   "B");
            _so.Post(NotificationKind.FriendRequest, "C");

            IReadOnlyList<NotificationEntry> all = _so.GetAll();
            Assert.AreEqual(3, all.Count);
            Assert.AreEqual("A", all[0].Message, "First element must be the oldest (A).");
            Assert.AreEqual("B", all[1].Message);
            Assert.AreEqual("C", all[2].Message, "Last element must be the newest (C).");
        }

        // ── [10] Post — TimestampTicks is positive ────────────────────────────

        [Test]
        public void Post_TimestampTicks_IsPositive()
        {
            _so.Post(NotificationKind.Generic, "Test notification.");

            long ticks = _so.GetAll()[0].TimestampTicks;
            Assert.Greater(ticks, 0L, "TimestampTicks must be a positive DateTime.UtcNow.Ticks value.");
        }

        // ── [11] Ring buffer — Count does not exceed capacity ─────────────────

        [Test]
        public void Post_AtCapacity_CountStaysAtCapacity()
        {
            SetCapacity(_so, 3);

            _so.Post(NotificationKind.Generic, "A");
            _so.Post(NotificationKind.Generic, "B");
            _so.Post(NotificationKind.Generic, "C");
            _so.Post(NotificationKind.Generic, "D"); // overflow

            Assert.AreEqual(3, _so.Count,
                "Count must not exceed the ring-buffer capacity.");
        }

        // ── [12] Ring buffer — oldest entry is dropped when full ──────────────

        [Test]
        public void Post_AtCapacity_OldestDropped()
        {
            SetCapacity(_so, 3);

            _so.Post(NotificationKind.Generic, "A");
            _so.Post(NotificationKind.Generic, "B");
            _so.Post(NotificationKind.Generic, "C");
            _so.Post(NotificationKind.Generic, "D"); // evicts A

            IReadOnlyList<NotificationEntry> all = _so.GetAll();
            Assert.AreEqual("B", all[0].Message, "B must now be the oldest remaining entry.");
            Assert.AreEqual("C", all[1].Message);
            Assert.AreEqual("D", all[2].Message);
        }

        // ── [13] Ring buffer — newest entry always present after overflow ─────

        [Test]
        public void Post_PastCapacity_NewestEntryPresent()
        {
            SetCapacity(_so, 2);

            _so.Post(NotificationKind.Generic, "X");
            _so.Post(NotificationKind.Generic, "Y");
            _so.Post(NotificationKind.Generic, "Z"); // evicts X

            IReadOnlyList<NotificationEntry> all = _so.GetAll();
            Assert.AreEqual("Z", all[all.Count - 1].Message,
                "The most-recently posted message must be the last entry in GetAll.");
        }

        // ── [14] Clear — resets Count to zero ────────────────────────────────

        [Test]
        public void Clear_ResetsCountToZero()
        {
            _so.Post(NotificationKind.Generic, "Msg 1");
            _so.Post(NotificationKind.Generic, "Msg 2");
            _so.Clear();

            Assert.AreEqual(0, _so.Count, "Count must be 0 after Clear.");
        }

        // ── [15] Clear — GetAll returns empty list ────────────────────────────

        [Test]
        public void Clear_EmptiesGetAll()
        {
            _so.Post(NotificationKind.Generic, "Msg 1");
            _so.Clear();

            Assert.AreEqual(0, _so.GetAll().Count,
                "GetAll must return an empty list after Clear.");
        }

        // ── [16] GetRecent — newest first ────────────────────────────────────

        [Test]
        public void GetRecent_ReturnsNewestFirst()
        {
            SetCapacity(_so, 5);

            _so.Post(NotificationKind.Generic, "A");
            _so.Post(NotificationKind.Generic, "B");
            _so.Post(NotificationKind.Generic, "C");

            IReadOnlyList<NotificationEntry> recent = _so.GetRecent(3);
            Assert.AreEqual(3, recent.Count);
            Assert.AreEqual("C", recent[0].Message, "Index 0 must be the newest entry (C).");
            Assert.AreEqual("B", recent[1].Message);
            Assert.AreEqual("A", recent[2].Message, "Index 2 must be the oldest entry (A).");
        }

        // ── [17] GetRecent — respects maxCount cap ────────────────────────────

        [Test]
        public void GetRecent_LimitsByMaxCount()
        {
            _so.Post(NotificationKind.Generic, "A");
            _so.Post(NotificationKind.Generic, "B");
            _so.Post(NotificationKind.Generic, "C");

            IReadOnlyList<NotificationEntry> recent = _so.GetRecent(2);
            Assert.AreEqual(2, recent.Count,
                "GetRecent must return at most maxCount entries.");
            Assert.AreEqual("C", recent[0].Message, "First returned must be the newest (C).");
            Assert.AreEqual("B", recent[1].Message);
        }

        // ── [18] GetRecent — zero max returns empty ───────────────────────────

        [Test]
        public void GetRecent_ZeroMax_ReturnsEmpty()
        {
            _so.Post(NotificationKind.Generic, "A");

            IReadOnlyList<NotificationEntry> recent = _so.GetRecent(0);
            Assert.IsNotNull(recent);
            Assert.AreEqual(0, recent.Count,
                "GetRecent(0) must return an empty list.");
        }

        // ── [19] Post — raises VoidGameEvent ─────────────────────────────────

        [Test]
        public void Post_RaisesVoidEvent()
        {
            // Wire a VoidGameEvent into _onNotificationPosted via reflection.
            var voidEvt = ScriptableObject.CreateInstance<VoidGameEvent>();
            InjectField(_so, "_onNotificationPosted", voidEvt);

            // Wire a VoidGameEventListener to count invocations.
            var listenerGo = new GameObject("VoidListener");
            var listener   = listenerGo.AddComponent<VoidGameEventListener>();
            listenerGo.SetActive(false); // disable so we can inject fields before OnEnable

            InjectField(listener, "_event", voidEvt);

            int callCount = 0;
            var response  = new UnityEvent();
            response.AddListener(() => callCount++);
            InjectField(listener, "_response", response);

            listenerGo.SetActive(true); // OnEnable → RegisterListener

            _so.Post(NotificationKind.Generic, "Event test");

            Assert.AreEqual(1, callCount,
                "Post must raise _onNotificationPosted exactly once.");

            Object.DestroyImmediate(listenerGo);
            Object.DestroyImmediate(voidEvt);
        }

        // ── [20] Post — raises StringGameEvent with message text ──────────────

        [Test]
        public void Post_RaisesStringEvent_WithMessage()
        {
            // Wire a StringGameEvent into _onMessagePosted via reflection.
            var strEvt = ScriptableObject.CreateInstance<StringGameEvent>();
            InjectField(_so, "_onMessagePosted", strEvt);

            // Wire a StringGameEventListener to capture the payload.
            var listenerGo = new GameObject("StringListener");
            var listener   = listenerGo.AddComponent<StringGameEventListener>();
            listenerGo.SetActive(false);

            InjectField(listener, "_event", strEvt);

            string received = null;
            var response    = new UnityEvent<string>();
            response.AddListener(s => received = s);
            InjectField(listener, "_response", response);

            listenerGo.SetActive(true); // OnEnable → RegisterListener

            const string msg = "You were mentioned in chat.";
            _so.Post(NotificationKind.ChatMention, msg);

            Assert.AreEqual(msg, received,
                "Post must raise _onMessagePosted with the exact message string.");

            Object.DestroyImmediate(listenerGo);
            Object.DestroyImmediate(strEvt);
        }

        // ── [21] NotificationToastUI — ShowToast sets label text ──────────────

        [Test]
        public void ShowToast_SetsLabelText()
        {
            var toastGo = new GameObject("Toast");
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(toastGo.transform, false);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(panelGo.transform, false);
            var label = labelGo.AddComponent<Text>();

            var toast = toastGo.AddComponent<NotificationToastUI>();
            InjectField(toast, "_toastPanel",   panelGo);
            InjectField(toast, "_messageLabel", label);

            // ShowToast tries to start a coroutine which requires an active MB.
            // Disable auto-hide by setting a very large duration.
            SetField(toast, "_displayDuration", 9999f);

            toast.ShowToast("Friend request received!");

            Assert.AreEqual("Friend request received!", label.text,
                "_messageLabel.text must match the message passed to ShowToast.");
            Assert.AreEqual("Friend request received!", toast.LastMessage,
                "LastMessage must be updated by ShowToast.");

            Object.DestroyImmediate(toastGo);
        }

        // ── [22] NotificationToastUI — ShowToast activates the panel ─────────

        [Test]
        public void ShowToast_MakesPanel_Visible()
        {
            var toastGo = new GameObject("Toast");
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(toastGo.transform, false);
            panelGo.SetActive(false); // start hidden

            var toast = toastGo.AddComponent<NotificationToastUI>();
            InjectField(toast, "_toastPanel", panelGo);
            SetField(toast, "_displayDuration", 9999f);

            Assert.IsFalse(toast.IsVisible, "Panel should be hidden before ShowToast.");

            toast.ShowToast("You were kicked.");

            Assert.IsTrue(toast.IsVisible,
                "ShowToast must activate _toastPanel (IsVisible == true).");

            Object.DestroyImmediate(toastGo);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Sets <see cref="NotificationSO._capacity"/> via reflection and forces
        /// the ring buffer to re-initialise on the next call.
        /// </summary>
        private static void SetCapacity(NotificationSO so, int cap)
        {
            var f = typeof(NotificationSO).GetField(
                "_capacity",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(f, "Reflection: _capacity field not found on NotificationSO.");
            f.SetValue(so, cap);

            // Force buffer reallocation by nulling the private _buffer field.
            var buf = typeof(NotificationSO).GetField(
                "_buffer",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            buf?.SetValue(so, null);
        }

        /// <summary>
        /// Injects <paramref name="value"/> into a private field on <paramref name="target"/>,
        /// walking the inheritance chain to find fields declared on base types.
        /// </summary>
        private static void InjectField<TTarget, TValue>(TTarget target, string fieldName, TValue value)
            where TTarget : Object
        {
            System.Type type = target.GetType();
            System.Reflection.FieldInfo f = null;
            while (type != null && f == null)
            {
                f = type.GetField(fieldName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
                type = type.BaseType;
            }
            Assert.IsNotNull(f,
                $"Reflection: field '{fieldName}' not found on {target.GetType().Name} or its base types.");
            f.SetValue(target, value);
        }

        /// <summary>
        /// Sets a private field by value on a plain object (not necessarily a UnityEngine.Object).
        /// </summary>
        private static void SetField<TTarget, TValue>(TTarget target, string fieldName, TValue value)
        {
            System.Type type = target.GetType();
            System.Reflection.FieldInfo f = null;
            while (type != null && f == null)
            {
                f = type.GetField(fieldName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
                type = type.BaseType;
            }
            Assert.IsNotNull(f,
                $"Reflection: field '{fieldName}' not found on {typeof(TTarget).Name}.");
            f.SetValue(target, value);
        }
    }
}
