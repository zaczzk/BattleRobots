using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>Event types that can appear in the in-match feed.</summary>
    public enum ZoneControlFeedEventType
    {
        ZoneCaptured      = 0,
        PowerUpCollected  = 1,
        BotCapture        = 2,
        VictoryAchieved   = 3,
    }

    /// <summary>Single entry stored in <see cref="ZoneControlMatchFeedSO"/>.</summary>
    [Serializable]
    public sealed class ZoneControlMatchFeedEntry
    {
        public float                   Timestamp;
        public ZoneControlFeedEventType Type;
        public string                  Message;
    }

    /// <summary>
    /// Append-only, bounded in-match event feed.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   <see cref="AddEntry"/> appends a <see cref="ZoneControlMatchFeedEntry"/>
    ///   and evicts the oldest when <c>_maxEntries</c> is reached, then fires
    ///   <c>_onFeedUpdated</c>.  A null or empty message is stored as
    ///   <see cref="string.Empty"/>.
    ///   <see cref="Reset"/> clears all entries silently.
    ///   <c>OnEnable</c> calls <see cref="Reset"/> so state never leaks across
    ///   play-mode sessions.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — resets via <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMatchFeed.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchFeed", order = 68)]
    public sealed class ZoneControlMatchFeedSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Maximum number of entries retained in the feed.")]
        [Min(1)]
        [SerializeField] private int _maxEntries = 10;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised after each entry is added.")]
        [SerializeField] private VoidGameEvent _onFeedUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private readonly List<ZoneControlMatchFeedEntry> _entries = new List<ZoneControlMatchFeedEntry>();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Current number of feed entries.</summary>
        public int EntryCount => _entries.Count;

        /// <summary>Maximum number of entries before oldest are evicted.</summary>
        public int MaxEntries => _maxEntries;

        /// <summary>Read-only view of all current entries (oldest first).</summary>
        public IReadOnlyList<ZoneControlMatchFeedEntry> Entries => _entries;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Appends a new feed entry.  Evicts the oldest entry when the buffer is
        /// at capacity.  A null or empty <paramref name="message"/> is stored as
        /// <see cref="string.Empty"/>.  Fires <c>_onFeedUpdated</c>.
        /// </summary>
        public void AddEntry(float timestamp, ZoneControlFeedEventType type, string message)
        {
            if (_entries.Count >= _maxEntries)
                _entries.RemoveAt(0);

            _entries.Add(new ZoneControlMatchFeedEntry
            {
                Timestamp = timestamp,
                Type      = type,
                Message   = string.IsNullOrEmpty(message) ? string.Empty : message,
            });

            _onFeedUpdated?.Raise();
        }

        /// <summary>
        /// Clears all entries silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _entries.Clear();
        }

        private void OnValidate()
        {
            _maxEntries = Mathf.Max(1, _maxEntries);
        }
    }
}
