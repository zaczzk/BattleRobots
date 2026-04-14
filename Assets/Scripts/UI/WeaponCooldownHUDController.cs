using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// In-match HUD controller that visualises the cooldown state of a single weapon slot.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate (Action) and _tickDelegate (Action).
    ///   OnEnable  → subscribes _onCooldownChanged → Refresh(); calls Refresh().
    ///   Update    → calls _cooldownSO?.Tick(Time.deltaTime) to advance the timer.
    ///   OnDisable → unsubscribes; hides cooldown overlay.
    ///   Refresh() → reads WeaponCooldownSO:
    ///                 • _cooldownBar.value = CooldownRatio
    ///                 • _cooldownLabel.text = "N.Ns" when on cooldown; cleared when ready
    ///                 • _readyLabel active when NOT on cooldown
    ///                 • _cooldownOverlay shown when IsOnCooldown
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • Update contains only a null-check and a Tick call (zero allocation).
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///   • DisallowMultipleComponent — one cooldown HUD per weapon slot.
    ///
    /// Scene wiring:
    ///   _cooldownSO        → WeaponCooldownSO asset for this weapon.
    ///   _onCooldownChanged → VoidGameEvent fired by WeaponCooldownSO.
    ///   _cooldownBar       → Slider whose value maps to CooldownRatio [0,1].
    ///   _cooldownLabel     → Text showing remaining seconds ("N.Ns") while cooling down.
    ///   _readyLabel        → Text or panel shown when weapon is ready (not on cooldown).
    ///   _cooldownOverlay   → GameObject shown while IsOnCooldown.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponCooldownHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("WeaponCooldownSO tracking the cooldown for this weapon slot.")]
        [SerializeField] private WeaponCooldownSO _cooldownSO;

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent raised by WeaponCooldownSO on StartCooldown and each Tick.")]
        [SerializeField] private VoidGameEvent _onCooldownChanged;

        [Header("UI References (optional)")]
        [Tooltip("Slider whose value = CooldownRatio (1 = just fired; 0 = ready).")]
        [SerializeField] private Slider _cooldownBar;

        [Tooltip("Text showing remaining cooldown seconds, e.g. '1.5s'. Cleared when ready.")]
        [SerializeField] private Text _cooldownLabel;

        [Tooltip("Text or label shown only when the weapon is ready (not on cooldown).")]
        [SerializeField] private Text _readyLabel;

        [Tooltip("Overlay GameObject shown while the weapon is on cooldown.")]
        [SerializeField] private GameObject _cooldownOverlay;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onCooldownChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onCooldownChanged?.UnregisterCallback(_refreshDelegate);
            _cooldownOverlay?.SetActive(false);
        }

        private void Update()
        {
            _cooldownSO?.Tick(Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current <see cref="WeaponCooldownSO"/> state and updates all UI elements.
        /// Fully null-safe.
        /// </summary>
        public void Refresh()
        {
            bool onCooldown = _cooldownSO != null && _cooldownSO.IsOnCooldown;

            if (_cooldownBar != null)
                _cooldownBar.value = _cooldownSO != null ? _cooldownSO.CooldownRatio : 0f;

            if (_cooldownLabel != null)
            {
                _cooldownLabel.text = onCooldown
                    ? string.Format("{0:F1}s", _cooldownSO.RemainingCooldown)
                    : string.Empty;
            }

            if (_readyLabel != null)
                _readyLabel.gameObject.SetActive(!onCooldown);

            _cooldownOverlay?.SetActive(onCooldown);
        }

        /// <summary>The assigned <see cref="WeaponCooldownSO"/>. May be null.</summary>
        public WeaponCooldownSO CooldownSO => _cooldownSO;
    }
}
