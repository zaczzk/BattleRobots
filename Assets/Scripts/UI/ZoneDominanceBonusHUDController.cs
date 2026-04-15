using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays a dominance bonus banner when the player holds
    /// a majority of arena zones, optionally showing the active score multiplier value.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _bonusPanel      → activated while HasDominance; hidden when dominance lost
    ///   _statusLabel     → "Dominance Bonus Active!" / "No Dominance"
    ///   _multiplierLabel → "×N.Nx" sourced from ScoreMultiplierSO (optional)
    ///   _panel           → root container; hidden when _dominanceSO is null
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes reactively to _onDominanceChanged.
    ///   - All UI refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one controller per HUD panel.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _dominanceSO       → ZoneDominanceSO asset.
    ///   2. Assign _onDominanceChanged→ same VoidGameEvent as ZoneDominanceSO._onDominanceChanged.
    ///   3. Assign _scoreMultiplier   → ScoreMultiplierSO (optional — shows "×N.Nx" label).
    ///   4. Assign optional UI Text and panel refs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneDominanceBonusHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneDominanceSO  _dominanceSO;
        [SerializeField] private ScoreMultiplierSO _scoreMultiplier;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onDominanceChanged;

        [Header("UI Refs (optional)")]
        [SerializeField] private GameObject _bonusPanel;
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _multiplierLabel;
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
            _onDominanceChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onDominanceChanged?.UnregisterCallback(_refreshDelegate);
            _panel?.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the HUD from the current dominance and multiplier state.
        /// Hides the panel when <c>_dominanceSO</c> is null.
        /// Zero allocation after Awake.
        /// </summary>
        public void Refresh()
        {
            if (_dominanceSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            bool hasDominance = _dominanceSO.HasDominance;
            _bonusPanel?.SetActive(hasDominance);

            if (_statusLabel != null)
                _statusLabel.text = hasDominance ? "Dominance Bonus Active!" : "No Dominance";

            if (_multiplierLabel != null)
            {
                float mult = _scoreMultiplier != null ? _scoreMultiplier.Multiplier : 1f;
                _multiplierLabel.text = $"\u00d7{mult:F1}x";
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ZoneDominanceSO"/>. May be null.</summary>
        public ZoneDominanceSO DominanceSO => _dominanceSO;

        /// <summary>The assigned <see cref="ScoreMultiplierSO"/>. May be null.</summary>
        public ScoreMultiplierSO ScoreMultiplier => _scoreMultiplier;
    }
}
