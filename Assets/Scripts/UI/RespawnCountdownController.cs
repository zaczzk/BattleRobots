using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// HUD controller that displays respawn availability and countdown from a
    /// <see cref="RobotRespawnSO"/>.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   _onRespawnUsed  fires → Refresh() shows countdown / remaining count.
    ///   _onRespawnReady fires → Refresh() shows "Ready" and updated remaining count.
    ///   Update(): while IsOnCooldown, ticks the SO timer and calls Refresh().
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   _respawnSO             → the RobotRespawnSO tracking respawn state.
    ///   _onRespawnUsed         → VoidGameEvent from RobotRespawnSO._onRespawnUsed.
    ///   _onRespawnReady        → VoidGameEvent from RobotRespawnSO._onRespawnReady.
    ///   _panel                 → Root panel; hidden when _respawnSO is null.
    ///   _countdownLabel        → Shows "Respawn in X.Xs" or "Ready".
    ///   _respawnsRemainingLabel→ Shows "N respawns left".
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - Delegate cached in Awake; zero heap allocations per event firing.
    ///   - Update only calls RobotRespawnSO.Tick() while IsOnCooldown (early-exit otherwise).
    ///   - DisallowMultipleComponent — one respawn HUD per canvas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RespawnCountdownController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("RobotRespawnSO tracking respawn count and cooldown state.")]
        [SerializeField] private RobotRespawnSO _respawnSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised by RobotRespawnSO when a respawn slot is consumed.")]
        [SerializeField] private VoidGameEvent _onRespawnUsed;

        [Tooltip("Raised by RobotRespawnSO when the cooldown timer reaches zero.")]
        [SerializeField] private VoidGameEvent _onRespawnReady;

        [Header("UI Refs (optional)")]
        [Tooltip("Root panel; hidden when _respawnSO is null.")]
        [SerializeField] private GameObject _panel;

        [Tooltip("Shows 'Respawn in X.Xs' while on cooldown, or 'Ready' when available.")]
        [SerializeField] private Text _countdownLabel;

        [Tooltip("Shows 'N respawns left'.")]
        [SerializeField] private Text _respawnsRemainingLabel;

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
            if (_respawnSO == null || !_respawnSO.IsOnCooldown) return;
            _respawnSO.Tick(Time.deltaTime);
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current <see cref="RobotRespawnSO"/> state and updates all UI labels.
        /// Hides the panel when <c>_respawnSO</c> is null. Fully null-safe.
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
                    ? string.Format("Respawn in {0:F1}s", _respawnSO.CooldownTimer)
                    : "Ready";
            }

            if (_respawnsRemainingLabel != null)
            {
                _respawnsRemainingLabel.text =
                    string.Format("{0} respawns left", _respawnSO.RespawnsRemaining);
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="RobotRespawnSO"/>. May be null.</summary>
        public RobotRespawnSO RespawnSO => _respawnSO;
    }
}
