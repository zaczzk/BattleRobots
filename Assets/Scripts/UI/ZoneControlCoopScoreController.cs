using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives the cooperative zone-score HUD.
    /// Subscribes to player and ally capture events, updates the shared
    /// <see cref="ZoneControlCoopScoreSO"/>, and refreshes the display.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _playerCaptureLabel → "Player: N".
    ///   _allyCaptureLabel   → "Ally: N".
    ///   _totalLabel         → "Total: N".
    ///   _milestoneBadge     → Active when the shared milestone has been reached.
    ///   _panel              → Root panel; hidden when <c>_coopScoreSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one coop-score panel per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _coopScoreSO          → ZoneControlCoopScoreSO asset.
    ///   2. Assign _onPlayerZoneCaptured → event raised when the player captures.
    ///   3. Assign _onAllyZoneCaptured   → event raised when the ally captures.
    ///   4. Assign _onCoopScoreUpdated   → coopScoreSO._onCoopScoreUpdated channel.
    ///   5. Assign _onMilestoneReached   → coopScoreSO._onMilestoneReached channel.
    ///   6. Assign label / badge / panel UI refs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlCoopScoreController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCoopScoreSO _coopScoreSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised when the player captures a zone.")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;

        [Tooltip("Raised when the ally captures a zone.")]
        [SerializeField] private VoidGameEvent _onAllyZoneCaptured;

        [Tooltip("Wire to ZoneControlCoopScoreSO._onCoopScoreUpdated.")]
        [SerializeField] private VoidGameEvent _onCoopScoreUpdated;

        [Tooltip("Wire to ZoneControlCoopScoreSO._onMilestoneReached.")]
        [SerializeField] private VoidGameEvent _onMilestoneReached;

        [Header("UI Refs — Labels (optional)")]
        [SerializeField] private Text _playerCaptureLabel;
        [SerializeField] private Text _allyCaptureLabel;
        [SerializeField] private Text _totalLabel;

        [Header("UI Refs — Badges / Panel (optional)")]
        [SerializeField] private GameObject _milestoneBadge;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handlePlayerCaptureDelegate;
        private Action _handleAllyCaptureDelegate;
        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handlePlayerCaptureDelegate = HandlePlayerCapture;
            _handleAllyCaptureDelegate   = HandleAllyCapture;
            _refreshDelegate             = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCaptureDelegate);
            _onAllyZoneCaptured?.RegisterCallback(_handleAllyCaptureDelegate);
            _onCoopScoreUpdated?.RegisterCallback(_refreshDelegate);
            _onMilestoneReached?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCaptureDelegate);
            _onAllyZoneCaptured?.UnregisterCallback(_handleAllyCaptureDelegate);
            _onCoopScoreUpdated?.UnregisterCallback(_refreshDelegate);
            _onMilestoneReached?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>Records a player capture on the SO and refreshes the HUD.</summary>
        public void HandlePlayerCapture()
        {
            _coopScoreSO?.AddPlayerCapture();
            Refresh();
        }

        /// <summary>Records an ally capture on the SO and refreshes the HUD.</summary>
        public void HandleAllyCapture()
        {
            _coopScoreSO?.AddAllyCapture();
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds all HUD elements from the current coop-score SO state.
        /// Hides the panel when <c>_coopScoreSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_coopScoreSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_playerCaptureLabel != null)
                _playerCaptureLabel.text = $"Player: {_coopScoreSO.PlayerCaptures}";

            if (_allyCaptureLabel != null)
                _allyCaptureLabel.text = $"Ally: {_coopScoreSO.AllyCaptures}";

            if (_totalLabel != null)
                _totalLabel.text = $"Total: {_coopScoreSO.TotalCaptures}";

            _milestoneBadge?.SetActive(_coopScoreSO.MilestoneReached);
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound coop-score SO (may be null).</summary>
        public ZoneControlCoopScoreSO CoopScoreSO => _coopScoreSO;
    }
}
