using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// In-match HUD controller that visualises a charge-up ability bar driven by
    /// an <see cref="AbilityChargeSO"/>.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate (Action).
    ///   OnEnable  → subscribes _onChargeChanged → Refresh(); calls Refresh().
    ///   OnDisable → unsubscribes; hides _readyOverlay.
    ///   Refresh() → reads AbilityChargeSO:
    ///                 • _chargeBar.value = ChargeRatio
    ///                 • _chargeLabel.text = "READY!" when full; "N%" when charging
    ///                 • _readyOverlay shown when IsFullyCharged
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • No Update / FixedUpdate — event-driven via VoidGameEvent channel.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///   • DisallowMultipleComponent — one ability charge HUD per canvas.
    ///
    /// Scene wiring:
    ///   _chargeSO        → AbilityChargeSO tracking the ability charge state.
    ///   _onChargeChanged → VoidGameEvent raised by AbilityChargeSO on every mutation.
    ///   _chargeBar       → Slider whose value = ChargeRatio [0, 1].
    ///   _chargeLabel     → Text showing "READY!" when full or "N%" when charging.
    ///   _readyOverlay    → GameObject shown when the ability is fully charged.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AbilityChargeHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("AbilityChargeSO tracking the ability charge state.")]
        [SerializeField] private AbilityChargeSO _chargeSO;

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent raised by AbilityChargeSO on AddCharge, Activate, and Reset.")]
        [SerializeField] private VoidGameEvent _onChargeChanged;

        [Header("UI References (optional)")]
        [Tooltip("Slider whose value maps to ChargeRatio [0, 1].")]
        [SerializeField] private Slider _chargeBar;

        [Tooltip("Text showing 'READY!' when the ability is fully charged, or 'N%' otherwise.")]
        [SerializeField] private Text _chargeLabel;

        [Tooltip("Overlay GameObject shown only when the ability is fully charged and ready.")]
        [SerializeField] private GameObject _readyOverlay;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onChargeChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onChargeChanged?.UnregisterCallback(_refreshDelegate);
            _readyOverlay?.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current <see cref="AbilityChargeSO"/> state and updates all UI elements.
        /// Fully null-safe.
        /// </summary>
        public void Refresh()
        {
            bool isReady = _chargeSO != null && _chargeSO.IsFullyCharged;
            float ratio  = _chargeSO != null ? _chargeSO.ChargeRatio : 0f;

            if (_chargeBar != null)
                _chargeBar.value = ratio;

            if (_chargeLabel != null)
            {
                _chargeLabel.text = isReady
                    ? "READY!"
                    : string.Format("{0}%", Mathf.RoundToInt(ratio * 100f));
            }

            _readyOverlay?.SetActive(isReady);
        }

        /// <summary>The assigned <see cref="AbilityChargeSO"/>. May be null.</summary>
        public AbilityChargeSO ChargeSO => _chargeSO;
    }
}
