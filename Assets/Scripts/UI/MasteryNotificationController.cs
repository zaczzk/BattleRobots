using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays a "<c>TYPE MASTERED!</c>" notification banner
    /// whenever <see cref="DamageTypeMasterySO"/> raises <c>_onMasteryUnlocked</c>.
    ///
    /// ── Mastery detection ─────────────────────────────────────────────────────────
    ///   On <see cref="OnEnable"/> the controller snapshots the current mastery flags
    ///   (previous state). When the event fires it compares the snapshot to the current
    ///   state to identify which type just became mastered (first newly-mastered type
    ///   wins), then shows the banner and updates the snapshot.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────────────
    ///   Awake     → caches _onMasteryDelegate.
    ///   OnEnable  → subscribes _onMasteryUnlocked → OnMasteryUnlocked();
    ///               snapshots current mastery state as "previous".
    ///   OnMasteryUnlocked() → diff previous vs current; ShowNotification for first
    ///                         newly-mastered type; update snapshot.
    ///   Tick(dt)  → decrements _displayTimer; hides panel when expired.
    ///   Update    → calls Tick(Time.deltaTime).
    ///   OnDisable → unsubscribes; hides panel; resets timer.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • DisallowMultipleComponent — one mastery banner per canvas.
    ///   • All UI fields optional — assign only those present in the scene.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _mastery             → shared DamageTypeMasterySO (mastery flags source).
    ///   _onMasteryUnlocked   → same VoidGameEvent as DamageTypeMasterySO._onMasteryUnlocked.
    ///   _notificationPanel   → banner root panel (starts inactive).
    ///   _notificationLabel   → Text child that receives "Physical MASTERED!" etc.
    ///   _notificationDuration→ seconds the banner stays visible (default 3).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MasteryNotificationController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Runtime mastery SO. Provides per-type mastery flags. Leave null to disable.")]
        [SerializeField] private DamageTypeMasterySO _mastery;

        // ── Inspector — Event Channel ─────────────────────────────────────────

        [Header("Event Channel — In")]
        [Tooltip("Raised by DamageTypeMasterySO when any type first reaches its threshold. " +
                 "Triggers the banner display.")]
        [SerializeField] private VoidGameEvent _onMasteryUnlocked;

        // ── Inspector — UI ────────────────────────────────────────────────────

        [Header("UI (optional)")]
        [Tooltip("Root notification banner panel. Activated when a type is mastered; " +
                 "deactivated when the display timer expires.")]
        [SerializeField] private GameObject _notificationPanel;

        [Tooltip("Text label that receives the mastery message. " +
                 "Format: '{TypeName} MASTERED!'")]
        [SerializeField] private Text _notificationLabel;

        // ── Inspector — Settings ──────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Seconds the notification banner remains visible.")]
        [SerializeField, Min(0.1f)] private float _notificationDuration = 3f;

        // ── Private state ─────────────────────────────────────────────────────

        private static readonly DamageType[] s_allTypes =
        {
            DamageType.Physical,
            DamageType.Energy,
            DamageType.Thermal,
            DamageType.Shock,
        };

        private Action _onMasteryDelegate;
        private float  _displayTimer;

        // Previous mastery snapshot — used for diff detection on each event.
        private bool _prevPhysical;
        private bool _prevEnergy;
        private bool _prevThermal;
        private bool _prevShock;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _onMasteryDelegate = OnMasteryUnlocked;
        }

        private void OnEnable()
        {
            _onMasteryUnlocked?.RegisterCallback(_onMasteryDelegate);
            SnapshotPrevious();
        }

        private void OnDisable()
        {
            _onMasteryUnlocked?.UnregisterCallback(_onMasteryDelegate);
            _notificationPanel?.SetActive(false);
            _displayTimer = 0f;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Advances the display timer by <paramref name="dt"/> seconds and hides the panel
        /// when the timer expires.
        ///
        /// <para>Exposed as public so tests can drive the timer without relying on
        /// <c>Time.deltaTime</c> being non-zero.</para>
        /// </summary>
        public void Tick(float dt)
        {
            if (_displayTimer <= 0f) return;

            _displayTimer -= dt;
            if (_displayTimer <= 0f)
                _notificationPanel?.SetActive(false);
        }

        /// <summary>
        /// Called when _onMasteryUnlocked fires.
        /// Diffs the previous mastery snapshot against current state to find the first
        /// newly-mastered type; shows the banner for that type; then updates the snapshot.
        /// No-op when _mastery is null.
        /// </summary>
        private void OnMasteryUnlocked()
        {
            if (_mastery == null) return;

            for (int i = 0; i < s_allTypes.Length; i++)
            {
                DamageType type      = s_allTypes[i];
                bool       wasBefore = GetPrevMastered(type);
                bool       isNow     = _mastery.IsTypeMastered(type);

                if (!wasBefore && isNow)
                {
                    ShowNotification(type);
                    break; // Show one banner at a time; remaining types get their own event.
                }
            }

            SnapshotPrevious();
        }

        private void ShowNotification(DamageType type)
        {
            if (_notificationLabel != null)
                _notificationLabel.text = $"{type} MASTERED!";

            _notificationPanel?.SetActive(true);
            _displayTimer = _notificationDuration;
        }

        private void SnapshotPrevious()
        {
            if (_mastery == null)
            {
                _prevPhysical = false;
                _prevEnergy   = false;
                _prevThermal  = false;
                _prevShock    = false;
            }
            else
            {
                _prevPhysical = _mastery.IsTypeMastered(DamageType.Physical);
                _prevEnergy   = _mastery.IsTypeMastered(DamageType.Energy);
                _prevThermal  = _mastery.IsTypeMastered(DamageType.Thermal);
                _prevShock    = _mastery.IsTypeMastered(DamageType.Shock);
            }
        }

        private bool GetPrevMastered(DamageType type)
        {
            switch (type)
            {
                case DamageType.Physical: return _prevPhysical;
                case DamageType.Energy:   return _prevEnergy;
                case DamageType.Thermal:  return _prevThermal;
                case DamageType.Shock:    return _prevShock;
                default:                  return false;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The currently assigned <see cref="DamageTypeMasterySO"/>. May be null.</summary>
        public DamageTypeMasterySO Mastery => _mastery;

        /// <summary>Seconds the notification banner remains visible. Defaults to 3.</summary>
        public float NotificationDuration => _notificationDuration;

        /// <summary>Remaining display time in seconds. 0 when the panel is hidden.</summary>
        public float DisplayTimer => _displayTimer;
    }
}
