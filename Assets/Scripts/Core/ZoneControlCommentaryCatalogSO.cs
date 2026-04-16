using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Event types that can trigger a commentary message in
    /// <see cref="ZoneControlCommentaryCatalogSO"/>.
    /// </summary>
    public enum ZoneControlCommentaryEventType
    {
        /// <summary>Player is capturing zones at a fast pace.</summary>
        FastPace      = 0,
        /// <summary>Player is capturing zones at a slow pace.</summary>
        SlowPace      = 1,
        /// <summary>A match rating has been calculated and set.</summary>
        RatingSet     = 2,
        /// <summary>A zone was captured.</summary>
        ZoneCaptured  = 3,
    }

    /// <summary>
    /// Immutable-at-runtime ScriptableObject that holds pools of commentary
    /// messages for each <see cref="ZoneControlCommentaryEventType"/>.
    ///
    /// ── Message selection ──────────────────────────────────────────────────────
    ///   Messages are served round-robin per event type.
    ///   Indices reset via <see cref="ResetIndices"/> (called in OnEnable).
    ///   An empty or null pool for a type returns <see cref="string.Empty"/>.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Message arrays are serialised and immutable at runtime.
    ///   - Runtime indices are not serialised; reset on domain reload.
    ///   - Zero heap allocation on <see cref="GetMessage"/>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCommentaryCatalog.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCommentaryCatalog", order = 27)]
    public sealed class ZoneControlCommentaryCatalogSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Fast Pace Messages")]
        [SerializeField] private string[] _fastPaceMessages =
        {
            "Fast pace!", "Speed run!", "Blazing captures!"
        };

        [Header("Slow Pace Messages")]
        [SerializeField] private string[] _slowPaceMessages =
        {
            "Slow down.", "Steady pace.", "Take it easy."
        };

        [Header("Rating Set Messages")]
        [SerializeField] private string[] _ratingSetMessages =
        {
            "Rating updated!", "Performance logged!", "Stars awarded!"
        };

        [Header("Zone Captured Messages")]
        [SerializeField] private string[] _zoneCapturedMessages =
        {
            "Zone captured!", "Territory claimed!", "Capture secured!"
        };

        // ── Runtime indices (not serialised) ──────────────────────────────────

        private int _fastIndex;
        private int _slowIndex;
        private int _ratingIndex;
        private int _captureIndex;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => ResetIndices();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Number of fast-pace messages defined.</summary>
        public int FastPaceCount     => _fastPaceMessages     != null ? _fastPaceMessages.Length     : 0;

        /// <summary>Number of slow-pace messages defined.</summary>
        public int SlowPaceCount     => _slowPaceMessages     != null ? _slowPaceMessages.Length     : 0;

        /// <summary>Number of rating-set messages defined.</summary>
        public int RatingSetCount    => _ratingSetMessages    != null ? _ratingSetMessages.Length    : 0;

        /// <summary>Number of zone-captured messages defined.</summary>
        public int ZoneCapturedCount => _zoneCapturedMessages != null ? _zoneCapturedMessages.Length : 0;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the next commentary message for the given event type using
        /// round-robin selection.  Returns <see cref="string.Empty"/> when the
        /// pool for that type is empty or null.
        /// Zero heap allocation.
        /// </summary>
        public string GetMessage(ZoneControlCommentaryEventType eventType)
        {
            switch (eventType)
            {
                case ZoneControlCommentaryEventType.FastPace:
                    return NextFrom(_fastPaceMessages, ref _fastIndex);

                case ZoneControlCommentaryEventType.SlowPace:
                    return NextFrom(_slowPaceMessages, ref _slowIndex);

                case ZoneControlCommentaryEventType.RatingSet:
                    return NextFrom(_ratingSetMessages, ref _ratingIndex);

                case ZoneControlCommentaryEventType.ZoneCaptured:
                    return NextFrom(_zoneCapturedMessages, ref _captureIndex);

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Resets all round-robin indices to 0.
        /// Called automatically in <c>OnEnable</c>.
        /// </summary>
        public void ResetIndices()
        {
            _fastIndex    = 0;
            _slowIndex    = 0;
            _ratingIndex  = 0;
            _captureIndex = 0;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static string NextFrom(string[] pool, ref int index)
        {
            if (pool == null || pool.Length == 0) return string.Empty;
            string msg = pool[index];
            index = (index + 1) % pool.Length;
            return msg ?? string.Empty;
        }
    }
}
