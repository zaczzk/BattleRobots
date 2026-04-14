using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// MonoBehaviour that shows a proximity-to-hazard warning banner when the
    /// player robot enters within <see cref="WarningDistance"/> metres of any
    /// registered arena hazard zone.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   Each Update → CheckProximity() iterates paired (_hazardZones, _hazardTransforms)
    ///   arrays, finds the nearest hazard, and activates/deactivates the warning panel.
    ///   On the first frame the player enters danger range, _onEnterDanger is raised once.
    ///
    /// ── Inspector wiring ─────────────────────────────────────────────────────
    ///   _hazardZones        → HazardZoneSO assets (one per hazard area).
    ///   _hazardTransforms   → World-space Transform for each zone centre (parallel array).
    ///   _playerTransform    → The player robot's Transform (set at runtime or inspector).
    ///   _warningDistance    → Trigger radius in metres (default 5).
    ///   _onEnterDanger      → VoidGameEvent raised when entering danger range.
    ///   _warningPanel       → Root panel to show/hide.
    ///   _warningLabel       → Text showing "HazardType — Xm".
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no BattleRobots.Physics references.
    ///   - CheckProximity() uses only float arithmetic — no heap allocations.
    ///   - String label rebuilt only when state changes (entering danger).
    ///   - All fields optional and null-safe.
    ///   - DisallowMultipleComponent — one warning banner per canvas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArenaHazardWarningController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("HazardZoneSO assets describing each hazard area. Parallel to _hazardTransforms.")]
        [SerializeField] private HazardZoneSO[] _hazardZones;

        [Header("Tracking (optional)")]
        [Tooltip("World-space Transform for the centre of each hazard zone. " +
                 "Must be parallel to _hazardZones (same index = same zone).")]
        [SerializeField] private Transform[] _hazardTransforms;

        [Tooltip("The player robot's Transform. " +
                 "Assign at runtime via PlayerTransform property or wire in inspector.")]
        [SerializeField] private Transform _playerTransform;

        [Tooltip("Proximity threshold in metres. " +
                 "When the player is within this distance of any hazard, the warning shows.")]
        [SerializeField, Min(0.1f)] private float _warningDistance = 5f;

        [Header("Event Channel — Out (optional)")]
        [Tooltip("Raised once each time the player enters the danger range of a hazard.")]
        [SerializeField] private VoidGameEvent _onEnterDanger;

        [Header("UI Refs (optional)")]
        [Tooltip("Root warning panel; shown when in danger range, hidden otherwise.")]
        [SerializeField] private GameObject _warningPanel;

        [Tooltip("Text label; displays 'HazardType — X.Xm' when in range.")]
        [SerializeField] private Text _warningLabel;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool _wasInDanger;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Update() => CheckProximity();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Checks proximity of <see cref="PlayerTransform"/> to all registered hazard zones.
        /// Activates <c>_warningPanel</c> and updates <c>_warningLabel</c> when in range.
        /// Raises <c>_onEnterDanger</c> on the first frame the player enters danger range.
        /// Safe to call directly from tests.
        /// </summary>
        public void CheckProximity()
        {
            if (_playerTransform == null)
            {
                _warningPanel?.SetActive(false);
                _wasInDanger = false;
                return;
            }

            int zoneCount = _hazardZones   != null ? _hazardZones.Length   : 0;
            int tfCount   = _hazardTransforms != null ? _hazardTransforms.Length : 0;
            int count     = zoneCount < tfCount ? zoneCount : tfCount;

            float sqrWarn    = _warningDistance * _warningDistance;
            float nearestSqr = float.MaxValue;
            int   nearestIdx = -1;

            Vector3 playerPos = _playerTransform.position;

            for (int i = 0; i < count; i++)
            {
                if (_hazardTransforms[i] == null) continue;
                float sqr = (playerPos - _hazardTransforms[i].position).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearestIdx = i;
                }
            }

            bool inDanger = nearestIdx >= 0 && nearestSqr <= sqrWarn;

            if (inDanger)
            {
                _warningPanel?.SetActive(true);

                // Rebuild label only on entering danger to minimise allocations.
                if (!_wasInDanger && _warningLabel != null)
                {
                    HazardZoneSO zone = (_hazardZones != null && nearestIdx < _hazardZones.Length)
                        ? _hazardZones[nearestIdx]
                        : null;

                    float dist = Mathf.Sqrt(nearestSqr);
                    _warningLabel.text = zone != null
                        ? string.Format("{0} \u2014 {1:F1}m", zone.HazardType, dist)
                        : string.Format("Hazard \u2014 {0:F1}m", dist);
                }
            }
            else
            {
                _warningPanel?.SetActive(false);
            }

            if (inDanger && !_wasInDanger)
                _onEnterDanger?.Raise();

            _wasInDanger = inDanger;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Distance threshold in metres that triggers the warning.</summary>
        public float WarningDistance => _warningDistance;

        /// <summary>
        /// The tracked player Transform. Assignable at runtime for late-binding scenarios
        /// (e.g. after player robot is spawned).
        /// </summary>
        public Transform PlayerTransform
        {
            get => _playerTransform;
            set => _playerTransform = value;
        }

        /// <summary>True when the player is currently within danger range of at least one hazard.</summary>
        public bool IsInDanger => _wasInDanger;
    }
}
