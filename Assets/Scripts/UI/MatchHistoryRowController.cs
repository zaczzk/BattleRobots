using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Drives a single row in the match history list UI.
    ///
    /// Each row displays: outcome (WIN / LOSS), match duration (MM:SS),
    /// currency earned, and the date the match was played.
    ///
    /// Populated by <see cref="MatchHistoryController.PopulateHistory"/>.
    /// All text references are optional — omit any you don't need in the prefab.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — no Physics refs.
    ///   • <see cref="Setup"/> is called once per row; no Update overhead.
    ///   • Static string constants avoid repeated string allocations.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchHistoryRowController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Displays 'WIN' or 'LOSS'.")]
        [SerializeField] private Text _outcomeText;

        [Tooltip("Displays match duration formatted as MM:SS.")]
        [SerializeField] private Text _durationText;

        [Tooltip("Displays currency earned, e.g. '+200' or '+50'.")]
        [SerializeField] private Text _rewardText;

        [Tooltip("Displays the local date the match was played, e.g. 'Apr 10, 2026'.")]
        [SerializeField] private Text _dateText;

        // ── Static labels (avoids per-row string allocation) ──────────────────

        private const string WinLabel  = "WIN";
        private const string LossLabel = "LOSS";

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Populate all text fields from the supplied <see cref="MatchRecord"/>.
        /// Call this immediately after <see cref="Instantiate"/> before the row is
        /// first rendered; subsequent field updates are not supported (one record → one row).
        /// </summary>
        public void Setup(MatchRecord record)
        {
            if (record == null) return;

            if (_outcomeText  != null)
                _outcomeText.text  = record.playerWon ? WinLabel : LossLabel;

            if (_durationText != null)
                _durationText.text = FormatDuration(record.durationSeconds);

            if (_rewardText   != null)
                _rewardText.text   = record.currencyEarned > 0
                    ? "+" + record.currencyEarned
                    : record.currencyEarned.ToString();

            if (_dateText     != null)
                _dateText.text     = FormatTimestamp(record.timestamp);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static string FormatDuration(float seconds)
        {
            int totalSec = Mathf.Max(0, Mathf.RoundToInt(seconds));
            int m = totalSec / 60;
            int s = totalSec % 60;
            return $"{m:D2}:{s:D2}";
        }

        private static string FormatTimestamp(string isoTimestamp)
        {
            if (string.IsNullOrEmpty(isoTimestamp)) return "--";

            // Parse ISO-8601 from MatchRecord.timestamp ("o" format in MatchManager).
            // Fall back to the raw string if parsing fails (older save data).
            if (DateTime.TryParse(isoTimestamp, null, DateTimeStyles.RoundtripKind,
                    out DateTime dt))
                return dt.ToLocalTime().ToString("MMM d, yyyy", CultureInfo.InvariantCulture);

            return isoTimestamp;
        }
    }
}
