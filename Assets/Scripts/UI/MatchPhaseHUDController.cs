using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// HUD controller that displays the current match phase from a <see cref="MatchPhaseSO"/>.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   MatchManager calls MatchPhaseSO.SetPhase()
    ///   MatchPhaseSO raises _onPhaseChanged
    ///   ──► MatchPhaseHUDController.Refresh() reads PhaseLabel and updates _phaseLabel.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   _matchPhase      → MatchPhaseSO asset tracking the current phase.
    ///   _onPhaseChanged  → VoidGameEvent raised by MatchPhaseSO._onPhaseChanged.
    ///   _phaseLabel      → Text element showing the phase name (e.g. "Active").
    ///   _panel           → Root panel; activated on every Refresh when SO is assigned.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • No Update / FixedUpdate — purely event-driven.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///   • DisallowMultipleComponent — one phase indicator per HUD canvas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchPhaseHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("MatchPhaseSO tracking the current match phase.")]
        [SerializeField] private MatchPhaseSO _matchPhase;

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent raised by MatchPhaseSO._onPhaseChanged.")]
        [SerializeField] private VoidGameEvent _onPhaseChanged;

        [Header("UI References (optional)")]
        [Tooltip("Text showing the current phase label (e.g. 'Active', 'Sudden Death').")]
        [SerializeField] private Text _phaseLabel;

        [Tooltip("Root panel activated when a valid MatchPhaseSO is assigned.")]
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
            _onPhaseChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPhaseChanged?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads <see cref="MatchPhaseSO.PhaseLabel"/> and updates the HUD.
        /// When <c>_matchPhase</c> is null the label shows "—" and the panel is hidden.
        /// Fully null-safe.
        /// </summary>
        public void Refresh()
        {
            if (_matchPhase == null)
            {
                if (_phaseLabel != null) _phaseLabel.text = "\u2014"; // em-dash
                _panel?.SetActive(false);
                return;
            }

            if (_phaseLabel != null)
                _phaseLabel.text = _matchPhase.PhaseLabel;

            _panel?.SetActive(true);
        }

        /// <summary>The assigned <see cref="MatchPhaseSO"/>. May be null.</summary>
        public MatchPhaseSO MatchPhase => _matchPhase;
    }
}
