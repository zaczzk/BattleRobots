using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Drives the in-combat HUD: round timer, player health bar, and enemy health bar.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   • Timer  : subscribes to <c>_onTimerUpdated</c> FloatGameEvent
    ///              (raised by MatchManager every Update frame while match runs).
    ///   • Health : subscribes to <c>_onPlayerHealthChanged</c> and
    ///              <c>_onEnemyHealthChanged</c> FloatGameEvents
    ///              (raised by HealthSO.ApplyDamage / Heal / Reset).
    ///   • Visibility: subscribes to the MatchStarted / MatchEnded VoidGameEvents
    ///                 to show/hide <c>_hudRoot</c>.
    ///
    /// ── Zero-alloc strategy ──────────────────────────────────────────────────
    ///   All Action delegates are cached as fields in Awake and reused across
    ///   OnEnable/OnDisable.  Timer text is rebuilt only when the displayed
    ///   integer-second value changes (≤1 string alloc/second — acceptable for UI).
    ///   Health integer text is rebuilt on each HealthChanged event (rare).
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   1. Place this component on a Canvas child that contains the HUD widgets.
    ///   2. Assign _hudRoot to that child's root GameObject.
    ///   3. Assign _timerText (UI.Text), _playerHealthSlider / _enemyHealthSlider
    ///      (UI.Slider), and optional _playerHealthText / _enemyHealthText (UI.Text).
    ///   4. Assign _playerHealth / _enemyHealth HealthSO assets so max-health is known.
    ///   5. Wire the five event channel SO fields to their matching assets:
    ///        _onMatchStarted  → MatchStarted VoidGameEvent SO
    ///        _onMatchEnded    → MatchEnded VoidGameEvent SO
    ///        _onTimerUpdated  → FloatGameEvent SO (same as MatchManager._onTimerUpdated)
    ///        _onPlayerHealthChanged → PlayerHealth HealthSO._onHealthChanged FloatGameEvent SO
    ///        _onEnemyHealthChanged  → EnemyHealth  HealthSO._onHealthChanged FloatGameEvent SO
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — no BattleRobots.Physics references.
    ///   - No Update / FixedUpdate — purely event-driven.
    /// </summary>
    public sealed class CombatHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("HUD Root")]
        [Tooltip("Root GameObject of the HUD panel. Hidden outside of a match.")]
        [SerializeField] private GameObject _hudRoot;

        [Header("Timer")]
        [Tooltip("Text element displaying MM:SS remaining.")]
        [SerializeField] private Text _timerText;

        [Header("Player Health")]
        [Tooltip("Slider showing the player's current health fraction.")]
        [SerializeField] private Slider _playerHealthSlider;

        [Tooltip("Optional text label showing integer health value.")]
        [SerializeField] private Text _playerHealthText;

        [Tooltip("Player HealthSO — needed to read MaxHealth for slider normalisation.")]
        [SerializeField] private HealthSO _playerHealth;

        [Header("Enemy Health")]
        [Tooltip("Slider showing the enemy's current health fraction.")]
        [SerializeField] private Slider _enemyHealthSlider;

        [Tooltip("Optional text label showing integer health value.")]
        [SerializeField] private Text _enemyHealthText;

        [Tooltip("Enemy HealthSO — needed to read MaxHealth for slider normalisation.")]
        [SerializeField] private HealthSO _enemyHealth;

        [Header("Event Channels — In")]
        [Tooltip("VoidGameEvent raised when a match begins (same SO as MatchManager uses).")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Tooltip("VoidGameEvent raised when a match ends.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Tooltip("FloatGameEvent raised by MatchManager each frame; payload = seconds remaining.")]
        [SerializeField] private FloatGameEvent _onTimerUpdated;

        [Tooltip("FloatGameEvent raised by the player HealthSO when health changes.")]
        [SerializeField] private FloatGameEvent _onPlayerHealthChanged;

        [Tooltip("FloatGameEvent raised by the enemy HealthSO when health changes.")]
        [SerializeField] private FloatGameEvent _onEnemyHealthChanged;

        // ── Cached delegates (zero-alloc OnEnable/OnDisable) ─────────────────

        private Action<float> _timerHandler;
        private Action<float> _playerHealthHandler;
        private Action<float> _enemyHealthHandler;
        private Action        _matchStartedHandler;
        private Action        _matchEndedHandler;

        // ── Timer dedup state ─────────────────────────────────────────────────

        private int _lastDisplayedSeconds = -1;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _timerHandler          = HandleTimerUpdated;
            _playerHealthHandler   = HandlePlayerHealthChanged;
            _enemyHealthHandler    = HandleEnemyHealthChanged;
            _matchStartedHandler   = HandleMatchStarted;
            _matchEndedHandler     = HandleMatchEnded;

            // Hide the HUD until a match begins.
            if (_hudRoot != null) _hudRoot.SetActive(false);
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_matchStartedHandler);
            _onMatchEnded?.RegisterCallback(_matchEndedHandler);
            _onTimerUpdated?.RegisterCallback(_timerHandler);
            _onPlayerHealthChanged?.RegisterCallback(_playerHealthHandler);
            _onEnemyHealthChanged?.RegisterCallback(_enemyHealthHandler);
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_matchStartedHandler);
            _onMatchEnded?.UnregisterCallback(_matchEndedHandler);
            _onTimerUpdated?.UnregisterCallback(_timerHandler);
            _onPlayerHealthChanged?.UnregisterCallback(_playerHealthHandler);
            _onEnemyHealthChanged?.UnregisterCallback(_enemyHealthHandler);
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void HandleMatchStarted()
        {
            _lastDisplayedSeconds = -1; // force timer refresh

            if (_hudRoot != null) _hudRoot.SetActive(true);

            // Prime health displays using current SO values.
            if (_playerHealth != null) HandlePlayerHealthChanged(_playerHealth.CurrentHealth);
            if (_enemyHealth  != null) HandleEnemyHealthChanged(_enemyHealth.CurrentHealth);
        }

        private void HandleMatchEnded()
        {
            if (_hudRoot != null) _hudRoot.SetActive(false);
        }

        /// <summary>
        /// Converts seconds to MM:SS and updates <c>_timerText</c>.
        /// Only rebuilds the string when the displayed second changes — caps GC at
        /// ≤1 small string allocation per second.
        /// </summary>
        private void HandleTimerUpdated(float seconds)
        {
            int totalSecs = Mathf.CeilToInt(seconds);
            if (totalSecs == _lastDisplayedSeconds) return;

            _lastDisplayedSeconds = totalSecs;

            if (_timerText == null) return;

            int mins = totalSecs / 60;
            int secs = totalSecs % 60;
            _timerText.text = string.Format("{0:00}:{1:00}", mins, secs);
        }

        private void HandlePlayerHealthChanged(float health)
        {
            float max = _playerHealth != null ? _playerHealth.MaxHealth : 1f;
            UpdateHealthWidgets(_playerHealthSlider, _playerHealthText, health, max);
        }

        private void HandleEnemyHealthChanged(float health)
        {
            float max = _enemyHealth != null ? _enemyHealth.MaxHealth : 1f;
            UpdateHealthWidgets(_enemyHealthSlider, _enemyHealthText, health, max);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Sets slider value to <c>health/max</c> and updates the optional label.
        /// Health events fire rarely (on damage/heal), so string allocation here is fine.
        /// </summary>
        private static void UpdateHealthWidgets(Slider slider, Text label, float health, float max)
        {
            if (slider != null)
                slider.value = max > 0f ? health / max : 0f;

            if (label != null)
                label.text = Mathf.CeilToInt(health).ToString();
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_onTimerUpdated == null)
                Debug.LogWarning("[CombatHUDController] _onTimerUpdated not assigned.", this);
            if (_onMatchStarted == null)
                Debug.LogWarning("[CombatHUDController] _onMatchStarted not assigned.", this);
            if (_onMatchEnded == null)
                Debug.LogWarning("[CombatHUDController] _onMatchEnded not assigned.", this);
        }
#endif
    }
}
