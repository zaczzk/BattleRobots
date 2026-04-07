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
    /// EditMode unit tests for T061 — Room Chat Channel.
    ///
    /// Coverage (10 cases):
    ///
    /// ChatSO — ring buffer
    ///   [01] ChatSO_AddMessage_StoresMessage
    ///   [02] ChatSO_CapacityExceeded_OverwritesOldestSlot
    ///   [03] ChatSO_Clear_EmptiesHistory
    ///   [04] ChatSO_AddMessage_FiresEvent
    ///   [05] ChatSO_NullOrEmpty_Ignored
    ///
    /// StubNetworkAdapter — SendChatMessage
    ///   [06] StubAdapter_SendChatMessage_AppendsSentMessages
    ///   [07] StubAdapter_NullMessage_Ignored
    ///
    /// INetworkAdapter — OnChatMessageReceived callback
    ///   [08] OnChatMessageReceived_CanBeAssignedAndInvoked
    ///
    /// NetworkEventBridge — SendChat + incoming callback wiring
    ///   [09] Bridge_SendChat_FormatsAndCallsAdapter
    ///   [10] Bridge_OnChatReceived_ForwardsToChatSO
    /// </summary>
    [TestFixture]
    public sealed class ChatTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private ChatSO             _chat;
        private StubNetworkAdapter _stub;
        private GameObject         _bridgeGo;
        private NetworkEventBridge _bridge;

        [SetUp]
        public void SetUp()
        {
            _chat = ScriptableObject.CreateInstance<ChatSO>();
            SetCapacity(_chat, 3); // small cap to test ring overflow

            StubNetworkAdapter.ClearRooms();
            _stub = new StubNetworkAdapter();

            _bridgeGo = new GameObject("NetworkEventBridge");
            _bridge   = _bridgeGo.AddComponent<NetworkEventBridge>();
            _bridge.SetAdapter(_stub);
            InjectField(_bridge, "_chat", _chat);
        }

        [TearDown]
        public void TearDown()
        {
            StubNetworkAdapter.ClearRooms();
            Object.DestroyImmediate(_bridgeGo);
            Object.DestroyImmediate(_chat);
        }

        // ── [01] AddMessage stores the message ───────────────────────────────

        [Test]
        public void ChatSO_AddMessage_StoresMessage()
        {
            _chat.AddMessage("Alice: Hello");

            Assert.AreEqual(1, _chat.Count,
                "Count must be 1 after one AddMessage call.");

            IReadOnlyList<string> msgs = _chat.GetMessages();
            Assert.AreEqual("Alice: Hello", msgs[0],
                "GetMessages must return the stored message.");
        }

        // ── [02] Capacity exceeded overwrites oldest slot ────────────────────

        [Test]
        public void ChatSO_CapacityExceeded_OverwritesOldestSlot()
        {
            // cap = 3; add 4 messages — first should be evicted.
            _chat.AddMessage("msg1");
            _chat.AddMessage("msg2");
            _chat.AddMessage("msg3");
            _chat.AddMessage("msg4"); // evicts "msg1"

            Assert.AreEqual(3, _chat.Count,
                "Count must not exceed capacity.");

            IReadOnlyList<string> msgs = _chat.GetMessages();
            Assert.AreEqual("msg2", msgs[0], "Oldest remaining message must be 'msg2'.");
            Assert.AreEqual("msg3", msgs[1]);
            Assert.AreEqual("msg4", msgs[2]);
        }

        // ── [03] Clear empties history ───────────────────────────────────────

        [Test]
        public void ChatSO_Clear_EmptiesHistory()
        {
            _chat.AddMessage("Alice: Hi");
            _chat.AddMessage("Bob: Hey");
            _chat.Clear();

            Assert.AreEqual(0, _chat.Count,
                "Count must be 0 after Clear.");
            Assert.AreEqual(0, _chat.GetMessages().Count,
                "GetMessages must return an empty list after Clear.");
        }

        // ── [04] AddMessage raises StringGameEvent ───────────────────────────

        [Test]
        public void ChatSO_AddMessage_FiresEvent()
        {
            // Create a StringGameEvent and wire it into the ChatSO.
            var evt = ScriptableObject.CreateInstance<StringGameEvent>();
            InjectField(_chat, "_onMessageReceived", evt);

            // Create a listener GO and inject fields before enabling.
            var listenerGo = new GameObject("StringListener");
            var listener   = listenerGo.AddComponent<StringGameEventListener>();
            listenerGo.SetActive(false); // pause OnEnable until fields are set

            InjectField(listener, "_event", evt);

            string received = null;
            var response = new UnityEvent<string>();
            response.AddListener(s => received = s);
            InjectField(listener, "_response", response);

            listenerGo.SetActive(true); // triggers OnEnable → registers listener

            _chat.AddMessage("Alice: event test");

            Assert.AreEqual("Alice: event test", received,
                "AddMessage must raise _onMessageReceived with the formatted message.");

            Object.DestroyImmediate(listenerGo);
            Object.DestroyImmediate(evt);
        }

        // ── [05] Null or empty messages are ignored ──────────────────────────

        [Test]
        public void ChatSO_NullOrEmpty_Ignored()
        {
            _chat.AddMessage(null);
            _chat.AddMessage(string.Empty);

            Assert.AreEqual(0, _chat.Count,
                "Null or empty messages must not increment Count.");
        }

        // ── [06] StubAdapter.SendChatMessage appends to SentChatMessages ─────

        [Test]
        public void StubAdapter_SendChatMessage_AppendsSentMessages()
        {
            _stub.SendChatMessage("Alice: Test");
            _stub.SendChatMessage("Bob: Reply");

            Assert.AreEqual(2, _stub.SentChatMessages.Count,
                "SentChatMessages must record each SendChatMessage call.");
            Assert.AreEqual("Alice: Test",  _stub.SentChatMessages[0]);
            Assert.AreEqual("Bob: Reply",   _stub.SentChatMessages[1]);
        }

        // ── [07] StubAdapter ignores null message ────────────────────────────

        [Test]
        public void StubAdapter_NullMessage_Ignored()
        {
            _stub.SendChatMessage(null);
            Assert.AreEqual(0, _stub.SentChatMessages.Count,
                "SendChatMessage(null) must not append to SentChatMessages.");
        }

        // ── [08] OnChatMessageReceived can be assigned and invoked ───────────

        [Test]
        public void OnChatMessageReceived_CanBeAssignedAndInvoked()
        {
            string received = null;
            _stub.OnChatMessageReceived = msg => received = msg;

            // Simulate a remote peer's message arriving at the adapter level.
            _stub.OnChatMessageReceived?.Invoke("Carol: incoming");

            Assert.AreEqual("Carol: incoming", received,
                "OnChatMessageReceived must pass the raw message string.");
        }

        // ── [09] Bridge.SendChat formats and calls adapter ───────────────────

        [Test]
        public void Bridge_SendChat_FormatsAndCallsAdapter()
        {
            _bridge.SendChat("Alice", "Hello world");

            Assert.AreEqual(1, _stub.SentChatMessages.Count,
                "SendChat must delegate to adapter.SendChatMessage.");
            Assert.AreEqual("Alice: Hello world", _stub.SentChatMessages[0],
                "Message must be formatted as 'sender: text'.");
        }

        // ── [10] Bridge wires OnChatMessageReceived → ChatSO.AddMessage ──────

        [Test]
        public void Bridge_OnChatReceived_ForwardsToChatSO()
        {
            // Simulate a message received from a remote peer via the adapter callback.
            _stub.OnChatMessageReceived?.Invoke("Bob: Hi from remote");

            Assert.AreEqual(1, _chat.Count,
                "Incoming chat message must be forwarded to ChatSO.AddMessage.");
            Assert.AreEqual("Bob: Hi from remote", _chat.GetMessages()[0]);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetCapacity(ChatSO so, int cap)
        {
            var f = typeof(ChatSO).GetField(
                "_capacity",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(f, "Reflection: _capacity field not found on ChatSO.");
            f.SetValue(so, cap);
        }

        /// <summary>
        /// Injects <paramref name="value"/> into a private field <paramref name="fieldName"/>
        /// declared on <paramref name="target"/> or any of its base types.
        /// Walks the inheritance chain so inherited private fields (e.g. those on
        /// <c>GameEventListener&lt;T&gt;</c>) are found even when called on a derived type.
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
    }
}
