using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that evaluates capture bonuses at match end and displays the
    /// running total in a HUD label.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _bonusLabel → "Total Bonus: N" (updated after each EvaluateBonus call).
    ///   _panel      → Root panel; shown when <c>_bonusSO</c> is assigned.
    ///                 Hidden when <c>_bonusSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one bonus panel per HUD.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureBonusController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureBonusSO    _bonusSO;
        [SerializeField] private PlayerWallet                  _wallet;
        [SerializeField] private ZoneControlSessionSummarySO  _summarySO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised at match end; triggers bonus evaluation and wallet credit.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Tooltip("Raised by ZoneControlCaptureBonusSO after a bonus is awarded; refreshes HUD.")]
        [SerializeField] private VoidGameEvent _onBonusAwarded;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text       _bonusLabel;
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
            _onBonusAwarded?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onBonusAwarded?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates the capture bonus for the current session, credits the wallet,
        /// and refreshes the HUD.
        /// </summary>
        public void HandleMatchEnded()
        {
            if (_bonusSO == null) { Refresh(); return; }

            int captureCount = _summarySO != null ? _summarySO.TotalZonesCaptured : 0;
            int bonus        = _bonusSO.EvaluateBonus(captureCount);
            if (bonus > 0)
                _wallet?.AddFunds(bonus);

            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the HUD label from the current total bonus.
        /// Hides the panel when <c>_bonusSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_bonusSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);
            if (_bonusLabel != null)
                _bonusLabel.text = $"Total Bonus: {_bonusSO.TotalBonusAwarded}";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound capture bonus SO (may be null).</summary>
        public ZoneControlCaptureBonusSO BonusSO => _bonusSO;
    }
}
