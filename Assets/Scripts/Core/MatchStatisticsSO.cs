using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime blackboard ScriptableObject that accumulates per-match combat statistics.
    ///
    /// ── Responsibilities ──────────────────────────────────────────────────────
    ///   • Records total damage dealt by the player and total damage taken.
    ///   • Tracks hit count (successful attacks) and hits received.
    ///   • Provides a <see cref="DamageEfficiency"/> ratio in [0, 1] for a quick
    ///     performance summary on the post-match screen.
    ///   • Resets cleanly at the start of every match via <see cref="Reset"/>.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   This SO is a pure data container — it does not subscribe to any events.
    ///   Drive it from the Arena scene via DamageGameEventListener components:
    ///
    ///   1. Add a DamageGameEventListener GameObject to the Arena scene.
    ///      Event = the DamageGameEvent channel the player's attacks raise
    ///      (i.e. the channel the <em>enemy</em> DamageReceiver listens to).
    ///      Response → MatchStatisticsSO.RecordDamageDealt(DamageInfo).
    ///
    ///   2. Add a second DamageGameEventListener.
    ///      Event = the channel enemy attacks raise
    ///      (the channel the <em>player</em> DamageReceiver listens to).
    ///      Response → MatchStatisticsSO.RecordDamageTaken(DamageInfo).
    ///
    ///   3. Assign this same SO to MatchManager._matchStatistics so that
    ///      EndMatch() uses the accurate accumulated values instead of the
    ///      end-of-match health-difference approximation.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace. No Physics / UI references.
    ///   - Zero alloc hot path: RecordDamage* methods are pure float accumulation.
    ///   - Reset() is the only way to clear state; it fires no events.
    ///   - SO assets should be treated as immutable by all callers except MatchManager
    ///     and the DamageGameEventListener response chain above.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ MatchStatisticsSO.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/MatchStatisticsSO", order = 6)]
    public sealed class MatchStatisticsSO : ScriptableObject
    {
        // ── Read-only statistics ───────────────────────────────────────────────

        /// <summary>Total damage the player dealt to the enemy this match.</summary>
        public float TotalDamageDealt { get; private set; }

        /// <summary>Total damage the player received from the enemy this match.</summary>
        public float TotalDamageTaken { get; private set; }

        /// <summary>Number of times the player successfully hit the enemy.</summary>
        public int HitCount { get; private set; }

        /// <summary>Number of times the player was hit by the enemy.</summary>
        public int HitsReceived { get; private set; }

        /// <summary>
        /// Ratio of damage dealt to total damage exchanged.
        /// Returns a value in [0, 1]:  1 = only dealt, 0 = only taken / none dealt.
        /// Returns 0 when both totals are zero (no hits recorded).
        /// </summary>
        public float DamageEfficiency
        {
            get
            {
                float total = TotalDamageDealt + TotalDamageTaken;
                return total > 0f ? TotalDamageDealt / total : 0f;
            }
        }

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Records one successful player attack.
        /// Ignores zero or negative values — only real damage counts.
        /// </summary>
        public void RecordDamageDealt(float amount)
        {
            if (amount <= 0f) return;
            TotalDamageDealt += amount;
            HitCount++;
        }

        /// <summary>
        /// Records one successful enemy attack against the player.
        /// Ignores zero or negative values.
        /// </summary>
        public void RecordDamageTaken(float amount)
        {
            if (amount <= 0f) return;
            TotalDamageTaken += amount;
            HitsReceived++;
        }

        /// <summary>
        /// Records a player attack using the full <see cref="DamageInfo"/> payload.
        /// Extracts <see cref="DamageInfo.amount"/> and delegates to
        /// <see cref="RecordDamageDealt(float)"/>.
        /// Enables direct wiring from a <c>DamageGameEventListener</c> UnityEvent.
        /// </summary>
        public void RecordDamageDealt(DamageInfo info) => RecordDamageDealt(info.amount);

        /// <summary>
        /// Records an enemy attack using the full <see cref="DamageInfo"/> payload.
        /// Extracts <see cref="DamageInfo.amount"/> and delegates to
        /// <see cref="RecordDamageTaken(float)"/>.
        /// Enables direct wiring from a <c>DamageGameEventListener</c> UnityEvent.
        /// </summary>
        public void RecordDamageTaken(DamageInfo info) => RecordDamageTaken(info.amount);

        /// <summary>
        /// Clears all accumulated statistics.
        /// Call at the start of every match (HandleMatchStarted) before gameplay begins.
        /// </summary>
        public void Reset()
        {
            TotalDamageDealt = 0f;
            TotalDamageTaken = 0f;
            HitCount         = 0;
            HitsReceived     = 0;
        }
    }
}
