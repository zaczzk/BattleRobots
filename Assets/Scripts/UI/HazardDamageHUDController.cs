using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Post-match panel that shows a breakdown of environment damage received
    /// from arena hazard zones, read from <see cref="HazardDamageTrackerSO"/>.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   <c>_onMatchEnded</c> → Refresh().
    ///   Refresh reads <see cref="HazardDamageTrackerSO"/> and sets:
    ///     • <c>_totalDamageLabel</c>        — "Total Hazard: N"
    ///     • <c>_mostFrequentTypeLabel</c>    — "Most Frequent: TypeName" or "—"
    ///     • Per-type labels                  — "Lava: N", "Electric: N", etc.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - Delegate cached in Awake; zero heap allocations after initialisation.
    ///   - DisallowMultipleComponent — one hazard-damage HUD per canvas.
    ///   - Panel hides in OnDisable and when _tracker is null on Refresh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HazardDamageHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("HazardDamageTrackerSO populated by HazardZoneController during the match.")]
        [SerializeField] private HazardDamageTrackerSO _tracker;

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent raised by MatchManager at match end. Triggers Refresh.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("UI References (optional)")]
        [Tooltip("Root panel; shown when tracker has data, hidden otherwise.")]
        [SerializeField] private GameObject _panel;

        [Tooltip("Label showing the total hazard damage, e.g. 'Total Hazard: 45'.")]
        [SerializeField] private Text _totalDamageLabel;

        [Tooltip("Label showing the most-hit hazard type, e.g. 'Most Frequent: Lava'.")]
        [SerializeField] private Text _mostFrequentTypeLabel;

        [Tooltip("Label showing damage from Lava zones.")]
        [SerializeField] private Text _lavaDamageLabel;

        [Tooltip("Label showing damage from Electric zones.")]
        [SerializeField] private Text _electricDamageLabel;

        [Tooltip("Label showing damage from Spikes zones.")]
        [SerializeField] private Text _spikesDamageLabel;

        [Tooltip("Label showing damage from Acid zones.")]
        [SerializeField] private Text _acidDamageLabel;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_refreshDelegate);
            _panel?.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads <see cref="HazardDamageTrackerSO"/> and updates all UI labels.
        /// Hides the panel when <c>_tracker</c> is null.
        /// Fully null-safe.
        /// </summary>
        public void Refresh()
        {
            if (_tracker == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_totalDamageLabel != null)
                _totalDamageLabel.text =
                    string.Format("Total Hazard: {0}",
                        Mathf.RoundToInt(_tracker.GetTotalDamage()));

            if (_mostFrequentTypeLabel != null)
            {
                HazardZoneType? most = _tracker.GetMostFrequentType();
                _mostFrequentTypeLabel.text = most.HasValue
                    ? string.Format("Most Frequent: {0}", most.Value)
                    : "Most Frequent: \u2014";
            }

            SetTypeLabel(_lavaDamageLabel,     HazardZoneType.Lava);
            SetTypeLabel(_electricDamageLabel, HazardZoneType.Electric);
            SetTypeLabel(_spikesDamageLabel,   HazardZoneType.Spikes);
            SetTypeLabel(_acidDamageLabel,     HazardZoneType.Acid);
        }

        /// <summary>The assigned <see cref="HazardDamageTrackerSO"/>. May be null.</summary>
        public HazardDamageTrackerSO Tracker => _tracker;

        // ── Private helpers ───────────────────────────────────────────────────

        private void SetTypeLabel(Text label, HazardZoneType type)
        {
            if (label == null) return;
            label.text = string.Format("{0}: {1}", type,
                Mathf.RoundToInt(_tracker.GetDamageForType(type)));
        }
    }
}
