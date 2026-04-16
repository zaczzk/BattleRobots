using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// ScriptableObject that defines the daily-challenge generation rules for the
    /// zone-control mode.
    ///
    /// ── Daily target generation ─────────────────────────────────────────────────
    ///   <see cref="GetTodayTarget"/> seeds a deterministic RNG with the hash of
    ///   <see cref="System.DateTime.Today"/>, then picks one value from
    ///   <see cref="_possibleTargets"/>.  This means all sessions on the same
    ///   calendar day receive the same target.
    ///
    /// ── Countdown label ────────────────────────────────────────────────────────
    ///   <see cref="GetSecondsUntilReset"/> returns the number of whole seconds
    ///   until midnight UTC so a HUD can display "Resets in Xh Ym".
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Immutable at runtime — all public API is read-only / query-only.
    ///   - Zero heap allocation on hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlDailyChallengeConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlDailyChallengeConfig", order = 30)]
    public sealed class ZoneControlDailyChallengeConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Challenge Definition")]
        [Tooltip("The metric this daily challenge measures.")]
        [SerializeField] private ZoneControlChallengeType _challengeType =
            ZoneControlChallengeType.ZoneCount;

        [Header("Possible Daily Targets")]
        [Tooltip("Pool of target values from which today's challenge is drawn. " +
                 "The selection is deterministic and date-seeded.")]
        [SerializeField] private float[] _possibleTargets = { 5f, 10f, 15f, 20f, 30f };

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The metric type this daily challenge measures.</summary>
        public ZoneControlChallengeType ChallengeType => _challengeType;

        /// <summary>Number of possible daily target values in the pool.</summary>
        public int PossibleTargetCount =>
            _possibleTargets != null ? _possibleTargets.Length : 0;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the target value for today's challenge.
        /// Uses a date-seeded deterministic RNG so all sessions on the same calendar
        /// day return the same value.
        /// Returns the first element (or 0 when the pool is empty / null).
        /// Zero heap allocation.
        /// </summary>
        public float GetTodayTarget()
        {
            if (_possibleTargets == null || _possibleTargets.Length == 0)
                return 0f;

            // Seed with today's date hash for determinism per calendar day.
            int seed = DateTime.Today.GetHashCode();
            // Use System.Random (not Unity's) so it works in EditMode tests too.
            var rng   = new System.Random(seed);
            int index = rng.Next(0, _possibleTargets.Length);
            return _possibleTargets[index];
        }

        /// <summary>
        /// Returns the number of whole seconds remaining until midnight UTC.
        /// Useful for a "Resets in Xh Ym" countdown label.
        /// </summary>
        public int GetSecondsUntilReset()
        {
            DateTime now      = DateTime.UtcNow;
            DateTime midnight = now.Date.AddDays(1); // next UTC midnight
            return (int)(midnight - now).TotalSeconds;
        }

        // ── Validation ────────────────────────────────────────────────────────

        private void OnValidate()
        {
            if (_possibleTargets == null) return;
            for (int i = 0; i < _possibleTargets.Length; i++)
                _possibleTargets[i] = Mathf.Max(0f, _possibleTargets[i]);
        }
    }
}
