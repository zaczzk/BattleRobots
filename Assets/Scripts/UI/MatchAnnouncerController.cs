using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// MonoBehaviour that displays a full-screen announcer banner for major match moments
    /// (critical hit, maximum momentum, sudden death, match start/end).
    ///
    /// ── Data flow ──────────────────────────────────────────────────────────────
    ///   Awake     → caches one Action delegate per event channel.
    ///   OnEnable  → subscribes all configured VoidGameEvent channels.
    ///   OnDisable → unsubscribes all channels.
    ///   Update    → calls Tick(Time.deltaTime) to advance the hide timer.
    ///   ShowMessage(text) → activates the panel, sets the label, resets the timer.
    ///   Tick(dt)  → decrements _displayTimer; hides the panel when timer hits zero.
    ///
    /// ── ARCHITECTURE RULES ─────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • Subscribes only to Core VoidGameEvent SO channels.
    ///   • All delegates cached in Awake; zero heap allocations after initialisation.
    ///   • DisallowMultipleComponent — one announcer banner per canvas.
    ///
    /// Assign <c>_config</c> to a <see cref="MatchAnnouncerConfig"/> asset and wire each
    /// optional VoidGameEvent channel to the corresponding match/combat game event.
    /// Leave channels null to silence the corresponding callout.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchAnnouncerController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Config (optional)")]
        [Tooltip("MatchAnnouncerConfig asset that supplies message strings and duration. " +
                 "Leave null to disable all announcer output.")]
        [SerializeField] private MatchAnnouncerConfig _config;

        [Header("UI References (optional)")]
        [Tooltip("Text label that displays the current announcer message.")]
        [SerializeField] private Text _announcerLabel;

        [Tooltip("Root panel GameObject shown while an announcement is active; " +
                 "hidden once the timer expires.")]
        [SerializeField] private GameObject _announcerPanel;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised by CriticalHitConfig when a critical hit lands.")]
        [SerializeField] private VoidGameEvent _onCriticalHit;

        [Tooltip("Raised by MatchMomentumSO when momentum reaches its maximum.")]
        [SerializeField] private VoidGameEvent _onMomentumFull;

        [Tooltip("Raised by MatchSuddenDeathController when sudden-death activates.")]
        [SerializeField] private VoidGameEvent _onSuddenDeath;

        [Tooltip("Raised when the match countdown completes (match starts).")]
        [SerializeField] private VoidGameEvent _onMatchStart;

        [Tooltip("Raised by MatchManager when the player wins.")]
        [SerializeField] private VoidGameEvent _onPlayerWin;

        [Tooltip("Raised by MatchManager when the player loses.")]
        [SerializeField] private VoidGameEvent _onPlayerLoss;

        // ── Private state ─────────────────────────────────────────────────────

        private Action _critHitDelegate;
        private Action _momentumFullDelegate;
        private Action _suddenDeathDelegate;
        private Action _matchStartDelegate;
        private Action _playerWinDelegate;
        private Action _playerLossDelegate;

        private float _displayTimer;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _critHitDelegate     = () => ShowMessage(_config?.CritHitMessage);
            _momentumFullDelegate = () => ShowMessage(_config?.MomentumFullMessage);
            _suddenDeathDelegate  = () => ShowMessage(_config?.SuddenDeathMessage);
            _matchStartDelegate   = () => ShowMessage(_config?.MatchStartMessage);
            _playerWinDelegate    = () => ShowMessage(_config?.PlayerWinMessage);
            _playerLossDelegate   = () => ShowMessage(_config?.PlayerLossMessage);
        }

        private void OnEnable()
        {
            _onCriticalHit?.RegisterCallback(_critHitDelegate);
            _onMomentumFull?.RegisterCallback(_momentumFullDelegate);
            _onSuddenDeath?.RegisterCallback(_suddenDeathDelegate);
            _onMatchStart?.RegisterCallback(_matchStartDelegate);
            _onPlayerWin?.RegisterCallback(_playerWinDelegate);
            _onPlayerLoss?.RegisterCallback(_playerLossDelegate);
        }

        private void OnDisable()
        {
            _onCriticalHit?.UnregisterCallback(_critHitDelegate);
            _onMomentumFull?.UnregisterCallback(_momentumFullDelegate);
            _onSuddenDeath?.UnregisterCallback(_suddenDeathDelegate);
            _onMatchStart?.UnregisterCallback(_matchStartDelegate);
            _onPlayerWin?.UnregisterCallback(_playerWinDelegate);
            _onPlayerLoss?.UnregisterCallback(_playerLossDelegate);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Displays <paramref name="message"/> on the announcer banner and resets the
        /// hide timer to <c>_config.MessageDuration</c>.
        /// No-op when <paramref name="message"/> is null or empty, or when
        /// <see cref="_config"/> is null (prevents showing a blank banner).
        /// Zero allocation — string reference, no boxing.
        /// </summary>
        public void ShowMessage(string message)
        {
            if (_config == null) return;
            if (string.IsNullOrEmpty(message)) return;

            if (_announcerLabel != null)
                _announcerLabel.text = message;

            _announcerPanel?.SetActive(true);
            _displayTimer = _config.MessageDuration;
        }

        /// <summary>
        /// Advances the display timer by <paramref name="deltaTime"/> seconds.
        /// Hides the announcer panel when the timer reaches zero.
        /// Called from <c>Update</c>; also called directly in EditMode tests.
        /// Zero allocation — float arithmetic only.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_displayTimer <= 0f) return;

            _displayTimer -= deltaTime;

            if (_displayTimer <= 0f)
            {
                _displayTimer = 0f;
                _announcerPanel?.SetActive(false);
            }
        }

        // ── Public API (diagnostics) ──────────────────────────────────────────

        /// <summary>Seconds remaining before the banner auto-hides. Zero when hidden.</summary>
        public float DisplayTimer => _displayTimer;

        /// <summary>The config asset currently assigned (may be null).</summary>
        public MatchAnnouncerConfig Config => _config;
    }
}
