using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSpindleController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSpindleSO _spindleSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSpindleWound;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _windLabel;
        [SerializeField] private Text       _boltLabel;
        [SerializeField] private Slider     _windBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleWoundDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleWoundDelegate        = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSpindleWound?.RegisterCallback(_handleWoundDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSpindleWound?.UnregisterCallback(_handleWoundDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_spindleSO == null) return;
            int bonus = _spindleSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_spindleSO == null) return;
            _spindleSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _spindleSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_spindleSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_windLabel != null)
                _windLabel.text = $"Winds: {_spindleSO.Winds}/{_spindleSO.WindsNeeded}";

            if (_boltLabel != null)
                _boltLabel.text = $"Bolts: {_spindleSO.BoltCount}";

            if (_windBar != null)
                _windBar.value = _spindleSO.WindProgress;
        }

        public ZoneControlCaptureSpindleSO SpindleSO => _spindleSO;
    }
}
