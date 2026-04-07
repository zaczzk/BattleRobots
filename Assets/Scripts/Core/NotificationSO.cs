using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>Classifies the event that triggered a notification.</summary>
    public enum NotificationKind
    {
        Generic       = 0,
        FriendRequest = 1,
        Kick          = 2,
        ChatMention   = 3,
    }

    /// <summary>
    /// Single notification entry stored in the <see cref="NotificationSO"/> ring buffer.
    /// Immutable value type; <see cref="TimestampTicks"/> is set to
    /// <c>DateTime.UtcNow.Ticks</c> at the moment <see cref="NotificationSO.Post"/> is called.
    /// </summary>
    [Serializable]
    public struct NotificationEntry
    {
        public NotificationKind Kind;
        public string           Message;
        public long             TimestampTicks;
    }

    /// <summary>
    /// Runtime ScriptableObject acting as a notification ring-buffer and dispatcher.
    ///
    /// ── Ring buffer ────────────────────────────────────────────────────────────
    ///   Retains up to <see cref="_capacity"/> notifications.  When the buffer is
    ///   full the oldest entry is silently overwritten (zero-alloc, same strategy
    ///   as <see cref="ChatSO"/>).
    ///
    /// ── Event channels ─────────────────────────────────────────────────────────
    ///   <list type="bullet">
    ///     <item><c>_onNotificationPosted</c> (VoidGameEvent) — raised on every
    ///       successful <see cref="Post"/> call.</item>
    ///     <item><c>_onMessagePosted</c> (StringGameEvent) — raised with the
    ///       message text so <see cref="BattleRobots.UI.NotificationToastUI"/> can
    ///       display a brief banner without a direct SO reference.</item>
    ///   </list>
    ///
    ///   Wire a <see cref="StringGameEventListener"/> on the
    ///   <c>NotificationToastUI</c> GameObject:
    ///     Event    → NotificationSO._onMessagePosted
    ///     Response → NotificationToastUI.ShowToast(string)
    ///
    /// ── Architecture rules ─────────────────────────────────────────────────────
    ///   • <c>BattleRobots.Core</c> namespace — no UI or Physics references.
    ///   • SO asset is immutable at runtime; only the transient ring-buffer state
    ///     changes.
    ///   • No heap allocations in <see cref="Post"/> hot path.
    ///
    /// Create via: Assets ▶ Create ▶ BattleRobots ▶ UI ▶ NotificationSO
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/UI/NotificationSO", order = 4)]
    public sealed class NotificationSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Ring Buffer")]
        [Tooltip("Maximum notifications retained. Oldest entry is overwritten when capacity is exceeded.")]
        [SerializeField, Min(1)] private int _capacity = 20;

        [Header("Event Channels — Out")]
        [Tooltip("Raised after every successful Post() call.")]
        [SerializeField] private VoidGameEvent _onNotificationPosted;

        [Tooltip("Raised after every successful Post() call. Payload = message text. " +
                 "Wire a StringGameEventListener on NotificationToastUI → ShowToast(string).")]
        [SerializeField] private StringGameEvent _onMessagePosted;

        // ── Ring-buffer state (transient) ─────────────────────────────────────

        private NotificationEntry[] _buffer;
        private int _head;   // next write index
        private int _count;  // valid entries (0 .. _capacity)

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Number of notifications currently stored (0 .. capacity).</summary>
        public int Count => _count;

        /// <summary>
        /// Posts a new notification to the ring buffer and raises both event channels.
        /// <para>Null, empty, or whitespace-only messages are silently ignored.</para>
        /// </summary>
        public void Post(NotificationKind kind, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            EnsureBuffer();

            _buffer[_head] = new NotificationEntry
            {
                Kind           = kind,
                Message        = message,
                TimestampTicks = DateTime.UtcNow.Ticks,
            };
            _head = (_head + 1) % _capacity;
            if (_count < _capacity)
                _count++;

            _onNotificationPosted?.Raise();
            _onMessagePosted?.Raise(message);
        }

        /// <summary>
        /// Clears all stored notifications.
        /// Does NOT raise event channels.
        /// </summary>
        public void Clear()
        {
            EnsureBuffer();
            Array.Clear(_buffer, 0, _buffer.Length);
            _head  = 0;
            _count = 0;
        }

        /// <summary>
        /// Returns all stored notifications in chronological order (oldest first).
        /// Allocates a new list — do not call in Update.
        /// </summary>
        public IReadOnlyList<NotificationEntry> GetAll()
        {
            EnsureBuffer();

            var result = new List<NotificationEntry>(_count);
            if (_count == _capacity)
            {
                // Buffer full — oldest entry is at _head.
                for (int i = 0; i < _count; i++)
                    result.Add(_buffer[(_head + i) % _capacity]);
            }
            else
            {
                // Buffer not yet full — valid entries are at indices 0 .. _count-1.
                for (int i = 0; i < _count; i++)
                    result.Add(_buffer[i]);
            }

            return result;
        }

        /// <summary>
        /// Returns up to <paramref name="maxCount"/> of the most-recently posted
        /// notifications, newest first.
        /// Allocates a new list — do not call in Update.
        /// </summary>
        public IReadOnlyList<NotificationEntry> GetRecent(int maxCount)
        {
            EnsureBuffer();
            if (maxCount <= 0) return new List<NotificationEntry>(0);

            int take   = Mathf.Min(maxCount, _count);
            var result = new List<NotificationEntry>(take);

            // Most-recent entry is at (_head - 1 + _capacity) % _capacity.
            // For iteration i = 0..take-1 this generalises to the formula below
            // which handles both the full and partial buffer cases correctly.
            for (int i = 0; i < take; i++)
            {
                int idx = ((_head - 1 - i) % _capacity + _capacity) % _capacity;
                result.Add(_buffer[idx]);
            }

            return result;
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void EnsureBuffer()
        {
            int cap = Mathf.Max(1, _capacity);
            if (_buffer == null || _buffer.Length != cap)
            {
                _buffer = new NotificationEntry[cap];
                _head   = 0;
                _count  = 0;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_onNotificationPosted == null)
                Debug.LogWarning("[NotificationSO] _onNotificationPosted not assigned — " +
                                 "VoidGameEventListeners will not receive post notifications.");
            if (_onMessagePosted == null)
                Debug.LogWarning("[NotificationSO] _onMessagePosted not assigned — " +
                                 "NotificationToastUI will not show banners automatically.");
        }
#endif
    }
}
