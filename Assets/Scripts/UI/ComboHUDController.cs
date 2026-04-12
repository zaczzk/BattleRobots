using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Drives the combo streak display on the combat HUD.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   ComboCounterSO.RecordHit() → _onComboChanged → ComboHUDController.Refresh()
    ///   ComboCounterSO.Tick()      ← ComboHUDController.Update() drives every frame
    ///   _comboFill.fillAmount      ← ComboCounterSO.ComboWindowRatio (per-frame, no alloc)
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Add this MB to any persistent Canvas GameObject in the Arena scene.
    ///   2. Assign _comboCounter → the ComboCounterSO asset (required for ticking).
    ///   3. Assign _onComboChanged → the VoidGameEvent SO assigned to
    ///      ComboCounterSO._onComboChanged. The controller subscribes on OnEnable so
    ///      Refresh() fires reactively on every hit or combo break.
    ///   4. Optionally assign visual refs (all silently skipped when null):
    ///      • _comboPanel     — root GO shown while combo is active.
    ///      • _comboCountText — Text showing "COMBO x{N}" while active.
    ///      • _multiplierText — Text showing "x{M:F1}" while active.
    ///      • _comboFill      — Image (Filled mode) whose fillAmount tracks the
    ///                          remaining combo window (1 = fresh, 0 = about to break).
    ///   5. Wire the same ComboCounterSO.Reset to the MatchStarted VoidGameEvent
    ///      (VoidGameEventListener) so the combo clears at match start.
    ///
    /// ── Architecture notes ─────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — no BattleRobots.Physics references.
    ///   - Update() is allocation-free: only float arithmetic + Image.fillAmount write.
    ///   - Text formatting uses string.Format with cached values — one alloc per
    ///     Refresh() call (event-driven, not per-frame).
    ///   - Cached Action delegate avoids per-frame closure allocation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ComboHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("State Source (required for tick)")]
        [Tooltip("ComboCounterSO blackboard. Tick(deltaTime) is called each Update() " +
                 "to advance the combo window timer. Without this, the combo never expires.")]
        [SerializeField] private ComboCounterSO _comboCounter;

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent assigned to ComboCounterSO._onComboChanged. " +
                 "Subscribing here triggers Refresh() on every hit and break, " +
                 "keeping text labels in sync without polling.")]
        [SerializeField] private VoidGameEvent _onComboChanged;

        [Header("Panel Root (optional)")]
        [Tooltip("Root GO shown when the combo is active (HitCount > 0), hidden otherwise.")]
        [SerializeField] private GameObject _comboPanel;

        [Header("Text Labels (optional)")]
        [Tooltip("Displays the current hit count, e.g. 'COMBO x7'.")]
        [SerializeField] private Text _comboCountText;

        [Tooltip("Displays the current score multiplier, e.g. 'x1.2'.")]
        [SerializeField] private Text _multiplierText;

        [Header("Fill Bar (optional)")]
        [Tooltip("Image component set to Filled mode. fillAmount is updated every frame " +
                 "to show the remaining combo window (1 = full, 0 = about to break).")]
        [SerializeField] private Image _comboFill;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshHandler;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            // Cache delegate once so RegisterCallback/UnregisterCallback compare by reference.
            _refreshHandler = Refresh;
        }

        private void OnEnable()
        {
            _onComboChanged?.RegisterCallback(_refreshHandler);
            Refresh();
        }

        private void OnDisable()
        {
            _onComboChanged?.UnregisterCallback(_refreshHandler);
            HideAll();
        }

        /// <summary>
        /// Drives the combo window timer and updates the fill bar each frame.
        /// Zero allocation: only float arithmetic and Image.fillAmount assignment.
        /// </summary>
        private void Update()
        {
            if (_comboCounter == null) return;

            _comboCounter.Tick(Time.deltaTime);

            if (_comboFill != null)
                _comboFill.fillAmount = _comboCounter.ComboWindowRatio;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Synchronises all combo indicators to the current <see cref="_comboCounter"/> state.
        /// Called automatically on each <see cref="_onComboChanged"/> event.
        /// Safe to call manually from an editor button or integration test.
        /// </summary>
        public void Refresh()
        {
            if (_comboCounter == null)
            {
                HideAll();
                return;
            }

            bool active = _comboCounter.IsComboActive;

            if (_comboPanel != null)
                _comboPanel.SetActive(active);

            if (_comboCountText != null)
                _comboCountText.text = active
                    ? string.Format("COMBO x{0}", _comboCounter.HitCount)
                    : string.Empty;

            if (_multiplierText != null)
                _multiplierText.text = active
                    ? string.Format("x{0:F1}", _comboCounter.ComboMultiplier)
                    : string.Empty;

            if (_comboFill != null)
                _comboFill.fillAmount = _comboCounter.ComboWindowRatio;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void HideAll()
        {
            if (_comboPanel     != null) _comboPanel.SetActive(false);
            if (_comboCountText != null) _comboCountText.text  = string.Empty;
            if (_multiplierText != null) _multiplierText.text  = string.Empty;
            if (_comboFill      != null) _comboFill.fillAmount = 0f;
        }
    }
}
