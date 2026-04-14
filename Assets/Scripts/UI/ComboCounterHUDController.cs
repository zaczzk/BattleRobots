using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// In-match HUD controller that visualises the player's current combo streak
    /// driven by a <see cref="ComboCounterSO"/>.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate (Action).
    ///   OnEnable  → subscribes _onComboChanged → Refresh(); calls Refresh().
    ///   Update    → calls _comboCounter?.Tick(Time.deltaTime) to advance the window timer.
    ///   OnDisable → unsubscribes; hides _comboPanel.
    ///   Refresh() → reads ComboCounterSO:
    ///                 • _comboPanel hidden when IsComboActive == false
    ///                 • _comboCountLabel.text = "N hits" (active) or "—" (inactive)
    ///                 • _multiplierLabel.text = "×1.5" format
    ///                 • _maxComboLabel.text = "Best: N"
    ///                 • _windowBar.value = ComboWindowRatio [0, 1]
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • Update contains only a null-check and a Tick call (zero allocation).
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///   • DisallowMultipleComponent — one combo HUD per canvas.
    ///
    /// Scene wiring:
    ///   _comboCounter    → ComboCounterSO tracking the player's hit combo.
    ///   _onComboChanged  → VoidGameEvent fired by ComboCounterSO on each hit or break.
    ///   _comboPanel      → Root panel shown only when a combo is active.
    ///   _comboCountLabel → Text showing "N hits" while combo is active.
    ///   _multiplierLabel → Text showing "×1.5" current score multiplier.
    ///   _maxComboLabel   → Text showing "Best: N" all-time-high this match.
    ///   _windowBar       → Slider whose value = ComboWindowRatio [0, 1].
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ComboCounterHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("ComboCounterSO tracking the player's hit combo this match.")]
        [SerializeField] private ComboCounterSO _comboCounter;

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent raised by ComboCounterSO on every RecordHit and combo break.")]
        [SerializeField] private VoidGameEvent _onComboChanged;

        [Header("UI References (optional)")]
        [Tooltip("Root panel shown only when a combo is active (IsComboActive == true).")]
        [SerializeField] private GameObject _comboPanel;

        [Tooltip("Text displaying the current hit count, e.g. '7 hits'.")]
        [SerializeField] private Text _comboCountLabel;

        [Tooltip("Text displaying the current score multiplier, e.g. '×1.2'.")]
        [SerializeField] private Text _multiplierLabel;

        [Tooltip("Text displaying the highest combo reached this match, e.g. 'Best: 12'.")]
        [SerializeField] private Text _maxComboLabel;

        [Tooltip("Slider whose value maps to ComboWindowRatio [0, 1] " +
                 "(1 = window just reset; 0 = window expired / inactive).")]
        [SerializeField] private Slider _windowBar;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onComboChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onComboChanged?.UnregisterCallback(_refreshDelegate);
            _comboPanel?.SetActive(false);
        }

        private void Update()
        {
            _comboCounter?.Tick(Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current <see cref="ComboCounterSO"/> state and updates all UI elements.
        /// Fully null-safe.
        /// </summary>
        public void Refresh()
        {
            bool isActive = _comboCounter != null && _comboCounter.IsComboActive;

            _comboPanel?.SetActive(isActive);

            if (_comboCountLabel != null)
            {
                _comboCountLabel.text = isActive
                    ? string.Format("{0} hits", _comboCounter.HitCount)
                    : "\u2014"; // em-dash
            }

            if (_multiplierLabel != null)
            {
                float mult = _comboCounter != null ? _comboCounter.ComboMultiplier : 1f;
                _multiplierLabel.text = string.Format("\u00d7{0:F1}", mult); // ×N.N
            }

            if (_maxComboLabel != null)
            {
                int max = _comboCounter != null ? _comboCounter.MaxCombo : 0;
                _maxComboLabel.text = string.Format("Best: {0}", max);
            }

            if (_windowBar != null)
                _windowBar.value = _comboCounter != null ? _comboCounter.ComboWindowRatio : 0f;
        }

        /// <summary>The assigned <see cref="ComboCounterSO"/>. May be null.</summary>
        public ComboCounterSO ComboCounter => _comboCounter;
    }
}
