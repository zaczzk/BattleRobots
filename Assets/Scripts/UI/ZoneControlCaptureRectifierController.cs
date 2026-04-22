using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureRectifierController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureRectifierSO _rectifierSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onRectifierConverted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _waveLabel;
        [SerializeField] private Text       _conversionLabel;
        [SerializeField] private Slider     _waveBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleConvertedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleConvertedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onRectifierConverted?.RegisterCallback(_handleConvertedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onRectifierConverted?.UnregisterCallback(_handleConvertedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_rectifierSO == null) return;
            int bonus = _rectifierSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_rectifierSO == null) return;
            _rectifierSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _rectifierSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_rectifierSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_waveLabel != null)
                _waveLabel.text = $"Waves: {_rectifierSO.Waves}/{_rectifierSO.WavesNeeded}";

            if (_conversionLabel != null)
                _conversionLabel.text = $"Conversions: {_rectifierSO.ConversionCount}";

            if (_waveBar != null)
                _waveBar.value = _rectifierSO.WaveProgress;
        }

        public ZoneControlCaptureRectifierSO RectifierSO => _rectifierSO;
    }
}
