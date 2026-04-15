using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays the player's consecutive zone-capture streak
    /// from a <see cref="ZoneCaptureStreakSO"/> as a streak-count label, an optional
    /// bonus badge, and an optional bonus-multiplier label.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _panel                → root container; hidden when _streakSO is null.
    ///   _streakLabel          → "Streak: N" where N = CurrentStreak.
    ///   _bonusBadge           → activated when HasBonus (streak ≥ threshold).
    ///   _bonusMultiplierLabel → "×N.N bonus" showing BonusMultiplier; only set
    ///                           when the bonus badge is active.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes reactively to _onStreakChanged for zero-delay HUD updates.
    ///   - All UI refs optional; null-safe throughout.
    ///   - Delegate cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one controller per HUD panel.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _streakSO        → the ZoneCaptureStreakSO asset.
    ///   2. Assign _onStreakChanged  → ZoneCaptureStreakSO._onStreakChanged channel.
    ///   3. Assign optional UI Text, badge, and panel references.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneCaptureStreakHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneCaptureStreakSO _streakSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onStreakChanged;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text       _streakLabel;
        [SerializeField] private GameObject _bonusBadge;
        [SerializeField] private Text       _bonusMultiplierLabel;
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
            _onStreakChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onStreakChanged?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the streak HUD from the current SO state.
        /// Hides the panel when <c>_streakSO</c> is null.
        /// Zero allocation after Awake.
        /// </summary>
        public void Refresh()
        {
            if (_streakSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_streakLabel != null)
                _streakLabel.text = $"Streak: {_streakSO.CurrentStreak}";

            bool hasBonus = _streakSO.HasBonus;
            _bonusBadge?.SetActive(hasBonus);

            if (_bonusMultiplierLabel != null)
                _bonusMultiplierLabel.text = $"\u00d7{_streakSO.BonusMultiplier:F1} bonus";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ZoneCaptureStreakSO"/>. May be null.</summary>
        public ZoneCaptureStreakSO StreakSO => _streakSO;
    }
}
