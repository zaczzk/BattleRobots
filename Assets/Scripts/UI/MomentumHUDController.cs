using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// MonoBehaviour that drives a momentum HUD widget from <see cref="MatchMomentumSO"/> state.
    ///
    /// ── Data flow ──────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate (Action).
    ///   OnEnable  → subscribes _onMomentumChanged → Refresh(); calls Refresh() immediately.
    ///   Update    → calls _momentumSO.Tick(Time.deltaTime) to advance passive decay.
    ///   Refresh() → reads MatchMomentumSO; hides panel when null;
    ///               otherwise sets _momentumBar.value (MomentumRatio),
    ///               _momentumLabel.text ("current / max"), and shows _momentumPanel.
    ///   OnDisable → unsubscribes _onMomentumChanged.
    ///
    /// ── ARCHITECTURE RULES ─────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///   • Update contains no heap allocations (Tick is pure float arithmetic).
    ///   • DisallowMultipleComponent — one momentum HUD per canvas.
    ///
    /// Pair with a MatchMomentumSO asset and wire AddMomentum to kill/crit events
    /// so the bar reacts to combat events in real time.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MomentumHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("The MatchMomentumSO runtime blackboard. Leave null to hide the panel.")]
        [SerializeField] private MatchMomentumSO _momentumSO;

        [Header("Event Channel (optional)")]
        [Tooltip("VoidGameEvent fired by MatchMomentumSO on every change. " +
                 "Wiring this allows reactive refresh without polling.")]
        [SerializeField] private VoidGameEvent _onMomentumChanged;

        [Header("UI References (optional)")]
        [Tooltip("Slider whose value is set to MomentumRatio each refresh.")]
        [SerializeField] private Slider _momentumBar;

        [Tooltip("Text label showing 'current / max' momentum integers.")]
        [SerializeField] private Text _momentumLabel;

        [Tooltip("Root panel GameObject; hidden when no SO is assigned.")]
        [SerializeField] private GameObject _momentumPanel;

        // ── Private state ─────────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onMomentumChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMomentumChanged?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            _momentumSO?.Tick(Time.deltaTime);
        }

        // ── UI logic ──────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current <see cref="MatchMomentumSO"/> state and pushes it to the UI.
        /// Hides the panel when no SO is assigned; shows it otherwise.
        /// Allocation-free — Mathf.RoundToInt is a pure integer conversion.
        /// </summary>
        public void Refresh()
        {
            if (_momentumSO == null)
            {
                Hide();
                return;
            }

            _momentumPanel?.SetActive(true);

            if (_momentumBar != null)
                _momentumBar.value = _momentumSO.MomentumRatio;

            if (_momentumLabel != null)
                _momentumLabel.text =
                    $"{Mathf.RoundToInt(_momentumSO.Momentum)}/{Mathf.RoundToInt(_momentumSO.MaxMomentum)}";
        }

        /// <summary>Hides the momentum panel. Called when SO is null.</summary>
        private void Hide()
        {
            _momentumPanel?.SetActive(false);
        }
    }
}
