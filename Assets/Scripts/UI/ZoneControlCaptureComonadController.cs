using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureComonadController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureComonadSO _comonadSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onComonadExtracted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _contextLabel;
        [SerializeField] private Text       _extractLabel;
        [SerializeField] private Slider     _contextBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleComonadExtractedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate           = HandlePlayerCaptured;
            _handleBotDelegate              = HandleBotCaptured;
            _handleMatchStartedDelegate     = HandleMatchStarted;
            _handleComonadExtractedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onComonadExtracted?.RegisterCallback(_handleComonadExtractedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onComonadExtracted?.UnregisterCallback(_handleComonadExtractedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_comonadSO == null) return;
            int bonus = _comonadSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_comonadSO == null) return;
            _comonadSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _comonadSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_comonadSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_contextLabel != null)
                _contextLabel.text = $"Contexts: {_comonadSO.Contexts}/{_comonadSO.ContextsNeeded}";

            if (_extractLabel != null)
                _extractLabel.text = $"Extracts: {_comonadSO.ExtractCount}";

            if (_contextBar != null)
                _contextBar.value = _comonadSO.ContextProgress;
        }

        public ZoneControlCaptureComonadSO ComonadSO => _comonadSO;
    }
}
