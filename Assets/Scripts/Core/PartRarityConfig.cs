using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Per-tier display and weight data for a single <see cref="PartRarity"/> value.
    ///
    /// Stored inside <see cref="PartRarityConfig._tiers"/> and configured in the Inspector.
    /// </summary>
    [Serializable]
    public struct RarityTierData
    {
        [Tooltip("Rarity tier this entry configures.")]
        public PartRarity rarity;

        [Tooltip("Player-facing label shown in the shop and loot notifications " +
                 "(e.g. \"Common\", \"Legendary\").")]
        public string displayName;

        [Tooltip("UI tint colour used for rarity badges, item borders, and glow effects.")]
        public Color tintColor;

        [Tooltip("Multiplier applied to this part's base loot-table weight when the " +
                 "rarity-aware RollDrop() overload is used.  " +
                 "Higher values make the tier more likely to drop; lower values reduce it.")]
        [Min(0.1f)] public float lootWeightMultiplier;
    }

    /// <summary>
    /// Immutable SO that maps each <see cref="PartRarity"/> value to a display name,
    /// UI tint colour, and loot-drop weight multiplier.
    ///
    /// ── Authoring ─────────────────────────────────────────────────────────────
    ///   Create one asset and configure one <see cref="RarityTierData"/> entry per
    ///   rarity tier.  The list order does not matter — all accessors search by
    ///   <see cref="PartRarity"/> enum value.
    ///
    ///   Suggested defaults:
    ///   <list type="bullet">
    ///     <item>Common    — grey,   multiplier 1.0</item>
    ///     <item>Uncommon  — green,  multiplier 0.7</item>
    ///     <item>Rare      — blue,   multiplier 0.4</item>
    ///     <item>Epic      — purple, multiplier 0.2</item>
    ///     <item>Legendary — yellow, multiplier 0.1</item>
    ///   </list>
    ///
    /// ── Safe defaults ─────────────────────────────────────────────────────────
    ///   All accessors return a sensible fallback when no matching entry is found:
    ///   <list type="bullet">
    ///     <item><see cref="GetDisplayName"/> → <c>rarity.ToString()</c></item>
    ///     <item><see cref="GetTintColor"/>   → <c>Color.white</c></item>
    ///     <item><see cref="GetLootWeightMultiplier"/> → <c>1f</c></item>
    ///   </list>
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • BattleRobots.Core namespace — no Physics / UI references.
    ///   • SO asset immutable at runtime (no setters).
    ///   • <see cref="GetLootWeightMultiplier"/> clamps the stored value to ≥ 0.1
    ///     at runtime to guard against accidental zero entries.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Shop ▶ PartRarityConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Shop/PartRarityConfig",
                     fileName = "PartRarityConfig")]
    public sealed class PartRarityConfig : ScriptableObject
    {
        [Tooltip("One entry per rarity tier.  Order does not matter.")]
        [SerializeField] private List<RarityTierData> _tiers = new List<RarityTierData>();

        /// <summary>Read-only view of all configured tier entries.</summary>
        public IReadOnlyList<RarityTierData> Tiers => _tiers;

        /// <summary>
        /// Returns the display name configured for <paramref name="rarity"/>,
        /// or <c>rarity.ToString()</c> when no matching entry exists.
        /// </summary>
        public string GetDisplayName(PartRarity rarity)
        {
            for (int i = 0; i < _tiers.Count; i++)
                if (_tiers[i].rarity == rarity)
                    return _tiers[i].displayName;
            return rarity.ToString();
        }

        /// <summary>
        /// Returns the tint colour configured for <paramref name="rarity"/>,
        /// or <c>Color.white</c> when no matching entry exists.
        /// </summary>
        public Color GetTintColor(PartRarity rarity)
        {
            for (int i = 0; i < _tiers.Count; i++)
                if (_tiers[i].rarity == rarity)
                    return _tiers[i].tintColor;
            return Color.white;
        }

        /// <summary>
        /// Returns the loot-drop weight multiplier configured for <paramref name="rarity"/>,
        /// clamped to a minimum of <c>0.1</c>, or <c>1f</c> when no matching entry exists.
        ///
        /// The runtime clamp guards against accidental zero/negative values that could
        /// completely exclude a rarity tier from drops regardless of inspector validation.
        /// </summary>
        public float GetLootWeightMultiplier(PartRarity rarity)
        {
            for (int i = 0; i < _tiers.Count; i++)
                if (_tiers[i].rarity == rarity)
                    return Mathf.Max(0.1f, _tiers[i].lootWeightMultiplier);
            return 1f;
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_tiers.Count == 0)
                Debug.LogWarning(
                    "[PartRarityConfig] Tier list is empty — all rarity accessors will " +
                    "return defaults.  Add one RarityTierData entry per PartRarity value.",
                    this);
        }
#endif
    }
}
