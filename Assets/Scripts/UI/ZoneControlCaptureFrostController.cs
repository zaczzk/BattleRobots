using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureFrostController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureFrostSO _frostSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFrozen;
        [SerializeField] private VoidGameEvent _onThawed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Slider     _frostBar;
        [SerializeField] private GameObject _panel;

        private Action _handleBotDelegate;
        private Action _handlePlayerDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleBotDelegate          = HandleBotCaptured;
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFrozen?.RegisterCallback(_refreshDelegate);
            _onThawed?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFrozen?.UnregisterCallback(_refreshDelegate);
            _onThawed?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleBotCaptured()
        {
            if (_frostSO == null) return;
            _frostSO.RecordBotCapture();
            Refresh();
        }

        private void HandlePlayerCaptured()
        {
            if (_frostSO == null) return;
            _frostSO.RecordPlayerCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _frostSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_frostSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
                _statusLabel.text = _frostSO.IsFrozen ? "FROZEN!" : "Active";

            if (_frostBar != null)
                _frostBar.value = _frostSO.FrostProgress;
        }

        public ZoneControlCaptureFrostSO FrostSO => _frostSO;
    }
}
