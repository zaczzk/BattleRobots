using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Row component for a single entry in the match-history scroll list.
    /// Populated by <see cref="MatchHistoryUI"/> via <see cref="Populate"/>.
    ///
    /// Architecture:
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Read-only display — no wallet or SO mutations.
    /// </summary>
    public sealed class MatchHistoryEntryUI : MonoBehaviour
    {
        [SerializeField] private Text _resultLabel;
        [SerializeField] private Text _dateLabel;
        [SerializeField] private Text _durationLabel;
        [SerializeField] private Text _earningsLabel;
        [SerializeField] private Text _damageLabel;
        [SerializeField] private Image _backgroundImage;

        [Header("Row Colours")]
        [SerializeField] private Color _winColour  = new Color(0.18f, 0.6f, 0.18f, 0.25f);
        [SerializeField] private Color _lossColour = new Color(0.7f,  0.1f, 0.1f,  0.25f);

        /// <summary>Fills all labels from a <see cref="MatchRecord"/>.</summary>
        public void Populate(MatchRecord record)
        {
            if (_resultLabel   != null) _resultLabel.text   = record.playerWon ? "WIN"  : "LOSS";
            if (_dateLabel     != null) _dateLabel.text     = FormatTimestamp(record.timestamp);
            if (_durationLabel != null) _durationLabel.text = $"{record.durationSeconds:F0}s";
            if (_earningsLabel != null) _earningsLabel.text = record.playerWon
                ? $"+{record.currencyEarned} cr"
                : "—";
            if (_damageLabel != null)
                _damageLabel.text = $"↑{record.damageDone:F0}  ↓{record.damageTaken:F0}";

            if (_backgroundImage != null)
                _backgroundImage.color = record.playerWon ? _winColour : _lossColour;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Converts an ISO-8601 UTC string to a short local-time display string.
        /// Falls back to the raw string if parsing fails.
        /// </summary>
        private static string FormatTimestamp(string iso)
        {
            if (string.IsNullOrEmpty(iso)) return string.Empty;

            if (System.DateTime.TryParse(iso,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out System.DateTime dt))
            {
                return dt.ToLocalTime().ToString("MM/dd HH:mm");
            }

            return iso;
        }
    }
}
