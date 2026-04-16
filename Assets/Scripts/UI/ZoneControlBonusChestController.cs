using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives the zone-control bonus-chest HUD.
    ///
    /// Tracks cumulative player zone captures and delegates milestone evaluation to
    /// <see cref="ZoneControlBonusChestSO.CheckChest"/>.  The label displays how
    /// many more captures are needed before the next chest spawns.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _nextChestLabel → "Next chest: N"
    ///   _panel          → hidden when _chestSO is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one chest-HUD panel per scene.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _chestSO         → ZoneControlBonusChestSO asset.
    ///   2. Assign _onPlayerCaptured → VoidGameEvent raised per player zone capture.
    ///   3. Assign _onMatchStarted  → VoidGameEvent raised at match start.
    ///   4. Assign _nextChestLabel / _panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlBonusChestController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlBonusChestSO _chestSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised each time the player captures a zone; increments the internal counter.")]
        [SerializeField] private VoidGameEvent _onPlayerCaptured;

        [Tooltip("Raised at match start; resets the capture counter and SO state.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text _nextChestLabel;

        [Header("UI Refs — Panel (optional)")]
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handlePlayerCapturedDelegate;
        private Action _handleMatchStartedDelegate;

        // ── Runtime state ─────────────────────────────────────────────────────

        private int _captureCount;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handlePlayerCapturedDelegate = HandlePlayerCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
        }

        private void OnEnable()
        {
            _onPlayerCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Increments the cumulative capture counter and delegates milestone
        /// evaluation to <see cref="ZoneControlBonusChestSO.CheckChest"/>.
        /// </summary>
        public void HandlePlayerCaptured()
        {
            _captureCount++;
            _chestSO?.CheckChest(_captureCount);
            Refresh();
        }

        /// <summary>
        /// Resets the capture counter and the chest SO state at match start.
        /// </summary>
        public void HandleMatchStarted()
        {
            _captureCount = 0;
            _chestSO?.Reset();
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the next-chest label.
        /// Hides the panel when <c>_chestSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_chestSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_nextChestLabel != null)
            {
                int interval = _chestSO.CaptureInterval;
                int next     = interval > 0
                    ? interval - (_captureCount % interval)
                    : 0;
                _nextChestLabel.text = $"Next chest: {next}";
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Cumulative player zone captures since the last match start.</summary>
        public int CaptureCount => _captureCount;

        /// <summary>The bound bonus-chest SO (may be null).</summary>
        public ZoneControlBonusChestSO ChestSO => _chestSO;
    }
}
