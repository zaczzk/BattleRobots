using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// In-match HUD controller that visualises progress toward a single in-match
    /// objective driven by an <see cref="InMatchObjectiveSO"/>.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate (Action).
    ///   OnEnable  → subscribes _onProgressChanged → Refresh(); calls Refresh().
    ///   OnDisable → unsubscribes.
    ///   Refresh() → reads InMatchObjectiveSO:
    ///                 • _objectivePanel hidden when _objective is null
    ///                 • _titleLabel.text = ObjectiveTitle
    ///                 • _progressLabel.text = "N / M" format
    ///                 • _progressBar.value = Progress
    ///                 • _completeLabel shown when IsComplete; hidden otherwise
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • No Update / FixedUpdate — event-driven via VoidGameEvent channel.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///   • DisallowMultipleComponent — one objective panel per HUD canvas.
    ///
    /// Scene wiring:
    ///   _objective          → InMatchObjectiveSO defining the goal and tracking progress.
    ///   _onProgressChanged  → VoidGameEvent raised by InMatchObjectiveSO on each mutation.
    ///   _objectivePanel     → Root panel; hidden entirely when _objective is null.
    ///   _titleLabel         → Text showing ObjectiveTitle.
    ///   _progressLabel      → Text showing "N / M" current vs target count.
    ///   _progressBar        → Slider whose value = Progress [0, 1].
    ///   _completeLabel      → Text shown when the objective is complete; hidden otherwise.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InMatchObjectiveController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("InMatchObjectiveSO defining and tracking the current match objective.")]
        [SerializeField] private InMatchObjectiveSO _objective;

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent raised by InMatchObjectiveSO on Increment and Reset.")]
        [SerializeField] private VoidGameEvent _onProgressChanged;

        [Header("UI References (optional)")]
        [Tooltip("Root panel for the objective HUD; hidden when _objective is null.")]
        [SerializeField] private GameObject _objectivePanel;

        [Tooltip("Text showing the objective's title, e.g. 'Destroy Parts'.")]
        [SerializeField] private Text _titleLabel;

        [Tooltip("Text showing current vs target count, e.g. '2 / 5'.")]
        [SerializeField] private Text _progressLabel;

        [Tooltip("Slider whose value maps to Progress [0, 1].")]
        [SerializeField] private Slider _progressBar;

        [Tooltip("Text or label shown only when IsComplete == true; hidden otherwise.")]
        [SerializeField] private Text _completeLabel;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onProgressChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onProgressChanged?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current <see cref="InMatchObjectiveSO"/> state and updates all UI elements.
        /// Fully null-safe.
        /// </summary>
        public void Refresh()
        {
            bool hasObjective = _objective != null;

            _objectivePanel?.SetActive(hasObjective);

            if (!hasObjective) return;

            if (_titleLabel != null)
                _titleLabel.text = _objective.ObjectiveTitle;

            if (_progressLabel != null)
                _progressLabel.text = string.Format("{0} / {1}",
                    _objective.CurrentCount, _objective.TargetCount);

            if (_progressBar != null)
                _progressBar.value = _objective.Progress;

            if (_completeLabel != null)
                _completeLabel.gameObject.SetActive(_objective.IsComplete);
        }

        /// <summary>The assigned <see cref="InMatchObjectiveSO"/>. May be null.</summary>
        public InMatchObjectiveSO Objective => _objective;
    }
}
