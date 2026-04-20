using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureScrollController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureScrollSO _scrollSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCodexComplete;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _scrollLabel;
        [SerializeField] private Text       _codexLabel;
        [SerializeField] private Slider     _scrollBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCodexCompleteDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCodexCompleteDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCodexComplete?.RegisterCallback(_handleCodexCompleteDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCodexComplete?.UnregisterCallback(_handleCodexCompleteDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_scrollSO == null) return;
            int bonus = _scrollSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_scrollSO == null) return;
            _scrollSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _scrollSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_scrollSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_scrollLabel != null)
                _scrollLabel.text = $"Scrolls: {_scrollSO.Scrolls}/{_scrollSO.ScrollsNeeded}";

            if (_codexLabel != null)
                _codexLabel.text = $"Codexes: {_scrollSO.CodexCount}";

            if (_scrollBar != null)
                _scrollBar.value = _scrollSO.ScrollProgress;
        }

        public ZoneControlCaptureScrollSO ScrollSO => _scrollSO;
    }
}
