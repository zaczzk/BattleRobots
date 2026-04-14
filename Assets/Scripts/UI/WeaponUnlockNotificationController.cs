using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays a notification banner whenever the player's
    /// prestige event causes one or more <see cref="DamageType"/> weapon categories
    /// to become newly unlocked.
    ///
    /// ── Data flow ────────────────────────────────────────────────────────────────
    ///   Awake     → caches _onPrestigeDelegate; zero-alloc after Awake.
    ///   OnEnable  → subscribes _onPrestige → OnPrestige();
    ///               snapshots _previousPrestigeCount from _prestigeSystem (or 0).
    ///   OnPrestige() → for each DamageType: was locked before? now unlocked? → ShowNotification.
    ///                  Updates _previousPrestigeCount to the new rank.
    ///   ShowNotification(type) → sets _notificationLabel text; activates panel;
    ///                            sets _displayTimer; raises _onNewTypeUnlocked.
    ///   Update    → decrements _displayTimer; hides panel when expired.
    ///   OnDisable → unsubscribes _onPrestige; hides panel immediately.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • DisallowMultipleComponent — one notification banner per canvas.
    ///   • All UI fields optional — assign only those present in the scene.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _unlockConfig      → WeaponTypeUnlockConfig asset (per-type prestige gates).
    ///   _prestigeSystem    → shared PrestigeSystemSO (reads new prestige count on event).
    ///   _onPrestige        → same VoidGameEvent as PrestigeSystemSO._onPrestige.
    ///   _onNewTypeUnlocked → optional out-channel; raised once per newly-unlocked type.
    ///   _notificationPanel → banner panel root; activated when a new type unlocks.
    ///   _notificationLabel → Text that receives "{TypeName} weapon type unlocked!".
    ///   _notificationDuration → seconds the banner stays visible (default 3).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponUnlockNotificationController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Config SO that specifies the prestige rank required per DamageType. " +
                 "Leave null to disable the notification system (no unlock gating).")]
        [SerializeField] private WeaponTypeUnlockConfig _unlockConfig;

        [Tooltip("Runtime prestige SO. Read on each _onPrestige event to compute newly-unlocked types. " +
                 "Leave null to treat prestige count as 0.")]
        [SerializeField] private PrestigeSystemSO _prestigeSystem;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channel — In")]
        [Tooltip("Raised each time the player earns a new prestige rank. " +
                 "Triggers the unlock comparison and banner display.")]
        [SerializeField] private VoidGameEvent _onPrestige;

        [Header("Event Channel — Out")]
        [Tooltip("Raised once for each DamageType that becomes newly unlocked on a prestige event. " +
                 "Leave null if no system needs to react to individual type unlocks.")]
        [SerializeField] private VoidGameEvent _onNewTypeUnlocked;

        // ── Inspector — UI ────────────────────────────────────────────────────

        [Header("UI (optional)")]
        [Tooltip("Root notification banner panel. Activated when a type unlocks; " +
                 "deactivated when the display timer expires.")]
        [SerializeField] private GameObject _notificationPanel;

        [Tooltip("Text label that receives the unlock message. " +
                 "Format: '{TypeName} weapon type unlocked!'")]
        [SerializeField] private Text _notificationLabel;

        // ── Inspector — Settings ──────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Seconds the notification banner remains visible after showing.")]
        [SerializeField, Min(0.1f)] private float _notificationDuration = 3f;

        // ── Private state ─────────────────────────────────────────────────────

        private static readonly DamageType[] s_allTypes =
        {
            DamageType.Physical,
            DamageType.Energy,
            DamageType.Thermal,
            DamageType.Shock,
        };

        private Action _onPrestigeDelegate;
        private int    _previousPrestigeCount;
        private float  _displayTimer;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _onPrestigeDelegate = OnPrestige;
        }

        private void OnEnable()
        {
            _onPrestige?.RegisterCallback(_onPrestigeDelegate);
            _previousPrestigeCount = _prestigeSystem?.PrestigeCount ?? 0;
        }

        private void OnDisable()
        {
            _onPrestige?.UnregisterCallback(_onPrestigeDelegate);
            _notificationPanel?.SetActive(false);
            _displayTimer = 0f;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Advances the display timer by <paramref name="dt"/> seconds and hides the
        /// notification panel when the timer expires.
        ///
        /// <para>Exposed as a public method so tests can drive the timer directly
        /// without relying on <c>Time.deltaTime</c> being non-zero.</para>
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
        /// Called each time the player earns a new prestige rank.
        ///
        /// <para>Compares the previous prestige count against the new count for every
        /// <see cref="DamageType"/>.  When a type transitions from locked to unlocked,
        /// <see cref="ShowNotification"/> is called and <c>_onNewTypeUnlocked</c> fires.</para>
        ///
        /// <para>Null <c>_unlockConfig</c> → no types are checked (no notifications).</para>
        /// <para>Null <c>_prestigeSystem</c> → new count treated as 0.</para>
        /// </summary>
        private void OnPrestige()
        {
            int newCount = _prestigeSystem?.PrestigeCount ?? 0;

            if (_unlockConfig != null)
            {
                for (int i = 0; i < s_allTypes.Length; i++)
                {
                    DamageType type          = s_allTypes[i];
                    bool       wasLocked     = !_unlockConfig.IsUnlocked(type, _previousPrestigeCount);
                    bool       isNowUnlocked =  _unlockConfig.IsUnlocked(type, newCount);

                    if (wasLocked && isNowUnlocked)
                        ShowNotification(type);
                }
            }

            _previousPrestigeCount = newCount;
        }

        /// <summary>
        /// Activates the notification panel with a message for <paramref name="type"/>,
        /// resets the display timer, and raises <c>_onNewTypeUnlocked</c>.
        /// </summary>
        private void ShowNotification(DamageType type)
        {
            if (_notificationLabel != null)
                _notificationLabel.text = $"{type} weapon type unlocked!";

            _notificationPanel?.SetActive(true);
            _displayTimer = _notificationDuration;
            _onNewTypeUnlocked?.Raise();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The currently assigned <see cref="WeaponTypeUnlockConfig"/>. May be null.</summary>
        public WeaponTypeUnlockConfig UnlockConfig => _unlockConfig;

        /// <summary>The currently assigned <see cref="PrestigeSystemSO"/>. May be null.</summary>
        public PrestigeSystemSO PrestigeSystem => _prestigeSystem;

        /// <summary>Seconds the notification banner remains visible. Defaults to 3.</summary>
        public float NotificationDuration => _notificationDuration;

        /// <summary>Remaining display time in seconds. 0 when the panel is hidden.</summary>
        public float DisplayTimer => _displayTimer;
    }
}
