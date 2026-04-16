using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays the player's zone-capture progression tier
    /// and the zone count required to unlock the next gate tier.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _tiersLabel          → "Tier: N"               (unlocked / total)
    ///   _nextThresholdLabel  → "Next: N zones"          or "Max Tier!"
    ///   _progressLabel       → "N / M"                  (captured / next threshold)
    ///   _panel               → Root panel; hidden when _gateSO is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes to _onSummaryUpdated to trigger EvaluateGates + Refresh.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one progression gate panel per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _gateSO       → ZoneControlProgressionGateSO asset.
    ///   2. Assign _summarySO    → ZoneControlSessionSummarySO asset.
    ///   3. Assign _onSummaryUpdated → shared SummaryUpdated VoidGameEvent.
    ///   4. Optionally assign _onGateUnlocked for additional notifications.
    ///   5. Assign labels and _panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlProgressionGateController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlProgressionGateSO   _gateSO;
        [SerializeField] private ZoneControlSessionSummarySO    _summarySO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to the shared SummaryUpdated VoidGameEvent.")]
        [SerializeField] private VoidGameEvent _onSummaryUpdated;
        [Tooltip("Wire to the gate's own GateUnlocked VoidGameEvent for additional reactions.")]
        [SerializeField] private VoidGameEvent _onGateUnlocked;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text       _tiersLabel;
        [SerializeField] private Text       _nextThresholdLabel;
        [SerializeField] private Text       _progressLabel;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onSummaryUpdated?.RegisterCallback(_refreshDelegate);
            _onGateUnlocked?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onSummaryUpdated?.UnregisterCallback(_refreshDelegate);
            _onGateUnlocked?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates progression gates and rebuilds all labels.
        /// Hides the panel when <c>_gateSO</c> is null.
        /// Zero allocation after Awake.
        /// </summary>
        public void Refresh()
        {
            if (_gateSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            int totalCaptured = _summarySO != null ? _summarySO.TotalZonesCaptured : 0;
            _gateSO.EvaluateGates(totalCaptured);

            if (_tiersLabel != null)
                _tiersLabel.text = $"Tier: {_gateSO.UnlockedTiers}";

            if (_nextThresholdLabel != null)
            {
                _nextThresholdLabel.text = _gateSO.AllUnlocked
                    ? "Max Tier!"
                    : $"Next: {_gateSO.NextThreshold} zones";
            }

            if (_progressLabel != null)
            {
                string next = _gateSO.AllUnlocked
                    ? _gateSO.GateCount.ToString()
                    : _gateSO.NextThreshold.ToString();
                _progressLabel.text = $"{totalCaptured} / {next}";
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound progression gate SO (may be null).</summary>
        public ZoneControlProgressionGateSO GateSO => _gateSO;

        /// <summary>The bound session summary SO (may be null).</summary>
        public ZoneControlSessionSummarySO SummarySO => _summarySO;
    }
}
