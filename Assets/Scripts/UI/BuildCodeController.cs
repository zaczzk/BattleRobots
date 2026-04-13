using System;
using System.Collections.Generic;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI controller for exporting and importing loadout build codes.
    ///
    /// Export: encodes the currently equipped part IDs via <see cref="BuildCodeEncoder"/>
    ///         into a shareable Base64 string displayed in <c>_exportCodeText</c>.
    /// Import: decodes a code pasted into <c>_importCodeField</c>, validates part IDs
    ///         against the optional catalog, and applies the loadout via
    ///         <see cref="PlayerLoadout.SetLoadout"/>. The result is persisted via
    ///         a load→mutate→Save round-trip.
    ///
    /// ── Inspector wiring ─────────────────────────────────────────────────────────
    ///   Data (all optional):
    ///     _playerLoadout  → the <see cref="PlayerLoadout"/> SO asset.
    ///     _shopCatalog    → the <see cref="ShopCatalog"/> SO (enables per-ID validation on import).
    ///   Event Channels — In (optional):
    ///     _onLoadoutChanged → refreshes the export code when the loadout changes.
    ///   UI Refs (all optional):
    ///     _exportCodeText   → Text that displays the current build code.
    ///     _importCodeField  → InputField where the player pastes a build code.
    ///     _exportButton     → Button that writes the current code to _exportCodeText.
    ///     _importButton     → Button that applies the code in _importCodeField.
    ///     _statusText       → Text that shows "Build applied!" / "Invalid code." etc.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no BattleRobots.Physics references.
    ///   - No allocations in Update/FixedUpdate — no Update loop.
    ///   - All inspector fields optional; null-safe throughout.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildCodeController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (all optional)")]
        [Tooltip("SO holding the currently equipped part IDs. " +
                 "Leave null to disable both export and import.")]
        [SerializeField] private PlayerLoadout _playerLoadout;

        [Tooltip("Catalog of all valid parts. When assigned, import validates that every decoded " +
                 "part ID exists in the catalog before applying. Leave null to skip validation.")]
        [SerializeField] private ShopCatalog _shopCatalog;

        [Header("Event Channel — In (optional)")]
        [Tooltip("Raised by PlayerLoadout when the loadout changes. " +
                 "This controller subscribes to auto-refresh the export code. " +
                 "Leave null to refresh only on OnEnable.")]
        [SerializeField] private VoidGameEvent _onLoadoutChanged;

        [Header("UI Refs (all optional)")]
        [Tooltip("Text that displays the current build code after Export. Leave null to skip.")]
        [SerializeField] private Text _exportCodeText;

        [Tooltip("InputField where the player pastes a build code to import. Leave null to skip.")]
        [SerializeField] private InputField _importCodeField;

        [Tooltip("Button that triggers ExportCode(). Leave null if you call it manually.")]
        [SerializeField] private Button _exportButton;

        [Tooltip("Button that triggers ImportCode(). Leave null if you call it manually.")]
        [SerializeField] private Button _importButton;

        [Tooltip("Text that shows status messages ('Build applied!', 'Invalid code.', etc.).")]
        [SerializeField] private Text _statusText;

        // ── Runtime ───────────────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;

            if (_exportButton != null) _exportButton.onClick.AddListener(ExportCode);
            if (_importButton != null) _importButton.onClick.AddListener(ImportCode);
        }

        private void OnEnable()
        {
            _onLoadoutChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onLoadoutChanged?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Encodes the current loadout and writes the result to <c>_exportCodeText</c>.
        /// Silent no-op when <c>_playerLoadout</c> is null.
        /// </summary>
        public void ExportCode()
        {
            if (_playerLoadout == null)
            {
                SetStatus("No loadout assigned.");
                return;
            }

            string code = BuildCodeEncoder.Encode(_playerLoadout.EquippedPartIds);

            if (_exportCodeText != null)
                _exportCodeText.text = code;

            SetStatus(code.Length > 0 ? "Code ready." : "Loadout is empty — nothing to export.");
        }

        /// <summary>
        /// Decodes the code in <c>_importCodeField</c>, optionally validates against the catalog,
        /// applies the loadout, and persists the change via load→mutate→Save.
        /// Sets <c>_statusText</c> to an appropriate message.
        /// Silent no-op when <c>_importCodeField</c> is null.
        /// </summary>
        public void ImportCode()
        {
            if (_importCodeField == null) return;

            string code = _importCodeField.text;
            List<string> decoded = BuildCodeEncoder.Decode(code);

            if (decoded == null)
            {
                SetStatus("Invalid code.");
                return;
            }

            // Optional catalog validation: all IDs must exist in the catalog.
            if (_shopCatalog != null)
            {
                var parts = _shopCatalog.Parts;
                for (int i = 0; i < decoded.Count; i++)
                {
                    bool found = false;
                    for (int j = 0; j < parts.Count; j++)
                    {
                        if (parts[j] != null && parts[j].PartId == decoded[i])
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        SetStatus($"Unknown part: {decoded[i]}");
                        return;
                    }
                }
            }

            if (_playerLoadout == null)
            {
                SetStatus("No loadout assigned.");
                return;
            }

            _playerLoadout.SetLoadout(decoded);

            // Persist loadout via load→mutate→Save.
            SaveData save = SaveSystem.Load();
            save.loadoutPartIds.Clear();
            IReadOnlyList<string> equipped = _playerLoadout.EquippedPartIds;
            for (int i = 0; i < equipped.Count; i++)
                save.loadoutPartIds.Add(equipped[i]);
            SaveSystem.Save(save);

            SetStatus("Build applied!");
        }

        // ── Internal ──────────────────────────────────────────────────────────

        /// <summary>
        /// Refreshes the export code text from the current loadout.
        /// Called on OnEnable and whenever <c>_onLoadoutChanged</c> fires.
        /// </summary>
        private void Refresh()
        {
            if (_playerLoadout == null || _exportCodeText == null) return;
            _exportCodeText.text = BuildCodeEncoder.Encode(_playerLoadout.EquippedPartIds);
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
                _statusText.text = message;
        }
    }
}
