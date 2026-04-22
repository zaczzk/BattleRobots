using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSynthesizerController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSynthesizerSO _synthesizerSO;
        [SerializeField] private PlayerWallet                     _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSynthesizerPlayed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _voiceLabel;
        [SerializeField] private Text       _synthLabel;
        [SerializeField] private Slider     _voiceBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleSynthDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleSynthDelegate        = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSynthesizerPlayed?.RegisterCallback(_handleSynthDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSynthesizerPlayed?.UnregisterCallback(_handleSynthDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_synthesizerSO == null) return;
            int bonus = _synthesizerSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_synthesizerSO == null) return;
            _synthesizerSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _synthesizerSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_synthesizerSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_voiceLabel != null)
                _voiceLabel.text = $"Voices: {_synthesizerSO.Voices}/{_synthesizerSO.VoicesNeeded}";

            if (_synthLabel != null)
                _synthLabel.text = $"Syntheses: {_synthesizerSO.SynthCount}";

            if (_voiceBar != null)
                _voiceBar.value = _synthesizerSO.VoiceProgress;
        }

        public ZoneControlCaptureSynthesizerSO SynthesizerSO => _synthesizerSO;
    }
}
