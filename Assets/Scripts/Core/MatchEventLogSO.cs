using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// A single timestamped event entry in the match event log.
    /// Lightweight value type — no heap allocation per entry beyond the description string.
    /// </summary>
    [Serializable]
    public struct MatchEventEntry
    {
        /// <summary>Game time in seconds when the event occurred (e.g. Time.time).</summary>
        public float gameTime;

        /// <summary>Human-readable description of the event (e.g. "Player scored a hit!").</summary>
        public string description;

        /// <summary>Initialises a new entry with the supplied values.</summary>
        public MatchEventEntry(float gameTime, string description)
        {
            this.gameTime    = gameTime;
            this.description = description;
        }
    }

    /// <summary>
    /// Runtime SO that accumulates up to <see cref="MaxEvents"/> timestamped narrative
    /// entries during a match (e.g. "Player scored a hit!", "Enemy destroyed!",
    /// "Power-up collected!").
    ///
    /// ── Typical flow ─────────────────────────────────────────────────────────────
    ///   1. Call <see cref="Reset"/> at match start (or wire to a match-start event).
    ///   2. Any game system calls <see cref="LogEvent"/> to append a notable moment.
    ///   3. At match end, <see cref="MatchTimelineController"/> subscribes to
    ///      <c>_onMatchEnded</c> and reads <see cref="Events"/> to display the timeline.
    ///
    /// ── Mutators ─────────────────────────────────────────────────────────────────
    ///   • <see cref="LogEvent"/>  — appends an entry (oldest evicted when at capacity).
    ///   • <see cref="Reset"/>     — silent clear; no event.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is not serialised to the SO asset.
    ///   - <see cref="Reset"/> does NOT fire any event (bootstrapper/match-start safe).
    ///
    /// ── Scene / SO wiring ────────────────────────────────────────────────────────
    ///   1. Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ MatchEventLog.
    ///   2. Optionally assign <c>_onEventLogged</c> to a VoidGameEvent SO.
    ///   3. Assign to any system that needs to log events.
    ///   4. Assign to <see cref="BattleRobots.UI.MatchTimelineController"/> for display.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/MatchEventLog",
        fileName = "MatchEventLogSO")]
    public sealed class MatchEventLogSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Maximum number of event entries to keep. Oldest are evicted when at capacity.")]
        [SerializeField, Range(10, 100)] private int _maxEvents = 50;

        [Tooltip("VoidGameEvent raised after each successful LogEvent call. " +
                 "Leave null if no system needs to react immediately.")]
        [SerializeField] private VoidGameEvent _onEventLogged;

        // ── Runtime state ─────────────────────────────────────────────────────

        private readonly List<MatchEventEntry> _events = new List<MatchEventEntry>();

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>
        /// Chronological list of logged match events (oldest first).
        /// Never null; may be empty before the first <see cref="LogEvent"/> call or after
        /// <see cref="Reset"/>.
        /// </summary>
        public IReadOnlyList<MatchEventEntry> Events => _events;

        /// <summary>
        /// Maximum number of entries this log can hold before oldest are evicted.
        /// </summary>
        public int MaxEvents => _maxEvents;

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Appends a timestamped event entry.
        /// <para>
        /// • Null or whitespace-only <paramref name="description"/> is silently ignored.
        /// • When the list reaches <see cref="MaxEvents"/>, the oldest entry is evicted.
        /// • Fires <c>_onEventLogged</c> after appending.
        /// </para>
        /// </summary>
        /// <param name="description">Human-readable description. Must be non-null and non-whitespace.</param>
        /// <param name="gameTime">Game time in seconds when the event occurred (e.g. Time.time).</param>
        public void LogEvent(string description, float gameTime)
        {
            if (string.IsNullOrWhiteSpace(description)) return;

            // Evict oldest when at capacity.
            if (_events.Count >= _maxEvents)
                _events.RemoveAt(0);

            _events.Add(new MatchEventEntry(gameTime, description));
            _onEventLogged?.Raise();
        }

        /// <summary>
        /// Clears all entries without firing any event.
        /// Safe to call at match start or from bootstrap context.
        /// </summary>
        public void Reset()
        {
            _events.Clear();
        }
    }
}
