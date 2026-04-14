using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// HUD controller that displays respawn countdown and remaining-respawns status
    /// from a <see cref="RobotRespawnSO"/>.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────────
    ///   Update ticks RobotRespawnSO.Tick(dt).
    ///   _onRespawnUsed / _onRespawnReady → Refresh().
    ///   Refresh reads RespawnSO state and updates labels.
    ///
    /// ── Labels ────────────────────────────────────────────────────────────────────
    ///   _countdownLabel      → "N.Ns" while on cooldown; "Ready" when available.
    ///   _respawnsRemaining   → "{N} left" (always shown when panel is visible).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - Delegate cached in Awake; zero heap allocations after initialisation.
    ///   - DisallowMultipleComponent — one respawn HUD per canvas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RespawnCountdownController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("RobotRespawnSO tracking respawn count and cooldown.")]
        [SerializeField] private RobotRespawnSO _respawnSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("VoidGameEvent raised by RobotRespawnSO when a respawn is consumed.")]
        [SerializeField] private VoidGameEvent _onRespawnUsed;

        [Tooltip("VoidGameEvent raised by RobotRespawnSO when the cooldown expires.")]
        [SerializeField] private VoidGameEvent _onRespawnReady;

        [Header("UI References (optional)")]
        [Tooltip("Text showing 'N.Ns' while on cooldown, or 'Ready' when available.")]
        [SerializeField] private Text _countdownLabel;

        [Tooltip("Text showing the number of respawns remaining, e.g. '2 left'.")]
        [SerializeField] private Text _respawnsRemainingLabel;

        [Tooltip("Root panel; activated when a valid RobotRespawnSO is assigned.")]
        [SerializeField] private GameObject _panel;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onRespawnUsed?.RegisterCallback(_refreshDelegate);
            _onRespawnReady?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onRespawnUsed?.UnregisterCallback(_refreshDelegate);
            _onRespawnReady?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            _respawnSO?.Tick(Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current <see cref="RobotRespawnSO"/> state and updates all UI labels.
        /// Hides the panel when <c>_respawnSO</c> is null.
        /// Fully null-safe.
        /// </summary>
        public void Refresh()
        {
            if (_respawnSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_countdownLabel != null)
            {
                _countdownLabel.text = _respawnSO.IsOnCooldown
                    ? string.Format("{0:F1}s", _respawnSO.CooldownTimeRemaining)
                    : "Ready";
            }

            if (_respawnsRemainingLabel != null)
                _respawnsRemainingLabel.text = string.Format("{0} left", _respawnSO.RespawnsRemaining);
        }

        /// <summary>The assigned <see cref="RobotRespawnSO"/>. May be null.</summary>
        public RobotRespawnSO RespawnSO => _respawnSO;
    }
}
