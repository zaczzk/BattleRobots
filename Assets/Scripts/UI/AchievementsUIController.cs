using System;
using System.Collections.Generic;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Populates the achievements panel by instantiating one
    /// <see cref="AchievementRowController"/> prefab per
    /// <see cref="AchievementDefinitionSO"/> in the assigned
    /// <see cref="AchievementCatalogSO"/>.
    ///
    /// ── Responsibilities ──────────────────────────────────────────────────────
    ///   • Instantiates and configures achievement rows in OnEnable.
    ///   • Subscribes to <c>_onAchievementUnlocked</c> (VoidGameEvent) and
    ///     repopulates the panel automatically when a new achievement is earned.
    ///   • Cleans up the subscription in OnDisable.
    ///   • Derives current progress for each trigger type from optional runtime SOs.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Create a prefab with <see cref="AchievementRowController"/> attached.
    ///      Assign optional Text / badge fields inside the prefab Inspector.
    ///   2. Add <see cref="AchievementsUIController"/> to the achievements panel
    ///      GameObject.
    ///   3. Assign <c>_rowPrefab</c> and <c>_listContainer</c> (ScrollRect content
    ///      Transform; VerticalLayoutGroup recommended).
    ///   4. Assign <c>_catalog</c> → AchievementCatalogSO asset.
    ///   5. Assign <c>_playerAchievements</c> → PlayerAchievementsSO asset.
    ///   6. Optionally assign progress sources:
    ///      <c>_winStreak</c>, <c>_playerProgression</c>, <c>_playerPartUpgrades</c>.
    ///   7. Optionally assign <c>_onAchievementUnlocked</c> → the same VoidGameEvent
    ///      SO as <see cref="PlayerAchievementsSO._onAchievementUnlocked"/> so the
    ///      panel auto-refreshes on each new unlock.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - Delegate cached in Awake; zero alloc after Awake outside PopulateCatalog.
    ///   - PopulateCatalog is the only path that allocates (row instantiation).
    ///   - All optional SO fields are null-safe; the panel degrades gracefully when
    ///     progress sources are absent (progress shows as 0 for missing sources).
    ///   - GetCurrentProgress is private (tested via reflection in EditMode tests).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AchievementsUIController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Layout")]
        [Tooltip("Prefab with AchievementRowController attached. " +
                 "One instance is spawned per achievement definition.")]
        [SerializeField] private GameObject _rowPrefab;

        [Tooltip("Parent Transform for instantiated rows (ScrollRect Content). " +
                 "A VerticalLayoutGroup on this Transform is recommended.")]
        [SerializeField] private Transform _listContainer;

        [Header("Achievement Data")]
        [Tooltip("Full catalog of achievement definitions. Required for population.")]
        [SerializeField] private AchievementCatalogSO _catalog;

        [Tooltip("Runtime SO tracking which achievements are unlocked and match counters. " +
                 "Required for population.")]
        [SerializeField] private PlayerAchievementsSO _playerAchievements;

        [Header("Progress Sources (optional)")]
        [Tooltip("Provides BestStreak for WinStreak-trigger progress display. Leave null to show 0.")]
        [SerializeField] private WinStreakSO _winStreak;

        [Tooltip("Provides CurrentLevel for ReachLevel-trigger progress display. Leave null to show 0.")]
        [SerializeField] private PlayerProgressionSO _playerProgression;

        [Tooltip("Provides part-tier data for PartUpgraded-trigger progress display. Leave null to show 0.")]
        [SerializeField] private PlayerPartUpgrades _playerPartUpgrades;

        [Header("Event Channels — In")]
        [Tooltip("VoidGameEvent raised by PlayerAchievementsSO when any achievement is unlocked. " +
                 "Subscribe to auto-refresh the panel on each new unlock. Leave null to skip.")]
        [SerializeField] private VoidGameEvent _onAchievementUnlocked;

        // ── Cached delegate (allocated once in Awake — zero alloc in event callbacks) ──

        private Action _populateDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _populateDelegate = PopulateCatalog;
        }

        private void OnEnable()
        {
            _onAchievementUnlocked?.RegisterCallback(_populateDelegate);
            PopulateCatalog();
        }

        private void OnDisable()
        {
            _onAchievementUnlocked?.UnregisterCallback(_populateDelegate);
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Destroys all existing rows, then instantiates one
        /// <see cref="AchievementRowController"/> row per definition in the catalog.
        ///
        /// Each row is set up with the current unlock state and trigger progress
        /// derived from the optional progress-source SOs.
        ///
        /// Safe to call from a UI Button.onClick UnityEvent.
        /// </summary>
        public void PopulateCatalog()
        {
            if (_catalog == null || _playerAchievements == null)
            {
                Debug.LogWarning("[AchievementsUIController] _catalog or _playerAchievements " +
                                 "not assigned — achievements panel not populated.", this);
                return;
            }

            if (_listContainer == null || _rowPrefab == null)
            {
                Debug.LogWarning("[AchievementsUIController] _listContainer or _rowPrefab " +
                                 "not assigned — check Inspector wiring.", this);
                return;
            }

            // Destroy existing rows top-down so Destroy order matches child order.
            for (int i = _listContainer.childCount - 1; i >= 0; i--)
                Destroy(_listContainer.GetChild(i).gameObject);

            IReadOnlyList<AchievementDefinitionSO> all = _catalog.Achievements;
            for (int i = 0; i < all.Count; i++)
            {
                AchievementDefinitionSO def = all[i];
                if (def == null) continue;

                bool isUnlocked   = _playerAchievements.HasUnlocked(def.Id);
                int  progress     = GetCurrentProgress(def.TriggerType);

                GameObject row  = Instantiate(_rowPrefab, _listContainer);
                var        ctrl = row.GetComponent<AchievementRowController>();

                if (ctrl == null)
                {
                    Debug.LogWarning("[AchievementsUIController] _rowPrefab is missing an " +
                                     "AchievementRowController component — row skipped.", this);
                    Destroy(row);
                    continue;
                }

                ctrl.Setup(def, isUnlocked, progress);
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Returns the current progress value for the given trigger type by reading
        /// the relevant runtime SO.  Returns 0 when the corresponding source SO is null.
        ///
        /// Private — exercised via reflection in EditMode tests without scene setup.
        /// </summary>
        private int GetCurrentProgress(AchievementTrigger trigger)
        {
            switch (trigger)
            {
                case AchievementTrigger.MatchWon:
                    return _playerAchievements?.TotalMatchesWon ?? 0;

                case AchievementTrigger.WinStreak:
                    return _winStreak != null ? _winStreak.BestStreak : 0;

                case AchievementTrigger.ReachLevel:
                    return _playerProgression != null ? _playerProgression.CurrentLevel : 0;

                case AchievementTrigger.TotalMatches:
                    return _playerAchievements?.TotalMatchesPlayed ?? 0;

                case AchievementTrigger.PartUpgraded:
                    return GetTotalUpgradeTiers();

                default:
                    return 0;
            }
        }

        private int GetTotalUpgradeTiers()
        {
            if (_playerPartUpgrades == null) return 0;

            _playerPartUpgrades.TakeSnapshot(out List<string> _, out List<int> values);
            int total = 0;
            for (int i = 0; i < values.Count; i++)
                total += values[i];
            return total;
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_catalog == null)
                Debug.LogWarning("[AchievementsUIController] _catalog not assigned — " +
                                 "panel will not populate.", this);
            if (_playerAchievements == null)
                Debug.LogWarning("[AchievementsUIController] _playerAchievements not assigned — " +
                                 "panel will not populate.", this);
            if (_rowPrefab == null)
                Debug.LogWarning("[AchievementsUIController] _rowPrefab not assigned.", this);
            if (_listContainer == null)
                Debug.LogWarning("[AchievementsUIController] _listContainer not assigned.", this);
        }
#endif
    }
}
