using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Pre-match UI controller that lets the player choose one
    /// <see cref="MatchBonusObjectiveSO"/> from a <see cref="BonusObjectiveCatalogSO"/>.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   • Previous / Next buttons cycle wrap-around through the catalog.
    ///   • Updates optional title, reward, and time-limit labels on each change.
    ///   • When <c>_onMatchStarted</c> fires, injects the selected objective into
    ///     <see cref="BonusObjectiveHUDController.SetObjective"/> so the in-match
    ///     HUD begins tracking the correct objective from the first frame.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - Button delegates and VoidGameEvent callback cached in Awake; zero
    ///     heap allocations after initialisation.
    ///   - No Update method.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_catalog</c> (BonusObjectiveCatalogSO).
    ///   2. Assign <c>_bonusObjectiveHUD</c> (BonusObjectiveHUDController in the
    ///      Arena scene or on the same canvas — injected on match start).
    ///   3. Assign <c>_onMatchStarted</c> (VoidGameEvent fired by MatchManager).
    ///   4. Optionally wire Previous/Next buttons, title, reward, time-limit labels.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BonusObjectiveSelectorController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Catalog of available bonus objectives the player can select.")]
        [SerializeField] private BonusObjectiveCatalogSO _catalog;

        [Tooltip("In-match HUD controller that will receive the selected objective " +
                 "via SetObjective() when the match starts.")]
        [SerializeField] private BonusObjectiveHUDController _bonusObjectiveHUD;

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent raised by MatchManager when the match starts. " +
                 "Triggers InjectObjective() so the HUD begins tracking immediately.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Header("UI (optional)")]
        [Tooltip("Button that cycles to the previous objective (wrap-around).")]
        [SerializeField] private Button _prevButton;

        [Tooltip("Button that cycles to the next objective (wrap-around).")]
        [SerializeField] private Button _nextButton;

        [Tooltip("Label showing the selected objective's BonusTitle.")]
        [SerializeField] private Text _titleLabel;

        [Tooltip("Label showing the selected objective's reward (e.g. 'Reward: 50').")]
        [SerializeField] private Text _rewardLabel;

        [Tooltip("Label showing the time limit ('N.0s' or 'No limit').")]
        [SerializeField] private Text _timeLimitLabel;

        // ── Runtime state ─────────────────────────────────────────────────────

        private int _selectedIndex;

        /// <summary>Current zero-based index into <see cref="BonusObjectiveCatalogSO.Objectives"/>.</summary>
        public int SelectedIndex => _selectedIndex;

        // ── Cached delegates ──────────────────────────────────────────────────

        private UnityAction _prevAction;
        private UnityAction _nextAction;
        private Action      _injectAction;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _prevAction   = PreviousObjective;
            _nextAction   = NextObjective;
            _injectAction = InjectObjective;
        }

        private void OnEnable()
        {
            _prevButton?.onClick.AddListener(_prevAction);
            _nextButton?.onClick.AddListener(_nextAction);
            _onMatchStarted?.RegisterCallback(_injectAction);
            ApplySelection();
        }

        private void OnDisable()
        {
            _prevButton?.onClick.RemoveListener(_prevAction);
            _nextButton?.onClick.RemoveListener(_nextAction);
            _onMatchStarted?.UnregisterCallback(_injectAction);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Advances to the next objective. Wraps from the last back to the first.
        /// No-op when the catalog is null or empty.
        /// </summary>
        public void NextObjective()
        {
            if (_catalog == null || _catalog.Count == 0) return;
            _selectedIndex = (_selectedIndex + 1) % _catalog.Count;
            ApplySelection();
        }

        /// <summary>
        /// Steps back to the previous objective. Wraps from the first to the last.
        /// No-op when the catalog is null or empty.
        /// </summary>
        public void PreviousObjective()
        {
            if (_catalog == null || _catalog.Count == 0) return;
            _selectedIndex = (_selectedIndex - 1 + _catalog.Count) % _catalog.Count;
            ApplySelection();
        }

        /// <summary>
        /// Injects the currently selected <see cref="MatchBonusObjectiveSO"/> into
        /// <see cref="_bonusObjectiveHUD"/> via
        /// <see cref="BonusObjectiveHUDController.SetObjective"/>.
        /// Called automatically when <c>_onMatchStarted</c> fires.
        /// Safe to call manually (e.g. from an Inspector button event).
        /// </summary>
        public void InjectObjective()
        {
            var selected = _catalog?.Get(_selectedIndex);
            _bonusObjectiveHUD?.SetObjective(selected);
        }

        /// <summary>The assigned catalog. May be null.</summary>
        public BonusObjectiveCatalogSO Catalog => _catalog;

        /// <summary>The assigned HUD controller. May be null.</summary>
        public BonusObjectiveHUDController BonusObjectiveHUD => _bonusObjectiveHUD;

        // ── Private helpers ───────────────────────────────────────────────────

        private void ApplySelection()
        {
            if (_catalog == null || _catalog.Count == 0)
            {
                if (_titleLabel     != null) _titleLabel.text     = "\u2014"; // em-dash
                if (_rewardLabel    != null) _rewardLabel.text    = string.Empty;
                if (_timeLimitLabel != null) _timeLimitLabel.text = string.Empty;
                return;
            }

            MatchBonusObjectiveSO obj = _catalog.Get(_selectedIndex);

            if (_titleLabel != null)
                _titleLabel.text = obj != null ? obj.BonusTitle : "\u2014";

            if (_rewardLabel != null)
                _rewardLabel.text = obj != null
                    ? string.Format("Reward: {0}", obj.BonusReward)
                    : string.Empty;

            if (_timeLimitLabel != null)
            {
                if (obj == null)
                    _timeLimitLabel.text = string.Empty;
                else if (obj.HasTimeLimit)
                    _timeLimitLabel.text = string.Format("{0:F1}s", obj.TimeRemaining);
                else
                    _timeLimitLabel.text = "No limit";
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_catalog == null)
                Debug.LogWarning("[BonusObjectiveSelectorController] _catalog is not assigned.", this);
            if (_bonusObjectiveHUD == null)
                Debug.LogWarning("[BonusObjectiveSelectorController] _bonusObjectiveHUD is not assigned " +
                                 "— InjectObjective() will be a no-op.", this);
        }
#endif
    }
}
