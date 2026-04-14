using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Data-driven SO that maps an average part-upgrade tier to a letter grade
    /// (S / A / B / C / D) and a short advice string.
    ///
    /// ── Grade thresholds ─────────────────────────────────────────────────────────
    ///   Average tier ≥ SThreshold → "S"
    ///   Average tier ≥ AThreshold → "A"
    ///   Average tier ≥ BThreshold → "B"
    ///   Average tier ≥ CThreshold → "C"
    ///   otherwise                 → "D"
    ///
    ///   OnValidate clamps thresholds so S ≥ A ≥ B ≥ C ≥ 1.
    ///
    /// ── Usage ─────────────────────────────────────────────────────────────────────
    ///   <see cref="BattleRobots.UI.BuildRatingController"/> calls
    ///   <see cref="GetGrade"/> with the result of
    ///   <c>BuildRatingController.ComputeAverageTier()</c> then passes that grade
    ///   to <see cref="GetAdvice"/> for the advice string.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ BuildGradeConfig.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/BuildGradeConfig",
        fileName = "BuildGradeConfig")]
    public sealed class BuildGradeConfig : ScriptableObject
    {
        // ── Inspector — Thresholds ─────────────────────────────────────────────

        [Header("Grade Thresholds (minimum average upgrade tier)")]
        [Tooltip("Minimum average tier required for grade S.")]
        [SerializeField, Min(1)] private int _sThreshold = 4;

        [Tooltip("Minimum average tier required for grade A.")]
        [SerializeField, Min(1)] private int _aThreshold = 3;

        [Tooltip("Minimum average tier required for grade B.")]
        [SerializeField, Min(1)] private int _bThreshold = 2;

        [Tooltip("Minimum average tier required for grade C.")]
        [SerializeField, Min(1)] private int _cThreshold = 1;

        // ── Inspector — Advice strings ─────────────────────────────────────────

        [Header("Advice per Grade")]
        [SerializeField] private string _sAdvice = "Outstanding build! Maximum potential unlocked.";
        [SerializeField] private string _aAdvice = "Excellent build! A few upgrades could push further.";
        [SerializeField] private string _bAdvice = "Solid build. Keep upgrading for better results.";
        [SerializeField] private string _cAdvice = "Room to grow. Prioritize upgrading key parts.";
        [SerializeField] private string _dAdvice = "Consider upgrading your parts before the next match.";

        // ── Public properties ──────────────────────────────────────────────────

        public int SThreshold => _sThreshold;
        public int AThreshold => _aThreshold;
        public int BThreshold => _bThreshold;
        public int CThreshold => _cThreshold;

        /// <summary>Advice string for grade S.</summary>
        public string SAdvice => _sAdvice;
        /// <summary>Advice string for grade A.</summary>
        public string AAdvice => _aAdvice;
        /// <summary>Advice string for grade B.</summary>
        public string BAdvice => _bAdvice;
        /// <summary>Advice string for grade C.</summary>
        public string CAdvice => _cAdvice;
        /// <summary>Advice string for grade D (fallback).</summary>
        public string DAdvice => _dAdvice;

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Maps <paramref name="averageTier"/> to a letter grade.
        /// Returns "S", "A", "B", "C", or "D".
        /// </summary>
        public string GetGrade(float averageTier)
        {
            if (averageTier >= _sThreshold) return "S";
            if (averageTier >= _aThreshold) return "A";
            if (averageTier >= _bThreshold) return "B";
            if (averageTier >= _cThreshold) return "C";
            return "D";
        }

        /// <summary>
        /// Returns the advice string for <paramref name="grade"/>.
        /// Falls back to the D-grade advice for unknown grades.
        /// </summary>
        public string GetAdvice(string grade)
        {
            switch (grade)
            {
                case "S": return _sAdvice;
                case "A": return _aAdvice;
                case "B": return _bAdvice;
                case "C": return _cAdvice;
                default:  return _dAdvice;
            }
        }

        // ── Editor validation ──────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            _sThreshold = Mathf.Max(1, _sThreshold);
            _aThreshold = Mathf.Clamp(_aThreshold, 1, _sThreshold);
            _bThreshold = Mathf.Clamp(_bThreshold, 1, _aThreshold);
            _cThreshold = Mathf.Clamp(_cThreshold, 1, _bThreshold);
        }
#endif
    }
}
