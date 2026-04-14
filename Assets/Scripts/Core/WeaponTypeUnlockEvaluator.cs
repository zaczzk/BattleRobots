namespace BattleRobots.Core
{
    /// <summary>
    /// Stateless evaluator that checks whether a given <see cref="DamageType"/> weapon
    /// is equippable based on the player's current prestige rank.
    ///
    /// ── Null-safety rules ────────────────────────────────────────────────────────
    ///   • Null <paramref name="config"/> → all types unlocked (backwards-compatible
    ///     with setups that do not use the weapon-type unlock system).
    ///   • Null <paramref name="prestige"/> → prestige count treated as 0.
    ///
    /// ── Usage ───────────────────────────────────────────────────────────────────
    ///   Call <see cref="IsTypeUnlocked"/> for a bool answer.
    ///   Call <see cref="GetLockReason"/> to retrieve a display string for locked types.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All methods are static; zero allocation; no Unity type dependencies beyond
    ///     ScriptableObject parameter passing.
    /// </summary>
    public static class WeaponTypeUnlockEvaluator
    {
        /// <summary>
        /// Returns <c>true</c> when the player's prestige rank allows equipping weapons
        /// of <paramref name="type"/>.
        ///
        /// <para>Null <paramref name="config"/> → always returns <c>true</c>.</para>
        /// <para>Null <paramref name="prestige"/> → treated as prestige count 0.</para>
        /// </summary>
        /// <param name="config">
        /// Config SO with per-type prestige requirements.
        /// May be null; null means all types are unlocked.
        /// </param>
        /// <param name="prestige">
        /// Runtime prestige SO. May be null; null is treated as count 0.
        /// </param>
        /// <param name="type">The damage type being checked.</param>
        public static bool IsTypeUnlocked(
            WeaponTypeUnlockConfig config,
            PrestigeSystemSO       prestige,
            DamageType             type)
        {
            if (config == null) return true;
            int count = prestige?.PrestigeCount ?? 0;
            return config.IsUnlocked(type, count);
        }

        /// <summary>
        /// Returns a human-readable lock reason when <paramref name="type"/> is locked,
        /// or <see cref="string.Empty"/> when it is unlocked or when
        /// <paramref name="config"/> is null.
        ///
        /// <para>Null <paramref name="prestige"/> → treated as prestige count 0.</para>
        /// </summary>
        /// <param name="config">
        /// Config SO with per-type prestige requirements. May be null.
        /// </param>
        /// <param name="prestige">
        /// Runtime prestige SO. May be null; null is treated as count 0.
        /// </param>
        /// <param name="type">The damage type being checked.</param>
        public static string GetLockReason(
            WeaponTypeUnlockConfig config,
            PrestigeSystemSO       prestige,
            DamageType             type)
        {
            if (config == null) return string.Empty;
            int count = prestige?.PrestigeCount ?? 0;
            return config.GetLockReason(type, count);
        }
    }
}
