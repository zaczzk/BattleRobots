using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// In-match HUD controller that drives a MM:SS countdown label and an optional
    /// warning panel from a <see cref="MatchClockSO"/>.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   Awake      → cache delegates.
    ///   OnEnable   → subscribe _onMatchStarted → HandleMatchStarted;
    ///                subscribe _onMatchEnded   → HandleMatchEnded;
    ///                Refresh().
    ///   OnDisable  → unsubscribe both.
    ///   Update     → Tick(Time.deltaTime).
    ///
    ///   HandleMatchStarted(): _clockSO?.StartClock().
    ///   HandleMatchEnded():   _clockSO?.StopClock().
    ///
    ///   Tick(dt):
    ///     • No-op when _clockSO null.
    ///     • Forwards dt to _clockSO.Tick(dt).
    ///     • Calls Refresh() to update the label and warning panel.
    ///
    ///   Refresh():
    ///     • Formats TimeRemaining as "MM:SS" and assigns to _timerLabel.
    ///     • Shows _warningPanel when MatchClockSO.WarningFired is true.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one clock HUD per canvas.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   _clockSO        → MatchClockSO asset (provides clock state).
    ///   _onMatchStarted → shared match-start VoidGameEvent.
    ///   _onMatchEnded   → shared match-end VoidGameEvent.
    ///   _timerLabel     → Text showing time in "MM:SS" format.
    ///   _warningPanel   → Panel shown when time is critically low.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchClockHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("MatchClockSO that provides the authoritative countdown state.")]
        [SerializeField] private MatchClockSO _clockSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("UI References (optional)")]
        [Tooltip("Text label displaying the remaining time in MM:SS format.")]
        [SerializeField] private Text _timerLabel;

        [Tooltip("Panel shown when MatchClockSO.WarningFired becomes true " +
                 "(e.g. flashing red overlay when time is low).")]
        [SerializeField] private GameObject _warningPanel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _startDelegate;
        private Action _endDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _startDelegate = HandleMatchStarted;
            _endDelegate   = HandleMatchEnded;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_startDelegate);
            _onMatchEnded?.RegisterCallback(_endDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_startDelegate);
            _onMatchEnded?.UnregisterCallback(_endDelegate);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Starts the underlying <see cref="MatchClockSO"/> countdown.
        /// Wired to <c>_onMatchStarted</c>.
        /// No-op when <c>_clockSO</c> is null.
        /// </summary>
        public void HandleMatchStarted()
        {
            _clockSO?.StartClock();
        }

        /// <summary>
        /// Stops the underlying <see cref="MatchClockSO"/> countdown.
        /// Wired to <c>_onMatchEnded</c>.
        /// No-op when <c>_clockSO</c> is null.
        /// </summary>
        public void HandleMatchEnded()
        {
            _clockSO?.StopClock();
        }

        /// <summary>
        /// Advances the clock by <paramref name="dt"/> seconds and refreshes the HUD.
        /// No-op when <c>_clockSO</c> is null.
        /// Zero allocation — delegates all arithmetic to MatchClockSO.
        /// </summary>
        public void Tick(float dt)
        {
            if (_clockSO == null) return;

            _clockSO.Tick(dt);
            Refresh();
        }

        /// <summary>
        /// Updates <c>_timerLabel</c> with the remaining time in "MM:SS" format and
        /// shows/hides <c>_warningPanel</c> based on <see cref="MatchClockSO.WarningFired"/>.
        /// No-op for null UI refs.
        /// </summary>
        public void Refresh()
        {
            if (_clockSO == null) return;

            if (_timerLabel != null)
            {
                float remaining = _clockSO.TimeRemaining;
                int   minutes   = Mathf.FloorToInt(remaining / 60f);
                int   seconds   = Mathf.FloorToInt(remaining % 60f);
                _timerLabel.text = $"{minutes:D2}:{seconds:D2}";
            }

            if (_warningPanel != null)
                _warningPanel.SetActive(_clockSO.WarningFired);
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="MatchClockSO"/>. May be null.</summary>
        public MatchClockSO ClockSO => _clockSO;

        /// <summary>True when the underlying clock is running.</summary>
        public bool IsClockRunning => _clockSO?.IsRunning ?? false;
    }
}
