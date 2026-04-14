using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Pre-match UI controller that displays the unlock status of each
    /// <see cref="DamageType"/> weapon based on the player's prestige rank.
    ///
    /// ── Display ──────────────────────────────────────────────────────────────────
    ///   <see cref="Refresh"/> instantiates one row per DamageType
    ///   (Physical / Energy / Thermal / Shock) inside <c>_listContainer</c>.
    ///   Each row's first <see cref="Text"/> receives the type name and the second
    ///   Text receives either "Unlocked" or the lock reason returned by
    ///   <see cref="WeaponTypeUnlockConfig.GetLockReason"/>.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate (zero-alloc after Awake).
    ///   OnEnable  → subscribes _onPrestige → Refresh(); calls Refresh().
    ///   OnDisable → unsubscribes _onPrestige.
    ///   Refresh() → destroys old row children; rebuilds four rows; null-safe.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • DisallowMultipleComponent — one unlock panel per canvas.
    ///   • All UI fields optional — assign only what the scene has.
    ///   • No Update / FixedUpdate loop.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _unlockConfig   → WeaponTypeUnlockConfig asset (set per-type prestige gates).
    ///   _prestigeSystem → shared PrestigeSystemSO (provides current prestige count).
    ///   _onPrestige     → same VoidGameEvent as PrestigeSystemSO._onPrestige.
    ///   _listContainer  → a ScrollRect Content Transform (VerticalLayoutGroup recommended).
    ///   _rowPrefab      → a prefab with ≥ 2 Text components (type name + status).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponTypeUnlockController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Config SO that sets the prestige rank required per DamageType. " +
                 "Leave null to disable the panel (no unlock gating).")]
        [SerializeField] private WeaponTypeUnlockConfig _unlockConfig;

        [Tooltip("Runtime prestige SO. Provides the player's current prestige count. " +
                 "Leave null to treat prestige count as 0 (all non-zero requirements are locked).")]
        [SerializeField] private PrestigeSystemSO _prestigeSystem;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channel — In")]
        [Tooltip("Raised each time the player earns a new prestige rank. " +
                 "Triggers Refresh() to update the unlock display. Leave null to disable auto-refresh.")]
        [SerializeField] private VoidGameEvent _onPrestige;

        // ── Inspector — UI ────────────────────────────────────────────────────

        [Header("UI (optional)")]
        [Tooltip("Parent Transform for the per-type unlock rows. " +
                 "Requires _rowPrefab to instantiate rows. Leave null to skip row generation.")]
        [SerializeField] private Transform _listContainer;

        [Tooltip("Row prefab instantiated once per DamageType. " +
                 "The first Text child receives the damage type name; " +
                 "the second Text child receives the unlock status or lock reason.")]
        [SerializeField] private GameObject _rowPrefab;

        // ── Private state ─────────────────────────────────────────────────────

        private static readonly DamageType[] s_allTypes =
        {
            DamageType.Physical,
            DamageType.Energy,
            DamageType.Thermal,
            DamageType.Shock,
        };

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPrestige?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPrestige?.UnregisterCallback(_refreshDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Destroys existing rows and rebuilds the per-type unlock status list.
        ///
        /// <para>One row is instantiated per <see cref="DamageType"/>
        /// (Physical / Energy / Thermal / Shock).  The first Text in the row receives
        /// the type name; the second Text receives "Unlocked" or the lock reason.</para>
        ///
        /// <para>Safe to call at any time — fully null-safe; skips row generation
        /// when <c>_listContainer</c>, <c>_rowPrefab</c>, or <c>_unlockConfig</c>
        /// are null.</para>
        /// </summary>
        public void Refresh()
        {
            if (_listContainer == null || _rowPrefab == null) return;

            // Destroy stale rows.
            for (int i = _listContainer.childCount - 1; i >= 0; i--)
                Destroy(_listContainer.GetChild(i).gameObject);

            if (_unlockConfig == null) return;

            int prestigeCount = _prestigeSystem?.PrestigeCount ?? 0;

            for (int i = 0; i < s_allTypes.Length; i++)
            {
                DamageType type       = s_allTypes[i];
                bool       unlocked   = _unlockConfig.IsUnlocked(type, prestigeCount);
                string     statusText = unlocked
                    ? "Unlocked"
                    : _unlockConfig.GetLockReason(type, prestigeCount);

                GameObject row   = Instantiate(_rowPrefab, _listContainer);
                Text[]     texts = row.GetComponentsInChildren<Text>(true);

                if (texts.Length > 0) texts[0].text = type.ToString();
                if (texts.Length > 1) texts[1].text = statusText;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The currently assigned <see cref="WeaponTypeUnlockConfig"/>. May be null.</summary>
        public WeaponTypeUnlockConfig UnlockConfig => _unlockConfig;

        /// <summary>The currently assigned <see cref="PrestigeSystemSO"/>. May be null.</summary>
        public PrestigeSystemSO PrestigeSystem => _prestigeSystem;
    }
}
