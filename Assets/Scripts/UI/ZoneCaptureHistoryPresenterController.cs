using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that renders the zone capture history as a compact
    /// mini-timeline strip — one coloured <see cref="Image"/> per entry — rather
    /// than a scrollable row list.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _stripContainer  → parent Transform; children are destroyed and rebuilt
    ///                       on every Refresh.
    ///   _entryPrefab     → prefab that contains at least one Image component;
    ///                       the Image's color is set to <see cref="_captureColor"/>
    ///                       for capture events and <see cref="_lostColor"/> for
    ///                       loss events.
    ///   _panel           → root panel; hidden when history is null or empty.
    ///   _emptyLabel      → activated when history has zero entries.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes to _onHistoryUpdated for reactive rebuild.
    ///   - All UI refs optional; null-safe throughout.
    ///   - Delegate cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one presenter per strip panel.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _historySO        → ZoneCaptureHistorySO asset.
    ///   2. Assign _onHistoryUpdated → ZoneCaptureHistorySO._onHistoryUpdated channel.
    ///   3. Assign _stripContainer, _entryPrefab, optional _emptyLabel and _panel.
    ///   4. Optionally customise _captureColor (default green) and
    ///      _lostColor (default red).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneCaptureHistoryPresenterController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneCaptureHistorySO _historySO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to ZoneCaptureHistorySO._onHistoryUpdated to rebuild on change.")]
        [SerializeField] private VoidGameEvent _onHistoryUpdated;

        [Header("Strip Settings")]
        [Tooltip("Tint applied to each entry image for a zone capture event.")]
        [SerializeField] private Color _captureColor = Color.green;

        [Tooltip("Tint applied to each entry image for a zone lost event.")]
        [SerializeField] private Color _lostColor = Color.red;

        [Header("UI Refs (optional)")]
        [SerializeField] private Transform  _stripContainer;
        [SerializeField] private GameObject _entryPrefab;
        [SerializeField] private GameObject _emptyLabel;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onHistoryUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onHistoryUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the timeline strip from the current capture history.
        /// Destroys all existing child objects in <c>_stripContainer</c> and
        /// instantiates one coloured entry per recorded event (oldest-first).
        /// Hides the panel when history is null or the container/prefab is unset.
        /// </summary>
        public void Refresh()
        {
            if (_historySO == null || _stripContainer == null || _entryPrefab == null)
            {
                _panel?.SetActive(false);
                return;
            }

            // Destroy stale children.
            for (int i = _stripContainer.childCount - 1; i >= 0; i--)
                Object.Destroy(_stripContainer.GetChild(i).gameObject);

            bool isEmpty = _historySO.Count == 0;
            _emptyLabel?.SetActive(isEmpty);

            if (isEmpty)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            // Build oldest-to-newest (index 0 = oldest in ring).
            for (int i = _historySO.Count - 1; i >= 0; i--)
            {
                ZoneCaptureHistoryEntry entry = _historySO.GetEntry(i);
                GameObject go = Object.Instantiate(_entryPrefab, _stripContainer);
                Image img = go.GetComponentInChildren<Image>();
                if (img != null)
                    img.color = entry.isCapture ? _captureColor : _lostColor;
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound history SO (may be null).</summary>
        public ZoneCaptureHistorySO HistorySO => _historySO;

        /// <summary>Capture-event tint colour.</summary>
        public Color CaptureColor => _captureColor;

        /// <summary>Lost-event tint colour.</summary>
        public Color LostColor => _lostColor;
    }
}
