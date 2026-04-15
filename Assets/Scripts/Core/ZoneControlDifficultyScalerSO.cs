using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable ScriptableObject that maps a difficulty index to a zone-capture
    /// time scale factor, making zones harder or easier to capture based on the
    /// selected difficulty preset.
    ///
    /// ── Usage ──────────────────────────────────────────────────────────────────
    ///   • Assign this SO to a <see cref="BattleRobots.Physics.ZoneControlDifficultyScaler"/>.
    ///   • The scaler reads <see cref="GetCaptureTimeScale"/> at match start and
    ///     pushes the value to each <see cref="BattleRobots.Physics.ControlZoneController"/>
    ///     via <c>SetCaptureTimeScale</c>.
    ///   • A scale &gt; 1 means the zone takes longer to capture (harder).
    ///   • A scale &lt; 1 means the zone captures faster (easier).
    ///   • Out-of-range indices return 1.0 (no scaling).
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - SO is immutable at runtime — no mutable state.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlDifficultyScaler.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlDifficultyScaler", order = 20)]
    public sealed class ZoneControlDifficultyScalerSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Capture Time Scales (per difficulty index)")]
        [Tooltip("Index 0 = easiest difficulty. Each value scales the capture time. " +
                 "1.0 = normal speed. > 1.0 = slower capture (harder). " +
                 "< 1.0 = faster capture (easier). Minimum 0.1.")]
        [SerializeField] private float[] _captureTimeScales = { 0.5f, 1.0f, 2.0f };

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the capture time scale factor for the given difficulty index.
        /// Returns 1.0 when the index is out of range or the array is empty.
        /// Zero allocation.
        /// </summary>
        public float GetCaptureTimeScale(int difficultyIndex)
        {
            if (_captureTimeScales == null || _captureTimeScales.Length == 0) return 1f;
            if (difficultyIndex < 0 || difficultyIndex >= _captureTimeScales.Length) return 1f;
            return _captureTimeScales[difficultyIndex];
        }

        /// <summary>Number of difficulty levels configured.</summary>
        public int ScaleCount => _captureTimeScales != null ? _captureTimeScales.Length : 0;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_captureTimeScales == null || _captureTimeScales.Length == 0)
            {
                Debug.LogWarning("[ZoneControlDifficultyScalerSO] _captureTimeScales is empty. " +
                                 "GetCaptureTimeScale will always return 1.", this);
                return;
            }

            for (int i = 0; i < _captureTimeScales.Length; i++)
            {
                if (_captureTimeScales[i] < 0.1f)
                {
                    _captureTimeScales[i] = 0.1f;
                    Debug.LogWarning($"[ZoneControlDifficultyScalerSO] Scale at index {i} " +
                                     "clamped to 0.1 (minimum).", this);
                }
            }
        }
#endif
    }
}
