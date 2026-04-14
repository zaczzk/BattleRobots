using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Monitors the distance between the robot and each registered arena hazard zone,
    /// then shows a proximity warning banner when the closest hazard is within
    /// <see cref="_warningDistance"/> units.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────────
    ///   Every <c>Update</c> tick → <see cref="CheckProximity"/> scans parallel arrays
    ///   <c>_hazardZones</c> and <c>_hazardTransforms</c>, finds the nearest hazard
    ///   within warning range, and updates the banner labels.
    ///   When all hazards are out of range the banner panel is hidden.
    ///
    /// ── Array alignment rule ──────────────────────────────────────────────────────
    ///   <c>_hazardZones[i]</c> and <c>_hazardTransforms[i]</c> must reference the
    ///   same hazard. Entries with null SO or null Transform are silently skipped.
    ///   If arrays have different lengths the shorter length is used.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - No heap allocations in Update; only struct arithmetic (Vector3 distance).
    ///   - DisallowMultipleComponent — one warning banner per canvas.
    ///
    /// ── Inspector wiring ──────────────────────────────────────────────────────────
    ///   _hazardZones        → Array of HazardZoneSO assets (one per hazard).
    ///   _hazardTransforms   → Parallel array of Transforms for each hazard position.
    ///   _robotTransform     → The robot (player) Transform for distance measurement.
    ///   _warningDistance    → Threshold in world units; default 10.
    ///   _bannerPanel        → Root panel; shown when hazard is near, hidden when safe.
    ///   _hazardNameLabel    → Text set to the nearest hazard type name.
    ///   _distanceLabel      → Text set to "{N}m" (rounded distance in metres).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArenaHazardWarningController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Array of HazardZoneSO assets — one entry per hazard. " +
                 "Must be aligned with _hazardTransforms.")]
        [SerializeField] private HazardZoneSO[] _hazardZones;

        [Tooltip("Parallel array of world-space Transforms for each hazard. " +
                 "Index i must match _hazardZones[i].")]
        [SerializeField] private Transform[] _hazardTransforms;

        [Tooltip("Transform of the robot being watched (typically the player robot's root).")]
        [SerializeField] private Transform _robotTransform;

        [Header("Proximity Settings")]
        [Tooltip("Distance in world units below which the warning banner is shown.")]
        [SerializeField, Min(0.1f)] private float _warningDistance = 10f;

        [Header("UI References (optional)")]
        [Tooltip("Root panel shown when a hazard is within warning range.")]
        [SerializeField] private GameObject _bannerPanel;

        [Tooltip("Text label updated with the nearest hazard type name (e.g. 'Lava').")]
        [SerializeField] private Text _hazardNameLabel;

        [Tooltip("Text label updated with the distance in metres, e.g. '7m'.")]
        [SerializeField] private Text _distanceLabel;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Update()
        {
            CheckProximity();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Scans all hazard zones for proximity, then updates the warning banner.
        /// Callable directly from tests or from external systems (e.g. a manual tick).
        /// Fully null-safe and zero-allocation.
        /// </summary>
        public void CheckProximity()
        {
            if (_robotTransform == null || _hazardZones == null || _hazardTransforms == null)
            {
                _bannerPanel?.SetActive(false);
                return;
            }

            int count = Mathf.Min(_hazardZones.Length, _hazardTransforms.Length);

            int   nearestIndex = -1;
            float nearestDist  = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (_hazardZones[i] == null || _hazardTransforms[i] == null)
                    continue;

                float dist = Vector3.Distance(_robotTransform.position,
                                              _hazardTransforms[i].position);

                if (dist <= _warningDistance && dist < nearestDist)
                {
                    nearestDist  = dist;
                    nearestIndex = i;
                }
            }

            if (nearestIndex >= 0)
            {
                _bannerPanel?.SetActive(true);

                if (_hazardNameLabel != null)
                    _hazardNameLabel.text = _hazardZones[nearestIndex].HazardType.ToString();

                if (_distanceLabel != null)
                    _distanceLabel.text = string.Format("{0}m", Mathf.RoundToInt(nearestDist));
            }
            else
            {
                _bannerPanel?.SetActive(false);
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Distance threshold for showing the warning banner.</summary>
        public float WarningDistance => _warningDistance;

        /// <summary>The robot transform being watched. May be null.</summary>
        public Transform RobotTransform => _robotTransform;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            _warningDistance = Mathf.Max(0.1f, _warningDistance);

            if (_hazardZones != null && _hazardTransforms != null
                && _hazardZones.Length != _hazardTransforms.Length)
            {
                Debug.LogWarning($"[ArenaHazardWarningController] '{name}': " +
                                 "_hazardZones and _hazardTransforms have different lengths. " +
                                 "Shorter array length will be used.");
            }
        }
#endif
    }
}
