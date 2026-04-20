using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureGridController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureGridSO _gridSO;
        [SerializeField] private PlayerWalletSO           _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onRowComplete;
        [SerializeField] private VoidGameEvent _onGridComplete;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _gridLabel;
        [SerializeField] private Text       _rowsLabel;
        [SerializeField] private Slider     _gridBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRowCompleteDelegate;
        private Action _handleGridCompleteDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleRowCompleteDelegate  = Refresh;
            _handleGridCompleteDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onRowComplete?.RegisterCallback(_handleRowCompleteDelegate);
            _onGridComplete?.RegisterCallback(_handleGridCompleteDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onRowComplete?.UnregisterCallback(_handleRowCompleteDelegate);
            _onGridComplete?.UnregisterCallback(_handleGridCompleteDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_gridSO == null) return;
            int bonus = _gridSO.RecordPlayerCapture();
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_gridSO == null) return;
            _gridSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _gridSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_gridSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_gridLabel != null)
                _gridLabel.text = $"Grid: {_gridSO.FilledSlots}/{_gridSO.TotalSlots}";

            if (_rowsLabel != null)
                _rowsLabel.text = $"Rows: {_gridSO.RowsCompleted}";

            if (_gridBar != null)
                _gridBar.value = _gridSO.GridProgress;
        }

        public ZoneControlCaptureGridSO GridSO => _gridSO;
    }
}
