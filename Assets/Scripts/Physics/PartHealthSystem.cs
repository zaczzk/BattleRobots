using System;
using System.Collections.Generic;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Distributes incoming hit damage across the robot's independently-tracked parts.
    ///
    /// ── Responsibilities ──────────────────────────────────────────────────────
    ///   Each robot equipped with this MB holds a list of <see cref="PartEntry"/> structs:
    ///   a named slot (partId) paired with a <see cref="PartConditionSO"/> that tracks that
    ///   part's individual HP. When DamageReceiver's TakeDamage path calls
    ///   <see cref="DistributeDamage"/>, the damage is applied to a randomly chosen
    ///   <em>living</em> part, creating tactical part-destruction gameplay without coupling
    ///   Physics to UI or requiring a per-part collider.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Add this MB to the robot root GameObject (alongside DamageReceiver).
    ///   2. Populate _parts in the Inspector: one PartEntry per equipped part slot.
    ///      Assign each entry's _condition → a PartConditionSO asset (one per slot).
    ///   3. On the robot's DamageReceiver, assign _partHealthSystem → this MB.
    ///      DamageReceiver will automatically call DistributeDamage on each hit.
    ///   4. Optionally assign _onAllPartsDestroyed → a VoidGameEvent SO to signal
    ///      total part loss (e.g. trigger a disassembly VFX or alternate death path).
    ///   5. Call Reset() at match start (VoidGameEventListener MatchStarted → Reset).
    ///
    /// ── Architecture notes ─────────────────────────────────────────────────────
    ///   - BattleRobots.Physics namespace; no UI references.
    ///   - DistributeDamage, GetLivingPartCount, and AreAllPartsDestroyed are
    ///     zero-allocation on the hot path (struct array + int counting, no LINQ).
    ///   - _onAllPartsDestroyed fires at most once per match lifetime until Reset().
    ///   - Random.Range is the only non-deterministic element; seed via
    ///     UnityEngine.Random.InitState for test determinism if required.
    /// </summary>
    public sealed class PartHealthSystem : MonoBehaviour
    {
        // ── Nested types ──────────────────────────────────────────────────────

        /// <summary>
        /// Associates a named part slot with its independent HP blackboard SO.
        /// Stored as a value-type struct to avoid per-entry heap allocation.
        /// </summary>
        [Serializable]
        public struct PartEntry
        {
            [Tooltip("Identifies the part slot (matches PartDefinition.PartId). " +
                     "Used for diagnostics and future stat-modifier lookups.")]
            public string partId;

            [Tooltip("PartConditionSO tracking this slot's current and max HP.")]
            public PartConditionSO condition;
        }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Parts")]
        [Tooltip("One entry per equipped part that can independently absorb damage. " +
                 "Populate at design time or at runtime via Assemble() when the " +
                 "robot is assembled from a loadout.")]
        [SerializeField] private PartEntry[] _parts;

        [Header("Event Channel (optional)")]
        [Tooltip("Raised once per match when every part's PartConditionSO.IsDestroyed " +
                 "becomes true. Leave null to skip.")]
        [SerializeField] private VoidGameEvent _onAllPartsDestroyed;

        // ── Private ───────────────────────────────────────────────────────────

        // Guard so _onAllPartsDestroyed fires at most once until Reset() is called.
        private bool _allDestroyedFired;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Read-only view of all registered parts.</summary>
        public IReadOnlyList<PartEntry> Parts => _parts;

        /// <summary>
        /// True when every part in the list has IsDestroyed == true.
        /// False if the list is null or empty (no parts registered).
        /// </summary>
        public bool AreAllPartsDestroyed
        {
            get
            {
                if (_parts == null || _parts.Length == 0) return false;
                for (int i = 0; i < _parts.Length; i++)
                {
                    var cond = _parts[i].condition;
                    if (cond != null && !cond.IsDestroyed) return false;
                }
                return true;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Selects a random living part and applies <paramref name="amount"/> damage to it.
        /// Living = has a non-null PartConditionSO that is not yet destroyed.
        /// No-op when: amount ≤ 0, no parts registered, or all parts already destroyed.
        /// Fires <see cref="_onAllPartsDestroyed"/> (once) if the hit destroys the last part.
        /// Zero allocation on the hot path — struct array iteration + int arithmetic.
        /// </summary>
        public void DistributeDamage(float amount)
        {
            if (amount <= 0f || _parts == null || _parts.Length == 0) return;

            int living = GetLivingPartCount();
            if (living == 0) return;

            // Pick the nth living part (n is random in [0, living)).
            int targetRank = UnityEngine.Random.Range(0, living);
            int rank       = 0;
            for (int i = 0; i < _parts.Length; i++)
            {
                var cond = _parts[i].condition;
                if (cond == null || cond.IsDestroyed) continue;

                if (rank == targetRank)
                {
                    cond.TakeDamage(amount);
                    break;
                }
                rank++;
            }

            // Check all-destroyed condition once per damage call (fire at most once per Reset).
            if (!_allDestroyedFired && AreAllPartsDestroyed)
            {
                _allDestroyedFired = true;
                _onAllPartsDestroyed?.Raise();
            }
        }

        /// <summary>
        /// Returns the number of parts that are not yet destroyed.
        /// Zero-allocation: iterates the struct array with a plain for loop.
        /// </summary>
        public int GetLivingPartCount()
        {
            if (_parts == null) return 0;
            int count = 0;
            for (int i = 0; i < _parts.Length; i++)
            {
                var cond = _parts[i].condition;
                if (cond != null && !cond.IsDestroyed) count++;
            }
            return count;
        }

        /// <summary>
        /// Restores all parts to full HP and resets the all-destroyed guard.
        /// Call at match start (wire MatchStarted VoidGameEvent → Reset via
        /// VoidGameEventListener on the same GameObject).
        /// </summary>
        public void Reset()
        {
            _allDestroyedFired = false;
            if (_parts == null) return;
            for (int i = 0; i < _parts.Length; i++)
                _parts[i].condition?.ResetToMax();
        }
    }
}
