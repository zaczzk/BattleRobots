using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays the per-zone capture cooldown state from a
    /// <see cref="ZoneTimerSO"/> as a countdown bar, a timer label, and a locked badge.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _cooldownBar → Slider.value = CooldownRatio  [1 = just started → 0 = done]
    ///   _timerLabel  → "{remaining:F1}s"
    ///   _lockedBadge → activated while IsOnCooldown; hidden when cooldown expires
    ///   _panel       → shown only while IsOnCooldown; hidden at cooldown end
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - _onCooldownStarted → show panel + Refresh.
    ///   - _onCooldownEnded   → hide panel.
    ///   - Update ticks the timer every frame while the panel is visible.
    ///   - All UI refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one controller per zone HUD entry.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _timerSO          → ZoneTimerSO asset (one per zone).
    ///   2. Assign _onCooldownStarted→ ZoneTimerSO._onCooldownStarted VoidGameEvent.
    ///   3. Assign _onCooldownEnded  → ZoneTimerSO._onCooldownEnded VoidGameEvent.
    ///   4. Assign optional _cooldownBar, _timerLabel, _lockedBadge, _panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneTimerHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneTimerSO _timerSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onCooldownStarted;
        [SerializeField] private VoidGameEvent _onCooldownEnded;

        [Header("UI Refs (optional)")]
        [SerializeField] private Slider     _cooldownBar;
        [SerializeField] private Text       _timerLabel;
        [SerializeField] private GameObject _lockedBadge;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _showDelegate;
        private Action _hideDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _showDelegate = HandleCooldownStarted;
            _hideDelegate = HandleCooldownEnded;
        }

        private void OnEnable()
        {
            _onCooldownStarted?.RegisterCallback(_showDelegate);
            _onCooldownEnded?.RegisterCallback(_hideDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onCooldownStarted?.UnregisterCallback(_showDelegate);
            _onCooldownEnded?.UnregisterCallback(_hideDelegate);
            _panel?.SetActive(false);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void HandleCooldownStarted()
        {
            _panel?.SetActive(true);
            Refresh();
        }

        private void HandleCooldownEnded()
        {
            _panel?.SetActive(false);
            _lockedBadge?.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Advances the UI every frame while the cooldown is running.
        /// Public for EditMode test driving. Zero allocation.
        /// </summary>
        public void Tick(float dt)
        {
            if (_timerSO == null || !_timerSO.IsOnCooldown) return;
            Refresh();
        }

        /// <summary>
        /// Rebuilds the HUD from the current cooldown state.
        /// Shows/hides based on <see cref="ZoneTimerSO.IsOnCooldown"/>.
        /// Zero allocation after Awake.
        /// </summary>
        public void Refresh()
        {
            if (_timerSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            bool onCooldown = _timerSO.IsOnCooldown;
            _panel?.SetActive(onCooldown);
            _lockedBadge?.SetActive(onCooldown);

            if (_cooldownBar != null)
                _cooldownBar.value = _timerSO.CooldownRatio;

            if (_timerLabel != null)
                _timerLabel.text = $"{_timerSO.RemainingCooldown:F1}s";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ZoneTimerSO"/>. May be null.</summary>
        public ZoneTimerSO TimerSO => _timerSO;
    }
}
