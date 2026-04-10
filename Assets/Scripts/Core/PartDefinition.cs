using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Gameplay-stat contribution of a single robot part.
    /// Values combine additively (health) or multiplicatively (speed/damage)
    /// inside <see cref="RobotStatsAggregator.Compute"/>.
    /// </summary>
    [Serializable]
    public struct PartStats
    {
        [Tooltip("Added to RobotDefinition.MaxHitPoints. Non-negative.")]
        [Min(0)] public int healthBonus;

        [Tooltip("Multiplied with RobotDefinition.MoveSpeed. 1.0 = no change.")]
        [Range(0.1f, 3f)] public float speedMultiplier;

        [Tooltip("Scales outgoing damage dealt by this robot. 1.0 = no change.")]
        [Range(0.1f, 3f)] public float damageMultiplier;

        [Tooltip("Flat damage reduction per hit. Summed across all parts, clamped to [0, 100].")]
        [Range(0, 100)] public int armorRating;

        /// <summary>
        /// Neutral default: zero bonus, neutral multipliers, zero armor.
        /// Equivalent to a stat-less cosmetic part.
        /// </summary>
        public static PartStats Default => new PartStats
        {
            healthBonus      = 0,
            speedMultiplier  = 1f,
            damageMultiplier = 1f,
            armorRating      = 0,
        };
    }

    /// <summary>
    /// Immutable ScriptableObject describing a purchasable robot part.
    ///
    /// Identity, category, shop cost, and thumbnail are set in the Inspector.
    /// Runtime state (owned/equipped) lives elsewhere; this SO is never mutated.
    ///
    /// Create via Assets ▶ BattleRobots ▶ Shop ▶ PartDefinition.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Shop/PartDefinition", order = 0)]
    public sealed class PartDefinition : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Stable unique key used in save data and RobotAssembly slot references.")]
        [SerializeField] private string _partId = "part_unnamed";

        [Tooltip("Player-facing display name shown in the shop UI.")]
        [SerializeField] private string _displayName = "Unnamed Part";

        [Tooltip("Category must match the PartSlot.category of the robot slot it fills.")]
        [SerializeField] private PartCategory _category = PartCategory.Weapon;

        [Tooltip("Short flavour or stat description shown in the shop browser.")]
        [SerializeField, TextArea(2, 4)] private string _description = "";

        // ── Economy ───────────────────────────────────────────────────────────

        [Header("Economy")]
        [Tooltip("Currency cost deducted from PlayerWallet on purchase.")]
        [SerializeField, Min(0)] private int _cost = 100;

        // ── Presentation ──────────────────────────────────────────────────────

        [Header("Presentation")]
        [SerializeField] private Sprite _thumbnail;

        // ── Prefab ────────────────────────────────────────────────────────────

        [Header("Prefab")]
        [Tooltip("Optional GameObject prefab instantiated at the robot's slot attachment point "
                 "by RobotAssembler during match setup. Leave null for stat-only parts.")]
        [SerializeField] private GameObject _prefab;

        // ── Stats ─────────────────────────────────────────────────────────────

        [Header("Combat Stats")]
        [Tooltip("Stat contribution of this part. Combined by RobotStatsAggregator.Compute() "
                 "at match start to derive the robot's final combat stats.")]
        [SerializeField] private PartStats _stats = PartStats.Default;

        // ── Public API ────────────────────────────────────────────────────────

        public string       PartId      => _partId;
        public string       DisplayName => _displayName;
        public PartCategory Category    => _category;
        public string       Description => _description;
        public int          Cost        => _cost;
        public Sprite       Thumbnail   => _thumbnail;
        public GameObject   Prefab      => _prefab;

        /// <summary>
        /// Gameplay-stat contribution of this part.
        /// Consumed by <see cref="RobotStatsAggregator.Compute"/> at match start.
        /// </summary>
        public PartStats Stats => _stats;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_partId))
                Debug.LogWarning($"[PartDefinition] '{name}': partId must not be empty.");
            if (string.IsNullOrWhiteSpace(_displayName))
                Debug.LogWarning($"[PartDefinition] '{name}': displayName must not be empty.");
        }
#endif
    }
}
