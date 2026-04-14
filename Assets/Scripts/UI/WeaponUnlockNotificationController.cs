using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI controller that shows a notification banner when the player earns a new
    /// prestige rank that unlocks previously-locked weapon <see cref="DamageType"/>s.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────────
    ///   On each <c>_onPrestige</c> event the controller compares the unlock state
    ///   before and after the prestige step using <see cref="WeaponTypeUnlockEvaluator"/>.
    ///   For every newly-unlocked <see cref="DamageType"/> it:
    ///   <list type="bullet">
    ///     <item>Raises <c>_onWeaponTypeUnlocked</c> (optional VoidGameEvent).</item>
    ///     <item>Appends the type name to a notification message string.</item>
    ///   </list>
    ///   The <c>_notificationPanel</c> is activated and <c>_notificationText</c> is
    ///   set to the composite message.  A timer-based auto-hide hides the panel after
    ///   <c>_displayDuration</c> seconds.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────────
    ///   Awake     → caches _prestigeDelegate; snapshots initial prestige count.
    ///   OnEnable  → snapshots current prestige count; subscribes _onPrestige; hides panel.
    ///   OnDisable → unsubscribes _onPrestige.
    ///   OnPrestige() → computes newly-unlocked types; shows panel when any found.
    ///   Update    → ticks display timer; hides panel when timer expires.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • <see cref="DisallowMultipleComponent"/> — one notification banner per canvas.
    ///   • All fields optional — null refs are handled safely.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────────
    ///   _unlockConfig           → WeaponTypeUnlockConfig asset.
    ///   _prestigeSystem         → shared PrestigeSystemSO.
    ///   _onPrestige             → same VoidGameEvent as PrestigeSystemSO._onPrestige.
    ///   _onWeaponTypeUnlocked   → VoidGameEvent raised once per newly-unlocked type.
    ///   _notificationPanel      → GameObject activated on unlock (optional).
    ///   _notificationText       → Text showing "X weapon unlocked!" message (optional).
    ///   _displayDuration        → seconds before the panel auto-hides (default 2.5).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponUnlockNotificationController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Config SO specifying the minimum prestige rank required per DamageType. " +
                 "Leave null to disable all unlock notifications.")]
        [SerializeField] private WeaponTypeUnlockConfig _unlockConfig;

        [Tooltip("Shared PrestigeSystemSO that holds the player's current prestige count. " +
                 "Leave null to disable all unlock notifications.")]
        [SerializeField] private PrestigeSystemSO _prestigeSystem;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channel — In")]
        [Tooltip("Same VoidGameEvent as PrestigeSystemSO._onPrestige. " +
                 "Triggers the unlock check each time the player earns a new prestige rank.")]
        [SerializeField] private VoidGameEvent _onPrestige;

        [Header("Event Channel — Out")]
        [Tooltip("Raised once for each weapon DamageType that becomes newly unlocked " +
                 "as a result of the current prestige step. Leave null if no system " +
                 "needs to react to individual unlock events.")]
        [SerializeField] private VoidGameEvent _onWeaponTypeUnlocked;

        // ── Inspector — UI ────────────────────────────────────────────────────

        [Header("UI (optional)")]
        [Tooltip("Panel activated when one or more weapon types are newly unlocked. " +
                 "Auto-hidden after _displayDuration seconds.")]
        [SerializeField] private GameObject _notificationPanel;

        [Tooltip("Text component populated with the names of newly-unlocked weapon types. " +
                 "Format: \"<Type> weapon unlocked!\" (joined by newlines for multiple types).")]
        [SerializeField] private Text _notificationText;

        [Tooltip("Seconds to display the notification panel before it auto-hides.")]
        [SerializeField, Min(0.1f)] private float _displayDuration = 2.5f;

        // ── Private state ─────────────────────────────────────────────────────

        private static readonly DamageType[] s_allTypes =
        {
            DamageType.Physical,
            DamageType.Energy,
            DamageType.Thermal,
            DamageType.Shock,
        };

        private Action _prestigeDelegate;
        private int    _lastKnownPrestigeCount;
        private float  _displayTimer;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _prestigeDelegate = OnPrestige;
        }

        private void OnEnable()
        {
            // Snapshot the current prestige count as the "before" baseline.
            _lastKnownPrestigeCount = _prestigeSystem?.PrestigeCount ?? 0;
            _onPrestige?.RegisterCallback(_prestigeDelegate);
            _notificationPanel?.SetActive(false);
        }

        private void OnDisable()
        {
            _onPrestige?.UnregisterCallback(_prestigeDelegate);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Decrements the auto-hide timer by <paramref name="dt"/> seconds and hides
        /// the notification panel when the timer reaches zero.
        /// Called from <see cref="Update"/> every frame; also exposed for testing.
        /// </summary>
        public void Tick(float dt)
        {
            if (_displayTimer <= 0f) return;

            _displayTimer -= dt;
            if (_displayTimer <= 0f)
                _notificationPanel?.SetActive(false);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Called when <c>_onPrestige</c> fires.
        /// Compares the unlock state before and after the prestige step and shows
        /// a notification for each newly-unlocked <see cref="DamageType"/>.
        /// Silent no-op when <c>_unlockConfig</c> or <c>_prestigeSystem</c> is null.
        /// </summary>
        private void OnPrestige()
        {
            if (_unlockConfig == null || _prestigeSystem == null) return;

            int oldCount = _lastKnownPrestigeCount;
            int newCount = _prestigeSystem.PrestigeCount;
            _lastKnownPrestigeCount = newCount;

            // Collect newly-unlocked types.
            var sb            = new StringBuilder();
            int unlockCount   = 0;

            for (int i = 0; i < s_allTypes.Length; i++)
            {
                DamageType type = s_allTypes[i];

                bool wasLocked  = !_unlockConfig.IsUnlocked(type, oldCount);
                bool nowUnlocked = _unlockConfig.IsUnlocked(type, newCount);

                if (wasLocked && nowUnlocked)
                {
                    if (unlockCount > 0) sb.Append('\n');
                    sb.Append($"{type} weapon unlocked!");
                    _onWeaponTypeUnlocked?.Raise();
                    unlockCount++;
                }
            }

            if (unlockCount > 0)
                ShowNotification(sb.ToString());
        }

        /// <summary>
        /// Activates the notification panel with the given message and starts the
        /// auto-hide timer.
        /// </summary>
        private void ShowNotification(string message)
        {
            if (_notificationText != null)
                _notificationText.text = message;

            _notificationPanel?.SetActive(true);
            _displayTimer = _displayDuration;
        }

        // ── Public API (for tests) ────────────────────────────────────────────

        /// <summary>The currently assigned <see cref="WeaponTypeUnlockConfig"/>. May be null.</summary>
        public WeaponTypeUnlockConfig UnlockConfig => _unlockConfig;

        /// <summary>The currently assigned <see cref="PrestigeSystemSO"/>. May be null.</summary>
        public PrestigeSystemSO PrestigeSystem => _prestigeSystem;

        /// <summary>Seconds until the notification panel auto-hides. Zero when hidden.</summary>
        public float DisplayTimer => _displayTimer;

        /// <summary>Configured display duration in seconds.</summary>
        public float DisplayDuration => _displayDuration;
    }
}
