using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays the player vs. enemy zone-control score from a
    /// <see cref="ZoneScoreTrackerSO"/> as labels, a pair of Sliders, and a total
    /// points label.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _playerScoreLabel → "P: N"   (player zone score, rounded)
    ///   _enemyScoreLabel  → "E: N"   (enemy zone score, rounded)
    ///   _totalLabel       → "N pts"  (combined total, rounded)
    ///   _playerBar        → Slider.value = player / total (0.5 when total == 0)
    ///   _enemyBar         → Slider.value = 1 − player / total
    ///   _panel            → activated on every Refresh; hidden when tracker is null
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes to two channels: _onScoreUpdated (refresh) and _onMatchEnded
    ///     (reset tracker + refresh).
    ///   - All UI refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one controller per HUD panel.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_tracker</c>         → the ZoneScoreTrackerSO asset.
    ///   2. Assign <c>_onScoreUpdated</c>  → ZoneScoreTrackerSO._onScoreUpdated channel.
    ///   3. Assign <c>_onMatchEnded</c>    → shared match-ended VoidGameEvent.
    ///   4. Assign optional UI Text, Slider, and panel references.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneScoreHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneScoreTrackerSO _tracker;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onScoreUpdated;
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text   _playerScoreLabel;
        [SerializeField] private Text   _enemyScoreLabel;
        [SerializeField] private Text   _totalLabel;
        [SerializeField] private Slider _playerBar;
        [SerializeField] private Slider _enemyBar;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _refreshDelegate;
        private Action _matchEndedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate    = Refresh;
            _matchEndedDelegate = HandleMatchEnded;
        }

        private void OnEnable()
        {
            _onScoreUpdated?.RegisterCallback(_refreshDelegate);
            _onMatchEnded?.RegisterCallback(_matchEndedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onScoreUpdated?.UnregisterCallback(_refreshDelegate);
            _onMatchEnded?.UnregisterCallback(_matchEndedDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the HUD from the current tracker state.
        /// Hides the panel when <c>_tracker</c> is null.
        /// Zero allocation after Awake.
        /// </summary>
        public void Refresh()
        {
            if (_tracker == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            float p     = _tracker.PlayerScore;
            float e     = _tracker.EnemyScore;
            float total = _tracker.TotalScore;

            if (_playerScoreLabel != null)
                _playerScoreLabel.text = $"P: {Mathf.RoundToInt(p)}";

            if (_enemyScoreLabel != null)
                _enemyScoreLabel.text = $"E: {Mathf.RoundToInt(e)}";

            if (_totalLabel != null)
                _totalLabel.text = $"{Mathf.RoundToInt(total)} pts";

            float ratio = total > 0f ? Mathf.Clamp01(p / total) : 0.5f;

            if (_playerBar != null)
                _playerBar.value = ratio;

            if (_enemyBar != null)
                _enemyBar.value = 1f - ratio;
        }

        /// <summary>
        /// Resets the tracker (zeros scores) then refreshes the HUD.
        /// Wired to <c>_onMatchEnded</c>.
        /// </summary>
        public void HandleMatchEnded()
        {
            _tracker?.Reset();
            Refresh();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ZoneScoreTrackerSO"/>. May be null.</summary>
        public ZoneScoreTrackerSO Tracker => _tracker;
    }
}
