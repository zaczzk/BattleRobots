using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Tracks the player's prestige rank — the number of times they have voluntarily
    /// reset their progression after reaching max level to earn a permanent cosmetic
    /// badge.
    ///
    /// ── Prestige mechanic ────────────────────────────────────────────────────────
    ///   • The player may prestige only when their <see cref="PlayerProgressionSO"/>
    ///     is at <see cref="PlayerProgressionSO.IsMaxLevel"/>.
    ///   • Prestiging calls <see cref="PlayerProgressionSO.Reset"/> (back to Level 1 /
    ///     0 XP) and increments the internal prestige counter.
    ///   • Once <see cref="IsMaxPrestige"/> is reached no further prestiges are allowed.
    ///
    /// ── Rank labels ──────────────────────────────────────────────────────────────
    ///   Prestige 0        → "None"
    ///   Prestige 1–3      → "Bronze I" … "Bronze III"
    ///   Prestige 4–6      → "Silver I" … "Silver III"
    ///   Prestige 7–9      → "Gold I"   … "Gold III"
    ///   Prestige 10 (max) → "Legend"
    ///
    /// ── Mutators ─────────────────────────────────────────────────────────────────
    ///   • <see cref="Prestige"/>       — guarded entry point; resets progression.
    ///   • <see cref="LoadSnapshot"/>   — bootstrapper-safe rehydration (no events).
    ///   • <see cref="Reset"/>          — silent clear to 0 (no events).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is never serialised to the SO asset; it is rehydrated from
    ///     <see cref="SaveData.prestigeCount"/> via <see cref="LoadSnapshot"/>.
    ///   - <see cref="LoadSnapshot"/> and <see cref="Reset"/> do NOT fire events
    ///     (bootstrapper-safe).
    ///
    /// ── Scene / SO wiring ────────────────────────────────────────────────────────
    ///   1. Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ PrestigeSystem.
    ///   2. Assign to <see cref="GameBootstrapper._prestigeSystem"/>.
    ///   3. Assign to <see cref="BattleRobots.UI.PrestigeController"/> for HUD display.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/PrestigeSystem",
        fileName = "PrestigeSystemSO")]
    public sealed class PrestigeSystemSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Maximum number of prestige ranks the player can earn. Must be ≥ 1.")]
        [SerializeField, Min(1)] private int _maxPrestigeRank = 10;

        [Tooltip("VoidGameEvent raised once each time the player prestiges. " +
                 "Leave null if no system needs to react.")]
        [SerializeField] private VoidGameEvent _onPrestige;

        // ── Runtime state ─────────────────────────────────────────────────────

        private int _prestigeCount;

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>
        /// Number of times the player has prestiged. Always in [0, <see cref="MaxPrestigeRank"/>].
        /// </summary>
        public int PrestigeCount => _prestigeCount;

        /// <summary>
        /// Maximum prestige rank configured in the Inspector.
        /// </summary>
        public int MaxPrestigeRank => _maxPrestigeRank;

        /// <summary>
        /// True when the player has reached the maximum prestige rank and may not prestige again.
        /// </summary>
        public bool IsMaxPrestige => _prestigeCount >= _maxPrestigeRank;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true when the player meets all conditions to prestige:
        /// <paramref name="progression"/> is non-null, at <see cref="PlayerProgressionSO.IsMaxLevel"/>,
        /// and <see cref="IsMaxPrestige"/> is false.
        /// </summary>
        /// <param name="progression">The player's progression SO. May be null.</param>
        public bool CanPrestige(PlayerProgressionSO progression)
            => progression != null && progression.IsMaxLevel && !IsMaxPrestige;

        /// <summary>
        /// Performs a prestige if <see cref="CanPrestige"/> returns true:
        /// resets <paramref name="progression"/> to Level 1 / 0 XP,
        /// increments <see cref="PrestigeCount"/>, and fires <c>_onPrestige</c>.
        /// Silent no-op when the preconditions are not met.
        /// </summary>
        /// <param name="progression">The player's progression SO. May be null.</param>
        public void Prestige(PlayerProgressionSO progression)
        {
            if (!CanPrestige(progression)) return;

            progression.Reset();
            _prestigeCount++;
            _onPrestige?.Raise();
        }

        /// <summary>
        /// Returns a human-readable rank label for the current <see cref="PrestigeCount"/>:
        /// <list type="bullet">
        ///   <item><description>0        → "None"</description></item>
        ///   <item><description>1–3      → "Bronze I/II/III"</description></item>
        ///   <item><description>4–6      → "Silver I/II/III"</description></item>
        ///   <item><description>7–9      → "Gold I/II/III"</description></item>
        ///   <item><description>10 (max) → "Legend"</description></item>
        /// </list>
        /// Values above 10 fall into the "Legend" bucket.
        /// </summary>
        public string GetRankLabel() => GetRankLabelForCount(_prestigeCount);

        /// <summary>
        /// Pure static helper used by tests and the controller to compute the rank
        /// label for any given <paramref name="count"/> without needing a live instance.
        /// </summary>
        public static string GetRankLabelForCount(int count)
        {
            if (count <= 0)  return "None";
            if (count >= 10) return "Legend";

            string tier;
            int    offset;

            if (count <= 3)      { tier = "Bronze"; offset = count; }
            else if (count <= 6) { tier = "Silver"; offset = count - 3; }
            else                 { tier = "Gold";   offset = count - 6; }

            string numeral = offset == 1 ? "I" : offset == 2 ? "II" : "III";
            return $"{tier} {numeral}";
        }

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Silently rehydrates state from a <see cref="SaveData.prestigeCount"/> snapshot.
        /// Does NOT fire any events — safe to call from <see cref="GameBootstrapper"/>.
        /// <paramref name="prestigeCount"/> is clamped to [0, <see cref="MaxPrestigeRank"/>].
        /// </summary>
        public void LoadSnapshot(int prestigeCount)
        {
            _prestigeCount = Mathf.Clamp(prestigeCount, 0, _maxPrestigeRank);
        }

        /// <summary>
        /// Silently resets prestige count to 0. Does NOT fire any events.
        /// Intended for fresh-install resets.
        /// </summary>
        public void Reset()
        {
            _prestigeCount = 0;
        }
    }
}
