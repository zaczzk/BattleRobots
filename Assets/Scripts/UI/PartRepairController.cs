using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Post-match / workshop UI controller that lists all damaged robot parts and lets
    /// the player spend credits to repair them via <see cref="PartRepairManager"/>.
    ///
    /// ── Flow ──────────────────────────────────────────────────────────────────
    ///   1. OnEnable subscribes to <c>_onRepairApplied</c> and calls Refresh().
    ///   2. Refresh() reads <see cref="PartConditionRegistry.GetDamagedParts"/> and
    ///      instantiates one row per damaged entry.  When all parts are healthy the
    ///      <c>_noRepairsLabel</c> is shown and the list is empty.
    ///   3. Each row's Repair button calls <see cref="RepairPart"/>;
    ///      the Repair All button calls <see cref="RepairAll"/>.
    ///   4. On success <see cref="PartRepairManager"/> raises <c>_onRepairApplied</c>,
    ///      which triggers another Refresh — keeping the list and wallet display in sync
    ///      automatically.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • All inspector fields are optional; null dependencies are handled gracefully.
    ///   • No Update / FixedUpdate.
    ///   • Refresh delegate cached in Awake; zero heap alloc after Awake.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Add to the Workshop / inter-match Canvas (DisallowMultipleComponent).
    ///   2. Assign _repairManager → PartRepairManager MB in the same scene.
    ///   3. Assign _registry     → PartConditionRegistry SO asset.
    ///   4. Assign _wallet       → PlayerWallet SO asset.
    ///   5. Assign _onRepairApplied → same VoidGameEvent as PartRepairManager._onRepairApplied.
    ///   6. Optionally assign _walletText, _noRepairsLabel, _repairAllButton,
    ///      _listContainer, and _rowPrefab.
    ///
    /// ── Row prefab layout ─────────────────────────────────────────────────────
    ///   Texts[0]   — part name (partId).
    ///   Texts[1]   — HP display,    e.g. "35 / 50 HP".
    ///   Texts[2]   — cost display,  e.g. "Cost: 30".
    ///   Buttons[0] — Repair button for this specific part.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PartRepairController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Repair backend that deducts credits and heals part HP.")]
        [SerializeField] private PartRepairManager _repairManager;

        [Tooltip("Registry supplying the list of damaged parts for display.")]
        [SerializeField] private PartConditionRegistry _registry;

        [Tooltip("Runtime wallet SO. Balance is displayed and updated after each repair.")]
        [SerializeField] private PlayerWallet _wallet;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In")]
        [Tooltip("Same VoidGameEvent as PartRepairManager._onRepairApplied. " +
                 "Triggers a Refresh() so the list and wallet balance stay in sync.")]
        [SerializeField] private VoidGameEvent _onRepairApplied;

        // ── Inspector — UI Refs (all optional) ────────────────────────────────

        [Header("UI Refs (optional)")]
        [Tooltip("Displays the current wallet balance, e.g. 'Credits: 500'.")]
        [SerializeField] private Text _walletText;

        [Tooltip("Shown when all parts are fully healthy; hidden when any are damaged.")]
        [SerializeField] private Text _noRepairsLabel;

        [Tooltip("Calls RepairAll() when clicked. May also be wired via Inspector.")]
        [SerializeField] private Button _repairAllButton;

        [Tooltip("Parent Transform for the instantiated damaged-part rows.")]
        [SerializeField] private Transform _listContainer;

        [Tooltip("Prefab for one damaged-part row. Expected child layout:\n" +
                 "  Texts[0]   — part name (partId)\n" +
                 "  Texts[1]   — HP display  (e.g. '35 / 50 HP')\n" +
                 "  Texts[2]   — cost display (e.g. 'Cost: 30')\n" +
                 "  Buttons[0] — Repair button for this part")]
        [SerializeField] private GameObject _rowPrefab;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;

            if (_repairAllButton != null)
                _repairAllButton.onClick.AddListener(RepairAll);
        }

        private void OnEnable()
        {
            _onRepairApplied?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onRepairApplied?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to repair all damaged parts the wallet can currently afford.
        /// Delegates to <see cref="PartRepairManager.RepairAll"/>; the subsequent
        /// <c>_onRepairApplied</c> event will trigger a Refresh.
        /// No-op when <c>_repairManager</c> is null.
        /// </summary>
        public void RepairAll()
        {
            if (_repairManager == null) return;
            _repairManager.RepairAll();
        }

        /// <summary>
        /// Attempts to repair the part identified by <paramref name="partId"/>.
        /// Delegates to <see cref="PartRepairManager.RepairPart"/>; the subsequent
        /// <c>_onRepairApplied</c> event will trigger a Refresh.
        /// No-op when <c>_repairManager</c> is null.
        /// </summary>
        public void RepairPart(string partId)
        {
            if (_repairManager == null) return;
            _repairManager.RepairPart(partId);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void Refresh()
        {
            // Update wallet balance display.
            if (_walletText != null)
                _walletText.text = _wallet != null
                    ? $"Credits: {_wallet.Balance}"
                    : "Credits: \u2014";

            // Gather damaged parts (returns Array.Empty when registry is null or all healthy).
            IReadOnlyList<PartConditionRegistry.PartConditionEntry> damaged =
                _registry != null
                    ? _registry.GetDamagedParts()
                    : Array.Empty<PartConditionRegistry.PartConditionEntry>();

            bool anyDamaged = damaged.Count > 0;

            // Show "no repairs needed" label when all parts are healthy.
            if (_noRepairsLabel != null)
                _noRepairsLabel.gameObject.SetActive(!anyDamaged);

            // Without container or prefab there is nothing to build.
            if (_listContainer == null || _rowPrefab == null) return;

            // Destroy existing rows before rebuilding.
            for (int i = _listContainer.childCount - 1; i >= 0; i--)
                Destroy(_listContainer.GetChild(i).gameObject);

            // Build one row per damaged part.
            for (int i = 0; i < damaged.Count; i++)
            {
                PartConditionRegistry.PartConditionEntry entry = damaged[i];
                string capturedPartId = entry.partId; // captured for lambda closure

                GameObject row = Instantiate(_rowPrefab, _listContainer);

                Text[] texts = row.GetComponentsInChildren<Text>();
                if (texts.Length >= 1)
                    texts[0].text = entry.partId;
                if (texts.Length >= 2 && entry.condition != null)
                    texts[1].text = FormatHP(entry.condition.CurrentHP, entry.condition.MaxHP);
                if (texts.Length >= 3)
                    texts[2].text = FormatCost(
                        _repairManager != null
                            ? _repairManager.GetRepairCost(capturedPartId)
                            : 0);

                Button[] buttons = row.GetComponentsInChildren<Button>();
                if (buttons.Length >= 1)
                    buttons[0].onClick.AddListener(() => RepairPart(capturedPartId));
            }
        }

        /// <summary>
        /// Formats an HP pair as "{current} / {max} HP", rounding both values to the
        /// nearest integer. Internal so tests can reach it via reflection.
        /// </summary>
        internal static string FormatHP(float currentHP, float maxHP)
            => $"{Mathf.RoundToInt(currentHP)} / {Mathf.RoundToInt(maxHP)} HP";

        /// <summary>
        /// Formats a repair cost as "Cost: {N}".
        /// Internal so tests can reach it via reflection.
        /// </summary>
        internal static string FormatCost(int cost)
            => $"Cost: {cost}";
    }
}
