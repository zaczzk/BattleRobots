using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays the adaptive difficulty suggestion produced by
    /// <see cref="AdaptiveDifficultyAdvisorSO"/> and provides a "Use Suggestion" button
    /// that writes the recommended preset to <see cref="SelectedDifficultySO"/>.
    ///
    /// ── Refresh triggers ──────────────────────────────────────────────────────
    ///   The panel refreshes on <c>OnEnable</c> and whenever either optional event
    ///   channel fires:
    ///     • <c>_onMatchEnded</c>      — raised by <see cref="MatchManager"/> after a match
    ///                                   so the suggestion updates as soon as new stats arrive.
    ///     • <c>_onHistoryUpdated</c>  — raised by <see cref="ScoreHistorySO"/> after
    ///                                   <c>Record()</c>, enabling live updates within a session.
    ///
    /// ── UI fields ─────────────────────────────────────────────────────────────
    ///   • <c>_suggestionText</c>      — shows "Try: {preset name}" or "No suggestion".
    ///   • <c>_suggestionReasonText</c>— shows the human-readable reason from
    ///                                   <see cref="AdaptiveDifficultyAdvisorSO.GetSuggestionReason"/>.
    ///   • <c>_useSuggestionButton</c> — interactable only when a suggestion exists;
    ///                                   calls <see cref="UseSuggestion"/> on click.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace. References BattleRobots.Core only; no Physics refs.
    ///   - All inspector fields are optional; any unassigned field is silently skipped.
    ///   - Delegate cached in Awake; button listener wired once in Awake — zero alloc
    ///     on subscribe/unsubscribe hot path.
    ///   - No Update / FixedUpdate — purely event-driven.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ UI ▶ AdaptiveDifficultyController
    /// or add as a component to the pre-match difficulty panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AdaptiveDifficultyController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Advisor SO that computes a difficulty suggestion from recent trend and streak data. " +
                 "Leave null — button will be non-interactable and labels show fallback text.")]
        [SerializeField] private AdaptiveDifficultyAdvisorSO _advisor;

        [Tooltip("Runtime SO holding the currently-selected difficulty config. " +
                 "Required for UseSuggestion() to take effect; otherwise clicking the button is a no-op.")]
        [SerializeField] private SelectedDifficultySO _selectedDifficulty;

        [Tooltip("Presets catalogue used to resolve the suggestion. " +
                 "Must match the catalogue used by DifficultySelectionController.")]
        [SerializeField] private DifficultyPresetsConfig _difficultyPresets;

        [Header("Event Channels — In (optional)")]
        [Tooltip("VoidGameEvent raised by MatchManager after a match ends. " +
                 "Triggers Refresh() so the suggestion updates with the latest match result.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Tooltip("VoidGameEvent raised by ScoreHistorySO after Record(). " +
                 "Wire the same asset assigned to ScoreHistorySO._onHistoryUpdated " +
                 "if live in-session updates are desired.")]
        [SerializeField] private VoidGameEvent _onHistoryUpdated;

        [Header("Labels (optional)")]
        [Tooltip("Displays the suggested difficulty: 'Try: {preset name}' or 'No suggestion'.")]
        [SerializeField] private Text _suggestionText;

        [Tooltip("Displays the human-readable reason from AdaptiveDifficultyAdvisorSO.GetSuggestionReason().")]
        [SerializeField] private Text _suggestionReasonText;

        [Header("Button (optional)")]
        [Tooltip("Button that applies the suggestion to SelectedDifficultySO. " +
                 "Non-interactable when no suggestion is available. " +
                 "OnClick is wired in Awake — do NOT add a duplicate listener in the Inspector.")]
        [SerializeField] private Button _useSuggestionButton;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;

            if (_useSuggestionButton != null)
                _useSuggestionButton.onClick.AddListener(UseSuggestion);
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_refreshDelegate);
            _onHistoryUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_refreshDelegate);
            _onHistoryUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current suggestion from <see cref="AdaptiveDifficultyAdvisorSO"/> and
        /// updates all UI labels and the button's interactable state.
        ///
        /// Safe to call with any combination of null inspector fields.
        /// Called automatically on <c>OnEnable</c> and whenever either event channel fires.
        /// </summary>
        public void Refresh()
        {
            BotDifficultyConfig suggestion = (_advisor != null && _difficultyPresets != null)
                ? _advisor.GetSuggestion(_difficultyPresets)
                : null;

            string reason = (_advisor != null && _difficultyPresets != null)
                ? _advisor.GetSuggestionReason(_difficultyPresets)
                : "No data";

            if (_suggestionText != null)
                _suggestionText.text = suggestion != null
                    ? string.Format("Try: {0}", suggestion.name)
                    : "No suggestion";

            if (_suggestionReasonText != null)
                _suggestionReasonText.text = reason;

            if (_useSuggestionButton != null)
                _useSuggestionButton.interactable = suggestion != null;
        }

        /// <summary>
        /// Applies the current difficulty suggestion to <see cref="SelectedDifficultySO"/>.
        ///
        /// Wired to <c>_useSuggestionButton.onClick</c> in Awake.
        /// If no suggestion is available or any required reference is null, this is a silent
        /// no-op (the button is also non-interactable in that state).
        /// </summary>
        public void UseSuggestion()
        {
            if (_advisor == null || _difficultyPresets == null || _selectedDifficulty == null)
                return;

            BotDifficultyConfig suggestion = _advisor.GetSuggestion(_difficultyPresets);
            if (suggestion != null)
                _selectedDifficulty.Select(suggestion);
        }
    }
}
