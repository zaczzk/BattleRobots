using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that shows a timed victory celebration panel when a
    /// <see cref="ZoneControlVictoryConditionSO"/> victory is achieved.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   On <c>_onVictoryAchieved</c>: reads <c>ZoneControlVictoryConditionSO.VictoryType</c>,
    ///   sets <c>_bannerLabel</c> from <see cref="ZoneControlVictoryCelebrationConfig.GetBannerText"/>,
    ///   arms the internal timer for <see cref="ZoneControlVictoryCelebrationConfig.Duration"/>
    ///   seconds, and shows <c>_panel</c>.
    ///   <c>Update</c> ticks the timer; on expiry hides <c>_panel</c> and fires
    ///   <c>_onCelebrationComplete</c>.
    ///   On <c>_onMatchStarted</c>: stops any running celebration and hides the panel.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one celebration controller per scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlVictoryCelebrationController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlVictoryCelebrationConfig _config;
        [SerializeField] private ZoneControlVictoryConditionSO        _victoryConditionSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onVictoryAchieved;
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Header("Event Channels — Out (optional)")]
        [SerializeField] private VoidGameEvent _onCelebrationComplete;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _bannerLabel;
        [SerializeField] private GameObject _panel;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool  _isRunning;
        private float _timer;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleVictoryAchievedDelegate;
        private Action _handleMatchStartedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleVictoryAchievedDelegate = HandleVictoryAchieved;
            _handleMatchStartedDelegate    = HandleMatchStarted;
        }

        private void OnEnable()
        {
            _onVictoryAchieved?.RegisterCallback(_handleVictoryAchievedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _panel?.SetActive(false);
        }

        private void OnDisable()
        {
            _onVictoryAchieved?.UnregisterCallback(_handleVictoryAchievedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
        }

        private void Update()
        {
            if (!_isRunning) return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                StopCelebration();
                _onCelebrationComplete?.Raise();
            }
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Starts the victory celebration using the victory type from the bound SO.
        /// No-op when <c>_config</c> is null.
        /// </summary>
        public void HandleVictoryAchieved()
        {
            if (_config == null) return;

            ZoneControlVictoryType type = _victoryConditionSO != null
                ? _victoryConditionSO.VictoryType
                : ZoneControlVictoryType.FirstToCaptures;

            StartCelebration(type);
        }

        /// <summary>Stops any running celebration and hides the panel.</summary>
        public void HandleMatchStarted() => StopCelebration();

        // ── Internal ──────────────────────────────────────────────────────────

        /// <summary>
        /// Arms the celebration timer, sets the banner label, and shows the panel.
        /// </summary>
        public void StartCelebration(ZoneControlVictoryType victoryType)
        {
            if (_config == null) return;

            _timer     = _config.Duration;
            _isRunning = true;

            if (_bannerLabel != null)
                _bannerLabel.text = _config.GetBannerText(victoryType);

            _panel?.SetActive(true);
        }

        /// <summary>Stops the celebration timer and hides the panel.</summary>
        public void StopCelebration()
        {
            _isRunning = false;
            _timer     = 0f;
            _panel?.SetActive(false);
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True while the celebration countdown is active.</summary>
        public bool IsRunning => _isRunning;

        /// <summary>Remaining celebration seconds.</summary>
        public float Timer => _timer;

        /// <summary>The bound config SO (may be null).</summary>
        public ZoneControlVictoryCelebrationConfig Config => _config;
    }
}
