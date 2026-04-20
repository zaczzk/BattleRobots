using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureQuakeController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureQuakeSO _quakeSO;
        [SerializeField] private PlayerWalletSO              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onQuake;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _tremorLabel;
        [SerializeField] private Text       _quakeCountLabel;
        [SerializeField] private Slider     _tremorBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleQuakeDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleQuakeDelegate        = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onQuake?.RegisterCallback(_handleQuakeDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onQuake?.UnregisterCallback(_handleQuakeDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_quakeSO == null) return;
            int prev  = _quakeSO.QuakeCount;
            int bonus = _quakeSO.RecordPlayerCapture();
            if (_quakeSO.QuakeCount > prev)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_quakeSO == null) return;
            _quakeSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _quakeSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_quakeSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_tremorLabel != null)
                _tremorLabel.text = $"Tremor: {_quakeSO.TremorCount}/{_quakeSO.CapturesPerQuake}";

            if (_quakeCountLabel != null)
                _quakeCountLabel.text = $"Quakes: {_quakeSO.QuakeCount}";

            if (_tremorBar != null)
                _tremorBar.value = _quakeSO.TremorProgress;
        }

        public ZoneControlCaptureQuakeSO QuakeSO => _quakeSO;
    }
}
