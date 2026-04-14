using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable data SO that specifies the minimum prestige rank required to equip
    /// weapons of each <see cref="DamageType"/>.
    ///
    /// ── Design intent ────────────────────────────────────────────────────────────
    ///   Physical weapons are available from the start (default requirement 0).
    ///   Energy, Thermal, and Shock weapons unlock progressively as the player
    ///   accumulates prestige ranks (default: Bronze I, Silver I, Gold I).
    ///   A requirement of 0 means the type is always equippable.
    ///
    /// ── Evaluation ───────────────────────────────────────────────────────────────
    ///   Use <see cref="IsUnlocked"/> for a direct bool answer, or
    ///   <see cref="WeaponTypeUnlockEvaluator.IsTypeUnlocked"/> for a version that
    ///   reads the requirement against a live <see cref="PrestigeSystemSO"/> instance.
    ///
    /// ── Lock reason strings ───────────────────────────────────────────────────────
    ///   <see cref="GetLockReason"/> returns an empty string when the type is
    ///   unlocked, or a human-readable message of the form
    ///   "Requires Prestige N (rank label)" when locked.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All properties read-only; asset is immutable at runtime.
    ///   - Zero allocation on the hot path: property reads + switch only.
    ///   - Backwards-compatible: null config → all types unlocked
    ///     (enforced by <see cref="WeaponTypeUnlockEvaluator"/>).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ WeaponTypeUnlockConfig.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/WeaponTypeUnlockConfig",
        fileName = "WeaponTypeUnlockConfig")]
    public sealed class WeaponTypeUnlockConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Prestige Rank Required per Damage Type")]
        [Tooltip("Prestige rank required to equip Physical weapons. " +
                 "0 = always available (recommended — Physical is the baseline type).")]
        [SerializeField, Min(0)] private int _physicalRequiredPrestige = 0;

        [Tooltip("Prestige rank required to equip Energy weapons. " +
                 "Default 1 = unlocked at Bronze I.")]
        [SerializeField, Min(0)] private int _energyRequiredPrestige = 1;

        [Tooltip("Prestige rank required to equip Thermal weapons. " +
                 "Default 4 = unlocked at Silver I.")]
        [SerializeField, Min(0)] private int _thermalRequiredPrestige = 4;

        [Tooltip("Prestige rank required to equip Shock weapons. " +
                 "Default 7 = unlocked at Gold I.")]
        [SerializeField, Min(0)] private int _shockRequiredPrestige = 7;

        // ── Read-only properties ──────────────────────────────────────────────

        /// <summary>Minimum prestige rank to equip Physical weapons. Defaults to 0.</summary>
        public int PhysicalRequiredPrestige => _physicalRequiredPrestige;

        /// <summary>Minimum prestige rank to equip Energy weapons. Defaults to 1.</summary>
        public int EnergyRequiredPrestige   => _energyRequiredPrestige;

        /// <summary>Minimum prestige rank to equip Thermal weapons. Defaults to 4.</summary>
        public int ThermalRequiredPrestige  => _thermalRequiredPrestige;

        /// <summary>Minimum prestige rank to equip Shock weapons. Defaults to 7.</summary>
        public int ShockRequiredPrestige    => _shockRequiredPrestige;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the minimum prestige rank needed to equip weapons of <paramref name="type"/>.
        /// Returns 0 for unknown / out-of-range types (effectively always unlocked).
        /// </summary>
        public int GetRequiredPrestige(DamageType type)
        {
            switch (type)
            {
                case DamageType.Physical: return _physicalRequiredPrestige;
                case DamageType.Energy:   return _energyRequiredPrestige;
                case DamageType.Thermal:  return _thermalRequiredPrestige;
                case DamageType.Shock:    return _shockRequiredPrestige;
                default:                  return 0;
            }
        }

        /// <summary>
        /// Returns <c>true</c> when <paramref name="prestigeCount"/> meets or exceeds
        /// the prestige requirement for <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The damage type to check.</param>
        /// <param name="prestigeCount">The player's current prestige rank (0 = no prestige).</param>
        public bool IsUnlocked(DamageType type, int prestigeCount)
            => prestigeCount >= GetRequiredPrestige(type);

        /// <summary>
        /// Returns a human-readable lock reason when <paramref name="type"/> is locked
        /// for the given <paramref name="prestigeCount"/>.
        /// Returns <see cref="string.Empty"/> when the type is unlocked.
        ///
        /// Format: <c>"Requires Prestige N (rank label)"</c>
        /// where N is <see cref="GetRequiredPrestige"/> and rank label is
        /// derived from <see cref="PrestigeSystemSO.GetRankLabelForCount"/>.
        /// </summary>
        public string GetLockReason(DamageType type, int prestigeCount)
        {
            if (IsUnlocked(type, prestigeCount)) return string.Empty;

            int    required  = GetRequiredPrestige(type);
            string rankLabel = PrestigeSystemSO.GetRankLabelForCount(required);
            return $"Requires Prestige {required} ({rankLabel})";
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            _physicalRequiredPrestige = Mathf.Max(0, _physicalRequiredPrestige);
            _energyRequiredPrestige   = Mathf.Max(0, _energyRequiredPrestige);
            _thermalRequiredPrestige  = Mathf.Max(0, _thermalRequiredPrestige);
            _shockRequiredPrestige    = Mathf.Max(0, _shockRequiredPrestige);
        }
#endif
    }
}
