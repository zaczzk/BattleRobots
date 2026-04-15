using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that bridges zone capture / loss events into a
    /// <see cref="ZoneCaptureStreakSO"/> consecutive-capture counter.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   _onZoneCaptured  → HandleZoneCaptured() → <see cref="ZoneCaptureStreakSO.IncrementStreak"/>
    ///   _onZoneLost      → HandleZoneLost()     → <see cref="ZoneCaptureStreakSO.ResetStreak"/>
    ///   _onMatchStarted  → HandleMatchStarted() → <see cref="ZoneCaptureStreakSO.Reset"/>
    ///   _onMatchEnded    → HandleMatchEnded()   → <see cref="ZoneCaptureStreakSO.Reset"/>
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace — no Physics / UI dependencies.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one streak controller per scene.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_streakSO</c>         → a ZoneCaptureStreakSO asset.
    ///   2. Assign <c>_onZoneCaptured</c>   → VoidGameEvent raised on each player capture.
    ///   3. Assign <c>_onZoneLost</c>       → VoidGameEvent raised when the player loses a zone.
    ///   4. Assign <c>_onMatchStarted</c>   → shared match-start VoidGameEvent.
    ///   5. Assign <c>_onMatchEnded</c>     → shared match-end VoidGameEvent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneCaptureStreakController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("The ZoneCaptureStreakSO that accumulates the consecutive capture count.")]
        [SerializeField] private ZoneCaptureStreakSO _streakSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onZoneLost;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _capturedDelegate;
        private Action _lostDelegate;
        private Action _startDelegate;
        private Action _endDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _capturedDelegate = HandleZoneCaptured;
            _lostDelegate     = HandleZoneLost;
            _startDelegate    = HandleMatchStarted;
            _endDelegate      = HandleMatchEnded;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_capturedDelegate);
            _onZoneLost?.RegisterCallback(_lostDelegate);
            _onMatchStarted?.RegisterCallback(_startDelegate);
            _onMatchEnded?.RegisterCallback(_endDelegate);
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_capturedDelegate);
            _onZoneLost?.UnregisterCallback(_lostDelegate);
            _onMatchStarted?.UnregisterCallback(_startDelegate);
            _onMatchEnded?.UnregisterCallback(_endDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Increments the streak. Wired to <c>_onZoneCaptured</c>.</summary>
        public void HandleZoneCaptured()
        {
            _streakSO?.IncrementStreak();
        }

        /// <summary>Resets the streak to zero. Wired to <c>_onZoneLost</c>.</summary>
        public void HandleZoneLost()
        {
            _streakSO?.ResetStreak();
        }

        /// <summary>Silently resets the streak. Wired to <c>_onMatchStarted</c>.</summary>
        public void HandleMatchStarted()
        {
            _streakSO?.Reset();
        }

        /// <summary>Silently resets the streak. Wired to <c>_onMatchEnded</c>.</summary>
        public void HandleMatchEnded()
        {
            _streakSO?.Reset();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ZoneCaptureStreakSO"/>. May be null.</summary>
        public ZoneCaptureStreakSO StreakSO => _streakSO;
    }
}
