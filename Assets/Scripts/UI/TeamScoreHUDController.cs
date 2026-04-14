using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// HUD controller that displays team vs team scores from a <see cref="TeamScoreSO"/>.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   AddTeamAScore / AddTeamBScore / ResetScores mutate TeamScoreSO.
    ///   TeamScoreSO raises _onScoreChanged
    ///   ──► TeamScoreHUDController.Refresh() reads scores and updates labels.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   _teamScore       → TeamScoreSO tracking the live scores.
    ///   _onScoreChanged  → VoidGameEvent raised by TeamScoreSO._onScoreChanged.
    ///   _teamALabel      → Text showing "A: N".
    ///   _teamBLabel      → Text showing "B: N".
    ///   _leadLabel       → Text showing "Team A Leads" / "Team B Leads" / "Tie".
    ///   _panel           → Root panel; activated when a valid TeamScoreSO is assigned.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • No Update / FixedUpdate — purely event-driven.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///   • DisallowMultipleComponent — one score HUD per canvas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TeamScoreHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("TeamScoreSO tracking the live scores for both teams.")]
        [SerializeField] private TeamScoreSO _teamScore;

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent raised by TeamScoreSO._onScoreChanged.")]
        [SerializeField] private VoidGameEvent _onScoreChanged;

        [Header("UI References (optional)")]
        [Tooltip("Text showing Team A's score, e.g. 'A: 3'.")]
        [SerializeField] private Text _teamALabel;

        [Tooltip("Text showing Team B's score, e.g. 'B: 1'.")]
        [SerializeField] private Text _teamBLabel;

        [Tooltip("Text showing which team leads, e.g. 'Team A Leads' or 'Tie'.")]
        [SerializeField] private Text _leadLabel;

        [Tooltip("Root panel; activated when a valid TeamScoreSO is assigned.")]
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
            _onScoreChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onScoreChanged?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current <see cref="TeamScoreSO"/> state and updates all UI labels.
        /// Hides the panel when <c>_teamScore</c> is null.
        /// Fully null-safe.
        /// </summary>
        public void Refresh()
        {
            if (_teamScore == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_teamALabel != null)
                _teamALabel.text = string.Format("A: {0}", _teamScore.TeamAScore);

            if (_teamBLabel != null)
                _teamBLabel.text = string.Format("B: {0}", _teamScore.TeamBScore);

            if (_leadLabel != null)
            {
                switch (_teamScore.LeadingTeam)
                {
                    case "A":   _leadLabel.text = "Team A Leads"; break;
                    case "B":   _leadLabel.text = "Team B Leads"; break;
                    default:    _leadLabel.text = "Tie";          break;
                }
            }
        }

        /// <summary>The assigned <see cref="TeamScoreSO"/>. May be null.</summary>
        public TeamScoreSO TeamScore => _teamScore;
    }
}
