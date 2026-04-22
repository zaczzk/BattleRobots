using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureMemoryController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureMemorySO _memorySO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMemoryFlushed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _cellLabel;
        [SerializeField] private Text       _flushLabel;
        [SerializeField] private Slider     _cellBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFlushedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleFlushedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMemoryFlushed?.RegisterCallback(_handleFlushedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMemoryFlushed?.UnregisterCallback(_handleFlushedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_memorySO == null) return;
            int bonus = _memorySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_memorySO == null) return;
            _memorySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _memorySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_memorySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_cellLabel != null)
                _cellLabel.text = $"Cells: {_memorySO.Cells}/{_memorySO.CellsNeeded}";

            if (_flushLabel != null)
                _flushLabel.text = $"Flushes: {_memorySO.FlushCount}";

            if (_cellBar != null)
                _cellBar.value = _memorySO.CellProgress;
        }

        public ZoneControlCaptureMemorySO MemorySO => _memorySO;
    }
}
