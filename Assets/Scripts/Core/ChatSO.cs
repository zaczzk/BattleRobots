using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that stores the recent in-room chat history as a
    /// ring-buffer and broadcasts each new message via a <see cref="StringGameEvent"/>
    /// channel.
    ///
    /// Messages are pre-formatted ("senderName: text") by the caller before being
    /// passed to <see cref="AddMessage"/>. This keeps the SO agnostic of network
    /// message framing.
    ///
    /// ── Architecture rules ─────────────────────────────────────────────────────
    ///   - Lives in BattleRobots.Core — no UI or Physics references.
    ///   - Mutated only through <see cref="AddMessage"/> and <see cref="Clear"/>.
    ///   - No allocations in hot path: ring-buffer overwrites the oldest slot once
    ///     capacity is reached.
    ///   - <see cref="Messages"/> is a read-only view; never mutate externally.
    ///
    /// ── Scene wiring ───────────────────────────────────────────────────────────
    ///   1. Create via Assets ▶ BattleRobots ▶ Network ▶ ChatSO.
    ///   2. Assign to <see cref="NetworkEventBridge._chat"/> in Inspector.
    ///   3. Wire <see cref="_onMessageReceived"/> to a <see cref="StringGameEventListener"/>
    ///      on the ChatUI GameObject; point Response to ChatUI.AppendMessage.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Network/ChatSO", order = 2)]
    public sealed class ChatSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Ring Buffer")]
        [Tooltip("Maximum number of chat messages retained. Oldest entry is dropped when capacity is exceeded.")]
        [SerializeField, Min(1)] private int _capacity = 50;

        [Header("Event Channel")]
        [Tooltip("Raised after each new message is added. Payload = the formatted message string. " +
                 "Wire a StringGameEventListener on ChatUI to ChatUI.AppendMessage.")]
        [SerializeField] private StringGameEvent _onMessageReceived;

        // ── Ring-buffer state ─────────────────────────────────────────────────

        // Ring buffer: _buffer[_head] is the next write position.
        // _count tracks how many valid entries exist (capped at _capacity).
        private string[] _buffer;
        private int      _head;   // next write index
        private int      _count;  // valid entries (0 .. _capacity)

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Add a pre-formatted chat message (e.g. "Alice: Hello!") to the ring buffer
        /// and raise the <see cref="_onMessageReceived"/> event channel.
        ///
        /// Null or empty messages are silently ignored.
        /// </summary>
        public void AddMessage(string formattedMessage)
        {
            if (string.IsNullOrEmpty(formattedMessage)) return;

            EnsureBuffer();

            _buffer[_head] = formattedMessage;
            _head = (_head + 1) % _capacity;
            if (_count < _capacity)
                _count++;

            _onMessageReceived?.Raise(formattedMessage);
        }

        /// <summary>Clear all chat history. Does NOT raise the event channel.</summary>
        public void Clear()
        {
            EnsureBuffer();
            for (int i = 0; i < _capacity; i++)
                _buffer[i] = null;
            _head  = 0;
            _count = 0;
        }

        /// <summary>Number of messages currently stored.</summary>
        public int Count => _count;

        /// <summary>
        /// Returns all stored messages in chronological order (oldest first).
        ///
        /// Allocates a new list — call only on UI rebuild, never in Update.
        /// </summary>
        public IReadOnlyList<string> GetMessages()
        {
            EnsureBuffer();

            var result = new List<string>(_count);
            if (_count == _capacity)
            {
                // Buffer is full — oldest entry is at _head.
                for (int i = 0; i < _count; i++)
                    result.Add(_buffer[(_head + i) % _capacity]);
            }
            else
            {
                // Buffer not yet full — entries run from index 0 to _count - 1.
                for (int i = 0; i < _count; i++)
                    result.Add(_buffer[i]);
            }

            return result;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Lazily allocates / reallocates the ring buffer.
        /// Called before any read or write — handles first-use and SO reimport.
        /// </summary>
        private void EnsureBuffer()
        {
            int cap = Mathf.Max(1, _capacity);
            if (_buffer == null || _buffer.Length != cap)
            {
                _buffer = new string[cap];
                _head   = 0;
                _count  = 0;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_onMessageReceived == null)
                Debug.LogWarning("[ChatSO] _onMessageReceived StringGameEvent not assigned — " +
                                 "ChatUI will not receive messages via the event channel.");
        }
#endif
    }
}
