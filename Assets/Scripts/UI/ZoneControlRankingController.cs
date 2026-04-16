using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that evaluates and displays the player's zone-control rank.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _rankLabel          → Human-readable rank title (e.g. "Gold").
    ///   _nextThresholdLabel → "Next rank at N zones" or "Max rank reached!".
    ///   _panel              → Root panel; hidden when <c>_rankingSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes to <c>_onGateUnlocked</c> to trigger rank re-evaluation.
    ///   - Subscribes to <c>_onRankChanged</c> for reactive refresh.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one rank panel per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _rankingSO  → ZoneControlRankingSO asset.
    ///   2. Assign _summarySO  → ZoneControlSessionSummarySO asset.
    ///   3. Assign _gateSO     → ZoneControlProgressionGateSO asset.
    ///   4. Assign _onGateUnlocked  → gateSO._onGateUnlocked channel.
    ///   5. Assign _onRankChanged   → rankingSO._onRankChanged channel.
    ///   6. Assign _rankLabel, _nextThresholdLabel, and _panel UI refs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlRankingController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlRankingSO           _rankingSO;
        [SerializeField] private ZoneControlSessionSummarySO    _summarySO;
        [SerializeField] private ZoneControlProgressionGateSO   _gateSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to ZoneControlProgressionGateSO._onGateUnlocked.")]
        [SerializeField] private VoidGameEvent _onGateUnlocked;

        [Tooltip("Wire to ZoneControlRankingSO._onRankChanged.")]
        [SerializeField] private VoidGameEvent _onRankChanged;

        [Header("UI Refs (optional)")]
        [Tooltip("Shows the player's current rank title.")]
        [SerializeField] private Text        _rankLabel;

        [Tooltip("Shows the zone count for the next rank, or 'Max rank reached!'.")]
        [SerializeField] private Text        _nextThresholdLabel;

        [SerializeField] private GameObject  _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleGateUnlockedDelegate;
        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleGateUnlockedDelegate = HandleGateUnlocked;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onGateUnlocked?.RegisterCallback(_handleGateUnlockedDelegate);
            _onRankChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onGateUnlocked?.UnregisterCallback(_handleGateUnlockedDelegate);
            _onRankChanged?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates the current rank from session summary and gate data, then
        /// refreshes the HUD. Called automatically when <c>_onGateUnlocked</c> fires.
        /// </summary>
        public void HandleGateUnlocked()
        {
            if (_rankingSO != null && _summarySO != null)
            {
                int unlockedTiers = _gateSO != null ? _gateSO.UnlockedTiers : 0;
                _rankingSO.EvaluateRank(_summarySO.TotalZonesCaptured, unlockedTiers);
            }
            Refresh();
        }

        /// <summary>
        /// Rebuilds all HUD elements from the current ranking SO state.
        /// Hides the panel when <c>_rankingSO</c> is null.
        /// Zero allocation after Awake.
        /// </summary>
        public void Refresh()
        {
            if (_rankingSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_rankLabel != null)
                _rankLabel.text = _rankingSO.GetRankLabel();

            if (_nextThresholdLabel != null)
            {
                int next = _rankingSO.GetNextZoneThreshold();
                _nextThresholdLabel.text = next < 0
                    ? "Max rank reached!"
                    : $"Next rank at {next} zones";
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound ranking SO (may be null).</summary>
        public ZoneControlRankingSO RankingSO => _rankingSO;

        /// <summary>The bound session summary SO (may be null).</summary>
        public ZoneControlSessionSummarySO SummarySO => _summarySO;

        /// <summary>The bound progression gate SO (may be null).</summary>
        public ZoneControlProgressionGateSO GateSO => _gateSO;
    }
}
