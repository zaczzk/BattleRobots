using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives the zone-control challenge tracker HUD.
    /// Listens for the match-ended event, evaluates all catalog challenges against the
    /// session summary, and refreshes the active / completed counts display.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _activeLabel    → "Active: N".
    ///   _completedLabel → "Completed: N".
    ///   _panel          → Root panel; hidden when <c>_catalogSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one challenge tracker per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _catalogSO          → ZoneControlChallengeCatalogSO asset.
    ///   2. Assign _summarySO          → ZoneControlSessionSummarySO asset.
    ///   3. Assign _onMatchEnded       → a shared VoidGameEvent raised at match end.
    ///   4. Assign _onCatalogUpdated   → catalogSO._onCatalogUpdated channel.
    ///   5. Assign label / panel UI refs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlChallengeTrackerController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlChallengeCatalogSO _catalogSO;
        [SerializeField] private ZoneControlSessionSummarySO   _summarySO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised at match end; triggers challenge evaluation.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Tooltip("Wire to ZoneControlChallengeCatalogSO._onCatalogUpdated for reactive refresh.")]
        [SerializeField] private VoidGameEvent _onCatalogUpdated;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text _activeLabel;
        [SerializeField] private Text _completedLabel;

        [Header("UI Refs — Panel (optional)")]
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleMatchEndedDelegate;
        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleMatchEndedDelegate = HandleMatchEnded;
            _refreshDelegate          = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onCatalogUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onCatalogUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates all catalog challenges against the current session summary,
        /// then refreshes the HUD.
        /// No-op when <c>_catalogSO</c> is null.
        /// </summary>
        public void HandleMatchEnded()
        {
            _catalogSO?.EvaluateAll(_summarySO);
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the active and completed count labels.
        /// Hides the panel when <c>_catalogSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_catalogSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_activeLabel != null)
                _activeLabel.text = $"Active: {_catalogSO.ActiveCount}";

            if (_completedLabel != null)
                _completedLabel.text = $"Completed: {_catalogSO.CompletedCount}";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound challenge catalog SO (may be null).</summary>
        public ZoneControlChallengeCatalogSO CatalogSO => _catalogSO;

        /// <summary>The bound session summary SO (may be null).</summary>
        public ZoneControlSessionSummarySO SummarySO => _summarySO;
    }
}
