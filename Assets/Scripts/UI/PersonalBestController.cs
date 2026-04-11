using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays the player's match score and personal best on the post-match screen.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   Subscribes to <c>_onMatchEnded</c>.  When the event fires:
    ///     1. Reads the already-updated <see cref="PersonalBestSO"/> (MatchManager has
    ///        already called Submit() before raising _onMatchEnded).
    ///     2. Refreshes _scoreText, _bestScoreText, and _newBestPanel.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Add this component to the Arena Canvas (e.g. alongside PostMatchController).
    ///   2. Assign <c>_personalBest</c> — the shared PersonalBestSO asset.
    ///   3. Assign <c>_onMatchEnded</c> — the same VoidGameEvent as MatchManager uses.
    ///   4. Optionally assign the three UI refs for live display:
    ///        _scoreText    → "Score: 1 450"
    ///        _bestScoreText → "Best: 2 100"
    ///        _newBestPanel  → shown only when IsNewBest is true
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace.  References BattleRobots.Core only.
    ///   - Must NOT reference BattleRobots.Physics.
    ///   - No Update / FixedUpdate — purely event-driven.
    ///   - Delegate cached in Awake — zero alloc on Subscribe/Unsubscribe.
    ///   - String allocations only in Refresh() cold path (once per match).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PersonalBestController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("SO that stores the current-match and all-time best scores. " +
                 "MatchManager calls Submit() before raising MatchEnded so this SO " +
                 "already holds fresh data when HandleMatchEnded runs.")]
        [SerializeField] private PersonalBestSO _personalBest;

        [Header("UI (optional)")]
        [Tooltip("Displays the score from the most recent match. e.g. 'Score: 1450'")]
        [SerializeField] private Text _scoreText;

        [Tooltip("Displays the all-time personal best score. e.g. 'Best: 2100'")]
        [SerializeField] private Text _bestScoreText;

        [Tooltip("Shown when the current match set a new personal best. " +
                 "Hidden otherwise.  Leave null to skip.")]
        [SerializeField] private GameObject _newBestPanel;

        [Header("Event Channels — In")]
        [Tooltip("VoidGameEvent raised by MatchManager when the round ends. " +
                 "Must be the same channel that MatchManager._onMatchEnded points to.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _matchEndedCallback;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _matchEndedCallback = HandleMatchEnded;

            // Ensure the new-best panel is hidden on startup.
            if (_newBestPanel != null) _newBestPanel.SetActive(false);
        }

        private void OnEnable()  => _onMatchEnded?.RegisterCallback(_matchEndedCallback);
        private void OnDisable() => _onMatchEnded?.UnregisterCallback(_matchEndedCallback);

        // ── Event handler ─────────────────────────────────────────────────────

        private void HandleMatchEnded()
        {
            // PersonalBestSO may be unassigned in a partial-wiring scene — safe to skip.
            if (_personalBest == null) return;
            Refresh();
        }

        // ── UI refresh ────────────────────────────────────────────────────────

        private void Refresh()
        {
            if (_personalBest == null) return;

            if (_scoreText != null)
                _scoreText.text = string.Format("Score: {0}", _personalBest.CurrentScore);

            if (_bestScoreText != null)
                _bestScoreText.text = string.Format("Best: {0}", _personalBest.BestScore);

            if (_newBestPanel != null)
                _newBestPanel.SetActive(_personalBest.IsNewBest);
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_personalBest == null)
                Debug.LogWarning("[PersonalBestController] _personalBest PersonalBestSO not assigned.", this);
            if (_onMatchEnded == null)
                Debug.LogWarning("[PersonalBestController] _onMatchEnded not assigned — " +
                                 "controller will never refresh.", this);
        }
#endif
    }
}
