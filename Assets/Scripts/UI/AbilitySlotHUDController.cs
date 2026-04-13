using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// MonoBehaviour that drives a single ability slot in the in-match HUD.
    ///
    /// ── Data flow ────────────────────────────────────────────────────────────
    ///   OnEnable   → subscribes channels + Refresh().
    ///   Refresh()  → sets ability name label, energy-cost label, energy-availability overlay.
    ///   OnActivated → starts a local cooldown timer; sets cooldown slider to 0.
    ///   Update()   → ticks the cooldown slider from 0→1 using a local end-time
    ///                (zero heap allocations — only float arithmetic + Slider.value assign).
    ///   OnDisable  → unsubscribes all channels.
    ///
    /// ── Cooldown visualisation ────────────────────────────────────────────────
    ///   When the ability is activated (_onAbilityActivated fires), this controller
    ///   captures the cooldown duration from _ability.CooldownDuration and sets an
    ///   internal end-time. Each Update drives _cooldownSlider.value from 0 to 1
    ///   over that duration. No Physics reference is needed — the ability SO carries
    ///   the duration data.
    ///
    /// ── ARCHITECTURE RULES ───────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • All physics data consumed via Core SOs (PartAbilitySO, EnergySystemSO).
    ///   • Delegate cached in Awake; zero allocations after initialisation.
    ///   • Update allocates nothing — float reads and Slider.value assign only.
    ///   • All fields optional and null-guarded.
    ///
    /// ── Scene wiring ─────────────────────────────────────────────────────────
    ///   1. Assign _ability → the PartAbilitySO for this slot.
    ///   2. Assign _energySystem → the robot's EnergySystemSO.
    ///   3. Assign _onAbilityActivated / _onAbilityFailed / _onEnergyChanged channels.
    ///   4. Wire up UI refs: _abilityNameText, _energyCostText, _cooldownSlider,
    ///      _cooldownOverlay (visible while on cooldown), _unavailableOverlay
    ///      (visible while energy is insufficient).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AbilitySlotHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Ability definition — provides name and energy cost for label display " +
                 "and cooldown duration for the cooldown timer.")]
        [SerializeField] private PartAbilitySO _ability;

        [Tooltip("Robot's energy pool — used to compute whether the ability can be afforded.")]
        [SerializeField] private EnergySystemSO _energySystem;

        [Header("Event Channels In")]
        [Tooltip("Raised by AbilityController when TryActivate succeeds. Starts cooldown timer.")]
        [SerializeField] private VoidGameEvent _onAbilityActivated;

        [Tooltip("Raised by AbilityController or AbilityInputController when activation fails.")]
        [SerializeField] private VoidGameEvent _onAbilityFailed;

        [Tooltip("Raised by EnergySystemSO on every energy change. Refreshes availability overlay.")]
        [SerializeField] private VoidGameEvent _onEnergyChanged;

        [Header("UI Refs")]
        [Tooltip("Text label for the ability's display name.")]
        [SerializeField] private Text _abilityNameText;

        [Tooltip("Text label for the ability's energy cost (rounded integer).")]
        [SerializeField] private Text _energyCostText;

        [Tooltip("Slider that visualises cooldown progress from 0 (just activated) to 1 (ready).")]
        [SerializeField] private Slider _cooldownSlider;

        [Tooltip("GameObject shown while the ability is on cooldown (e.g. a dark overlay).")]
        [SerializeField] private GameObject _cooldownOverlay;

        [Tooltip("GameObject shown while energy is insufficient to activate the ability.")]
        [SerializeField] private GameObject _unavailableOverlay;

        // ── Runtime state (not serialized) ────────────────────────────────────

        private float _cooldownEndTime;
        private float _cooldownDuration;
        private bool  _isOnCooldown;

        // ── Cached delegates ─────────────────────────────────────────────────

        private Action _refreshDelegate;
        private Action _activatedDelegate;
        private Action _failedDelegate;

        // ── Unity messages ────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate   = Refresh;
            _activatedDelegate = OnAbilityActivated;
            _failedDelegate    = OnAbilityFailed;
        }

        private void OnEnable()
        {
            _onAbilityActivated?.RegisterCallback(_activatedDelegate);
            _onAbilityFailed?.RegisterCallback(_failedDelegate);
            _onEnergyChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onAbilityActivated?.UnregisterCallback(_activatedDelegate);
            _onAbilityFailed?.UnregisterCallback(_failedDelegate);
            _onEnergyChanged?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            if (!_isOnCooldown) return;

            float remaining = _cooldownEndTime - Time.time;

            if (remaining <= 0f)
            {
                _isOnCooldown = false;
                if (_cooldownSlider != null) _cooldownSlider.value = 1f;
                _cooldownOverlay?.SetActive(false);
                Refresh(); // re-check availability after cooldown clears
                return;
            }

            // Lerp slider from 0→1 as remaining time counts down
            if (_cooldownSlider != null && _cooldownDuration > 0f)
                _cooldownSlider.value = 1f - (remaining / _cooldownDuration);
        }

        // ── Internal logic ────────────────────────────────────────────────────

        private void Refresh()
        {
            // Name label
            if (_abilityNameText != null)
                _abilityNameText.text = _ability != null ? _ability.AbilityName : string.Empty;

            // Cost label
            if (_energyCostText != null)
                _energyCostText.text = _ability != null
                    ? Mathf.RoundToInt(_ability.EnergyCost).ToString()
                    : string.Empty;

            // Availability overlay — shown when energy is insufficient
            bool canAfford = _energySystem != null && _ability != null &&
                             _energySystem.CurrentEnergy >= _ability.EnergyCost;
            _unavailableOverlay?.SetActive(!canAfford);
        }

        private void OnAbilityActivated()
        {
            if (_ability == null) return;

            _cooldownDuration = _ability.CooldownDuration;
            _isOnCooldown     = _cooldownDuration > 0f;
            _cooldownEndTime  = Time.time + _cooldownDuration;

            if (_cooldownSlider != null) _cooldownSlider.value = _isOnCooldown ? 0f : 1f;
            _cooldownOverlay?.SetActive(_isOnCooldown);
        }

        private void OnAbilityFailed()
        {
            // Hook for "NOT READY" feedback — subscribers handle visuals externally
            // via the _onAbilityFailed event channel.  No state change here.
        }
    }
}
