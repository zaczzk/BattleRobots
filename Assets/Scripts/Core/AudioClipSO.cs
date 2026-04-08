using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// ScriptableObject wrapping a single AudioClip with randomised volume/pitch ranges.
    ///
    /// Architecture:
    ///   - SO asset is immutable at runtime; Volume/Pitch are computed read-only properties.
    ///   - Random.Range returns a float — zero heap allocation.
    ///   - Referenced by AudioManager and raised through AudioGameEvent channels.
    ///
    /// Create via Assets ▶ BattleRobots ▶ Audio ▶ AudioClipSO.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Audio/AudioClipSO", order = 0)]
    public sealed class AudioClipSO : ScriptableObject
    {
        [Header("Clip")]
        [SerializeField] private AudioClip _clip;

        [Header("Volume Range")]
        [SerializeField, Range(0f, 1f)] private float _volumeMin = 0.85f;
        [SerializeField, Range(0f, 1f)] private float _volumeMax = 1.00f;

        [Header("Pitch Range")]
        [SerializeField, Range(0.5f, 2f)] private float _pitchMin = 0.90f;
        [SerializeField, Range(0.5f, 2f)] private float _pitchMax = 1.10f;

        // ── Runtime accessors ─────────────────────────────────────────────────

        /// <summary>The underlying AudioClip asset.</summary>
        public AudioClip Clip => _clip;

        /// <summary>
        /// Samples a random volume within [_volumeMin, _volumeMax].
        /// Each access returns a fresh sample — no heap allocation.
        /// </summary>
        public float Volume => UnityEngine.Random.Range(_volumeMin, _volumeMax);

        /// <summary>
        /// Samples a random pitch within [_pitchMin, _pitchMax].
        /// Each access returns a fresh sample — no heap allocation.
        /// </summary>
        public float Pitch => UnityEngine.Random.Range(_pitchMin, _pitchMax);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_volumeMin > _volumeMax)
                Debug.LogWarning($"[AudioClipSO] '{name}': volumeMin > volumeMax — values will be swapped at runtime.");
            if (_pitchMin > _pitchMax)
                Debug.LogWarning($"[AudioClipSO] '{name}': pitchMin > pitchMax — values will be swapped at runtime.");
        }
#endif
    }
}
