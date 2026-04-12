using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Bridges the stat-computation layer with the runtime gameplay systems.
    ///
    /// On match start this component:
    ///   1. Calls <see cref="RobotStatsAggregator.Compute"/> with the robot's
    ///      <see cref="RobotDefinition"/> and the parts assembled by
    ///      <see cref="RobotAssembler"/>.
    ///   2. Initialises <see cref="HealthSO"/> to the computed max health.
    ///   3. Sets <see cref="RobotLocomotionController"/>'s base speed.
    ///   4. Sets <see cref="RobotAIController"/>'s damage multiplier (enemy robots only).
    ///   5. Sets <see cref="DamageReceiver"/>'s flat armor rating.
    ///
    /// ── Scene wiring ─────────────────────────────────────────────────────────
    ///   • Add one per robot root GameObject.
    ///   • Assign <c>_matchStartedEvent</c> (same SO as MatchManager uses).
    ///   • Assign <c>_robotDefinition</c> — chassis SO supplying base stats.
    ///   • Assign <c>_assembler</c> — provides the list of equipped
    ///     <see cref="PartDefinition"/> SOs after Assemble() has run.
    ///     <b>MatchFlowController.HandleMatchStarted must call Assemble() before
    ///     this component's callback fires.</b>  Execution order is determined by
    ///     callback-registration order; MatchFlowController must be Awake'd first,
    ///     or assign this component a later DefaultExecutionOrder.
    ///   • Assign <c>_health</c> — the robot's runtime HealthSO.
    ///   • Assign <c>_locomotion</c> (optional) — locomotion controller.
    ///   • Assign <c>_aiController</c> (optional, enemy robots only).
    ///   • Assign <c>_damageReceiver</c> (optional).
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────
    ///   • BattleRobots.Physics namespace — may reference BattleRobots.Core.
    ///   • BattleRobots.UI must NOT reference this class.
    ///   • Zero heap allocations in the match-started callback (struct operations
    ///     + value-type passes only). Allocation happens in RobotStatsAggregator
    ///     only when iterating the equipped-parts collection (cold path, one call
    ///     per match start — acceptable).
    ///   • Delegate cached in Awake; OnEnable/OnDisable never allocate.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10)]   // After MatchFlowController (0) so Assemble() runs first.
    public sealed class CombatStatsApplicator : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channel — In")]
        [Tooltip("Raised by MatchManager when a round begins. " +
                 "ApplyStats() is called in the callback (after RobotAssembler.Assemble).")]
        [SerializeField] private VoidGameEvent _matchStartedEvent;

        [Header("Robot Data")]
        [Tooltip("Chassis definition supplying base MaxHitPoints and MoveSpeed.")]
        [SerializeField] private RobotDefinition _robotDefinition;

        [Tooltip("RobotAssembler on this robot. Must have completed Assemble() before this " +
                 "callback fires so GetEquippedParts() returns the current part list.")]
        [SerializeField] private RobotAssembler _assembler;

        [Header("Systems")]
        [Tooltip("HealthSO tracking this robot's current health. " +
                 "InitForMatch(TotalMaxHealth) + Reset() are called on match start.")]
        [SerializeField] private HealthSO _health;

        [Tooltip("(Optional) Locomotion controller. SetBaseSpeed(EffectiveSpeed) called on match start.")]
        [SerializeField] private RobotLocomotionController _locomotion;

        [Tooltip("(Optional) AI controller for enemy robots only. " +
                 "SetDamageMultiplier(EffectiveDamageMultiplier) called on match start.")]
        [SerializeField] private RobotAIController _aiController;

        [Tooltip("(Optional) DamageReceiver. SetArmorRating(TotalArmorRating) called on match start.")]
        [SerializeField] private DamageReceiver _damageReceiver;

        [Header("Match Modifier (optional)")]
        [Tooltip("Runtime SO written by MatchModifierSelectionController. " +
                 "When assigned and HasSelection is true:\n" +
                 "  • Current.SpeedMultiplier scales the computed base move speed.\n" +
                 "  • Current.ArmorMultiplier scales the computed armor rating (clamped [0,100]).\n" +
                 "Leave null to use unmodified stats (backwards-compatible).")]
        [SerializeField] private SelectedModifierSO _selectedModifier;

        [Header("Build Synergies (optional)")]
        [Tooltip("Synergy catalog SO. When assigned together with _playerLoadout and _shopCatalog, " +
                 "active synergy bonuses are evaluated at match start and folded into combat stats " +
                 "via RobotStatsAggregator.ApplySynergies (after base stats + modifier). " +
                 "Leave null to skip synergy application (backwards-compatible).")]
        [SerializeField] private PartSynergyConfig _synergyConfig;

        [Tooltip("Player loadout SO supplying equipped part IDs for synergy evaluation. " +
                 "Must be assigned alongside _synergyConfig and _shopCatalog.")]
        [SerializeField] private PlayerLoadout _playerLoadout;

        [Tooltip("Shop catalog SO used to resolve equipped part IDs to PartDefinitions. " +
                 "Must be assigned alongside _synergyConfig and _playerLoadout.")]
        [SerializeField] private ShopCatalog _shopCatalog;

        // ── Cached delegate ───────────────────────────────────────────────────

        private System.Action _onMatchStarted;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _onMatchStarted = ApplyStats;
        }

        private void OnEnable()
        {
            _matchStartedEvent?.RegisterCallback(_onMatchStarted);
        }

        private void OnDisable()
        {
            _matchStartedEvent?.UnregisterCallback(_onMatchStarted);
        }

        // ── Core logic ────────────────────────────────────────────────────────

        /// <summary>
        /// Computes <see cref="RobotCombatStats"/> from the current equipped parts and
        /// pushes the resolved values into all linked gameplay systems.
        ///
        /// Called automatically when the MatchStarted SO event fires.
        /// Also callable directly from code (e.g., tests or a pre-match lobby preview).
        /// </summary>
        public void ApplyStats()
        {
            RobotCombatStats stats = RobotStatsAggregator.Compute(
                _robotDefinition,
                _assembler != null ? _assembler.GetEquippedParts() : null);

            // ── Build synergy bonuses ─────────────────────────────────────────
            // Apply active synergy stat bonuses on top of the base part stats.
            // Must happen before health init so health synergy bonuses are included.
            // Requires all three synergy fields to be assigned; silently skips
            // when any is null (backwards-compatible).
            if (_synergyConfig != null && _playerLoadout != null && _shopCatalog != null)
            {
                var activeSynergies = _synergyConfig.GetActiveSynergies(
                    _playerLoadout.EquippedPartIds, _shopCatalog);
                stats = RobotStatsAggregator.ApplySynergies(stats, activeSynergies);
            }

            // ── Health ────────────────────────────────────────────────────────
            // Initialised after synergy application so HP bonuses are included.
            if (_health != null)
            {
                _health.InitForMatch(stats.TotalMaxHealth);
                _health.Reset();
            }

            // Resolve match-modifier multipliers (1.0 when no modifier is active).
            bool hasModifier = _selectedModifier != null
                               && _selectedModifier.HasSelection
                               && _selectedModifier.Current != null;
            float speedMult = hasModifier ? _selectedModifier.Current.SpeedMultiplier : 1f;
            float armorMult = hasModifier ? _selectedModifier.Current.ArmorMultiplier : 1f;

            // ── Locomotion ────────────────────────────────────────────────────
            _locomotion?.SetBaseSpeed(stats.EffectiveSpeed * speedMult);

            // ── AI damage output ──────────────────────────────────────────────
            _aiController?.SetDamageMultiplier(stats.EffectiveDamageMultiplier);

            // ── Damage intake (armor) ─────────────────────────────────────────
            int modifiedArmor = Mathf.Clamp(
                Mathf.RoundToInt(stats.TotalArmorRating * armorMult), 0, 100);
            _damageReceiver?.SetArmorRating(modifiedArmor);

            Debug.Log($"[CombatStatsApplicator] '{name}': HP={stats.TotalMaxHealth} " +
                      $"Speed={stats.EffectiveSpeed * speedMult:F2}(x{speedMult:F2}) " +
                      $"DmgMult={stats.EffectiveDamageMultiplier:F2} " +
                      $"Armor={modifiedArmor}(x{armorMult:F2}) " +
                      $"SynergyConfig={(_synergyConfig != null ? "assigned" : "none")}");
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_matchStartedEvent == null)
                Debug.LogWarning("[CombatStatsApplicator] _matchStartedEvent not assigned.", this);
            if (_robotDefinition == null)
                Debug.LogWarning("[CombatStatsApplicator] _robotDefinition not assigned.", this);
            if (_health == null)
                Debug.LogWarning("[CombatStatsApplicator] _health not assigned.", this);
        }
#endif
    }
}
