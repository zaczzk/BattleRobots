using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCrestController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCrestSO _crestSO;
        [SerializeField] private PlayerWalletSO             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCrest;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _waveLabel;
        [SerializeField] private Text       _crestLabel;
        [SerializeField] private Slider     _waveBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCrestDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCrestDelegate        = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCrest?.RegisterCallback(_handleCrestDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCrest?.UnregisterCallback(_handleCrestDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_crestSO == null) return;
            int prev = _crestSO.CrestCount;
            _crestSO.RecordPlayerCapture();
            if (_crestSO.CrestCount > prev)
                _wallet?.AddFunds(_crestSO.BonusPerCrest);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_crestSO == null) return;
            _crestSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _crestSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_crestSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_waveLabel != null)
                _waveLabel.text = $"Wave: {_crestSO.CurrentWaveHeight:F1}/{_crestSO.WaveHeightForCrest:F0}";

            if (_crestLabel != null)
                _crestLabel.text = $"Crests: {_crestSO.CrestCount}";

            if (_waveBar != null)
                _waveBar.value = _crestSO.WaveProgress;
        }

        public ZoneControlCaptureCrestSO CrestSO => _crestSO;
    }
}
