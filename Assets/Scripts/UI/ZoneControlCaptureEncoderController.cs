using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureEncoderController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureEncoderSO _encoderSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onEncoderEncoded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _symbolLabel;
        [SerializeField] private Text       _encodeLabel;
        [SerializeField] private Slider     _symbolBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleEncodedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleEncodedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onEncoderEncoded?.RegisterCallback(_handleEncodedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onEncoderEncoded?.UnregisterCallback(_handleEncodedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_encoderSO == null) return;
            int bonus = _encoderSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_encoderSO == null) return;
            _encoderSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _encoderSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_encoderSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_symbolLabel != null)
                _symbolLabel.text = $"Symbols: {_encoderSO.Symbols}/{_encoderSO.SymbolsNeeded}";

            if (_encodeLabel != null)
                _encodeLabel.text = $"Encodes: {_encoderSO.EncodeCount}";

            if (_symbolBar != null)
                _symbolBar.value = _encoderSO.SymbolProgress;
        }

        public ZoneControlCaptureEncoderSO EncoderSO => _encoderSO;
    }
}
