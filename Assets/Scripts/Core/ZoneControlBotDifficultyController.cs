using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that periodically calls
    /// <see cref="ZoneControlScoreboardSO.RecordBotCapture"/> for bot index 0,
    /// scaling the capture rate per wave from a
    /// <see cref="ZoneControlBotDifficultyProfileSO"/>.
    ///
    /// ── Flow ────────────────────────────────────────────────────────────────────
    ///   • <c>_onWaveStarted</c>  → reads CurrentWave from <c>_waveManagerSO</c>,
    ///     recomputes the interval, and arms the timer.
    ///   • <c>Update</c>          → accumulates dt; fires RecordBotCapture when the
    ///     interval elapses, then resets the accumulator.
    ///   • <c>_onMatchEnded</c>   → disarms the timer.
    ///   • <c>_onMatchStarted</c> → disarms the timer (clean state at match start).
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no UI references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one bot-difficulty driver per scene.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _profileSO       → ZoneControlBotDifficultyProfileSO asset.
    ///   2. Assign _scoreboardSO    → ZoneControlScoreboardSO asset.
    ///   3. Assign _waveManagerSO   → WaveManagerSO asset (provides CurrentWave).
    ///   4. Assign _onWaveStarted   → VoidGameEvent raised at each wave start.
    ///   5. Assign _onMatchStarted / _onMatchEnded channels.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlBotDifficultyController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlBotDifficultyProfileSO _profileSO;
        [SerializeField] private ZoneControlScoreboardSO           _scoreboardSO;
        [SerializeField] private WaveManagerSO                     _waveManagerSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised when a new wave begins; triggers interval recalculation.")]
        [SerializeField] private VoidGameEvent _onWaveStarted;

        [Tooltip("Raised at match start; disarms the capture timer.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Tooltip("Raised at match end; disarms the capture timer.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleWaveStartedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMatchEndedDelegate;

        // ── Runtime state ─────────────────────────────────────────────────────

        private float _currentInterval;
        private float _elapsed;
        private bool  _isRunning;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleWaveStartedDelegate  = HandleWaveStarted;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleMatchEndedDelegate   = HandleMatchEnded;
        }

        private void OnEnable()
        {
            _onWaveStarted?.RegisterCallback(_handleWaveStartedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
        }

        private void OnDisable()
        {
            _onWaveStarted?.UnregisterCallback(_handleWaveStartedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _isRunning = false;
        }

        private void Update()
        {
            if (!_isRunning || _currentInterval <= 0f) return;

            _elapsed += Time.deltaTime;
            if (_elapsed >= _currentInterval)
            {
                _elapsed = 0f;
                _scoreboardSO?.RecordBotCapture(0);
            }
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current wave from <see cref="WaveManagerSO"/>, computes the
        /// new capture interval from the profile, and arms the accumulation timer.
        /// </summary>
        public void HandleWaveStarted()
        {
            int wave = _waveManagerSO != null ? _waveManagerSO.CurrentWave : 0;
            _currentInterval = _profileSO != null
                ? _profileSO.GetCaptureInterval(wave)
                : 0f;
            _elapsed    = 0f;
            _isRunning  = _currentInterval > 0f;
        }

        /// <summary>Disarms the capture timer at match start.</summary>
        public void HandleMatchStarted()
        {
            _elapsed   = 0f;
            _isRunning = false;
        }

        /// <summary>Disarms the capture timer at match end.</summary>
        public void HandleMatchEnded()
        {
            _isRunning = false;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True while the bot capture timer is active.</summary>
        public bool IsRunning => _isRunning;

        /// <summary>Current computed capture interval (seconds).</summary>
        public float CurrentInterval => _currentInterval;

        /// <summary>Elapsed seconds since the last bot capture.</summary>
        public float Elapsed => _elapsed;

        /// <summary>The bound difficulty profile (may be null).</summary>
        public ZoneControlBotDifficultyProfileSO ProfileSO => _profileSO;
    }
}
