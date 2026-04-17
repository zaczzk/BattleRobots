using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that awards a season-end currency bonus to the player's
    /// wallet based on their current league division.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   On <c>_onSeasonEnded</c>: reads the current division from the bound
    ///   <see cref="ZoneControlLeagueSO"/>, calls
    ///   <see cref="ZoneControlSeasonalBonusSO.AwardBonus"/>, adds the bonus to
    ///   <see cref="PlayerWallet"/> via <c>AddFunds</c>, then refreshes the UI.
    ///   On <c>_onBonusAwarded</c>: refreshes the UI.
    ///   <see cref="Refresh"/> updates <c>_bonusLabel</c> and <c>_totalLabel</c>.
    ///   The panel is hidden when <c>_bonusSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one seasonal bonus controller per scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlSeasonalBonusController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlSeasonalBonusSO _bonusSO;
        [SerializeField] private ZoneControlLeagueSO         _leagueSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onSeasonEnded;
        [SerializeField] private VoidGameEvent _onBonusAwarded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private Text       _totalLabel;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleSeasonEndedDelegate;
        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleSeasonEndedDelegate = HandleSeasonEnded;
            _refreshDelegate           = Refresh;
        }

        private void OnEnable()
        {
            _onSeasonEnded?.RegisterCallback(_handleSeasonEndedDelegate);
            _onBonusAwarded?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onSeasonEnded?.UnregisterCallback(_handleSeasonEndedDelegate);
            _onBonusAwarded?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Awards the season-end bonus based on the current league division,
        /// credits the wallet, then refreshes the display.
        /// </summary>
        public void HandleSeasonEnded()
        {
            if (_bonusSO == null)
            {
                Refresh();
                return;
            }

            ZoneControlLeagueDivision division = _leagueSO != null
                ? _leagueSO.CurrentDivision
                : ZoneControlLeagueDivision.Bronze;

            _bonusSO.AwardBonus(division);
            _wallet?.AddFunds(_bonusSO.LastBonusAmount);
            Refresh();
        }

        // ── Display ───────────────────────────────────────────────────────────

        /// <summary>
        /// Updates the bonus and total labels.
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
                _bonusLabel.text = $"Last Bonus: {_bonusSO.LastBonusAmount}";

            if (_totalLabel != null)
                _totalLabel.text = $"Total Earned: {_bonusSO.TotalBonusAwarded}";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound bonus SO (may be null).</summary>
        public ZoneControlSeasonalBonusSO BonusSO => _bonusSO;

        /// <summary>The bound league SO (may be null).</summary>
        public ZoneControlLeagueSO LeagueSO => _leagueSO;

        /// <summary>The bound wallet (may be null).</summary>
        public PlayerWallet Wallet => _wallet;
    }
}
