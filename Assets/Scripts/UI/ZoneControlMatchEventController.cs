using System;
using System.Collections.Generic;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives the zone-control match-event timeline HUD.
    ///
    /// Subscribes to three capture/hazard/combo event channels and records each
    /// occurrence in <see cref="ZoneControlMatchEventSO"/>.  On refresh the list
    /// is rebuilt newest-first inside <c>_listContainer</c>: each row's first
    /// Text shows the timestamp ("{F1}s") and the second Text shows the
    /// description string.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields are optional and null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one timeline panel per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _eventSO         → ZoneControlMatchEventSO asset.
    ///   2. Assign _onZoneCaptured  → VoidGameEvent raised when a zone is captured.
    ///   3. Assign _onHazardActivated → VoidGameEvent raised when a hazard activates.
    ///   4. Assign _onComboReached  → VoidGameEvent raised when a combo milestone fires.
    ///   5. Assign _onEventAdded    → _eventSO._onEventAdded channel for live refresh.
    ///   6. Assign _onMatchStarted  → VoidGameEvent at match start (resets buffer).
    ///   7. Assign _listContainer / _rowPrefab / _emptyLabel / _panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchEventController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchEventSO _eventSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised when the player captures a zone; adds a ZoneCaptured event.")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;

        [Tooltip("Raised when a hazard activates; adds a HazardActivated event.")]
        [SerializeField] private VoidGameEvent _onHazardActivated;

        [Tooltip("Raised when a combo milestone fires; adds a ComboReached event.")]
        [SerializeField] private VoidGameEvent _onComboReached;

        [Tooltip("Wire to ZoneControlMatchEventSO._onEventAdded for reactive refresh.")]
        [SerializeField] private VoidGameEvent _onEventAdded;

        [Tooltip("Raised at match start; resets the event buffer and refreshes the list.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Header("UI Refs (optional)")]
        [SerializeField] private Transform  _listContainer;
        [SerializeField] private GameObject _rowPrefab;
        [SerializeField] private Text       _emptyLabel;

        [Header("UI Refs — Panel (optional)")]
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleZoneCapturedDelegate;
        private Action _handleHazardActivatedDelegate;
        private Action _handleComboReachedDelegate;
        private Action _refreshDelegate;
        private Action _handleMatchStartedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleZoneCapturedDelegate    = HandleZoneCaptured;
            _handleHazardActivatedDelegate = HandleHazardActivated;
            _handleComboReachedDelegate    = HandleComboReached;
            _refreshDelegate               = Refresh;
            _handleMatchStartedDelegate    = HandleMatchStarted;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onHazardActivated?.RegisterCallback(_handleHazardActivatedDelegate);
            _onComboReached?.RegisterCallback(_handleComboReachedDelegate);
            _onEventAdded?.RegisterCallback(_refreshDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onHazardActivated?.UnregisterCallback(_handleHazardActivatedDelegate);
            _onComboReached?.UnregisterCallback(_handleComboReachedDelegate);
            _onEventAdded?.UnregisterCallback(_refreshDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>Records a ZoneCaptured event in the timeline SO.</summary>
        public void HandleZoneCaptured()
            => _eventSO?.AddEvent(Time.time, ZoneControlMatchEventType.ZoneCaptured, "Zone Captured");

        /// <summary>Records a HazardActivated event in the timeline SO.</summary>
        public void HandleHazardActivated()
            => _eventSO?.AddEvent(Time.time, ZoneControlMatchEventType.HazardActivated, "Hazard Activated");

        /// <summary>Records a ComboReached event in the timeline SO.</summary>
        public void HandleComboReached()
            => _eventSO?.AddEvent(Time.time, ZoneControlMatchEventType.ComboReached, "Combo Reached");

        /// <summary>Resets the event buffer and refreshes the HUD list.</summary>
        public void HandleMatchStarted()
        {
            _eventSO?.Reset();
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the event list from the current SO state.
        /// Events are displayed newest-first.
        /// Hides the panel when <c>_eventSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_eventSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            // Destroy stale rows.
            if (_listContainer != null)
            {
                for (int i = _listContainer.childCount - 1; i >= 0; i--)
                    Destroy(_listContainer.GetChild(i).gameObject);
            }

            bool isEmpty = _eventSO.EventCount == 0;
            _emptyLabel?.gameObject.SetActive(isEmpty);

            if (isEmpty || _listContainer == null || _rowPrefab == null) return;

            IReadOnlyList<ZoneControlMatchEvent> events = _eventSO.Events;
            for (int i = events.Count - 1; i >= 0; i--)
            {
                ZoneControlMatchEvent evt = events[i];
                GameObject row   = Instantiate(_rowPrefab, _listContainer);
                Text[]     texts = row.GetComponentsInChildren<Text>(true);
                if (texts.Length > 0) texts[0].text = $"{evt.Timestamp:F1}s";
                if (texts.Length > 1) texts[1].text = evt.Description;
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound event timeline SO (may be null).</summary>
        public ZoneControlMatchEventSO EventSO => _eventSO;
    }
}
