using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureExcisionController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureExcisionSO _excisionSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onExcisionComplete;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _subsetLabel;
        [SerializeField] private Text       _exciseLabel;
        [SerializeField] private Slider     _subsetBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleExcisionDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleExcisionDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onExcisionComplete?.RegisterCallback(_handleExcisionDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onExcisionComplete?.UnregisterCallback(_handleExcisionDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_excisionSO == null) return;
            int bonus = _excisionSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_excisionSO == null) return;
            _excisionSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _excisionSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_excisionSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_subsetLabel != null)
                _subsetLabel.text = $"Subsets: {_excisionSO.Subsets}/{_excisionSO.SubsetsNeeded}";

            if (_exciseLabel != null)
                _exciseLabel.text = $"Excisions: {_excisionSO.ExcisionCount}";

            if (_subsetBar != null)
                _subsetBar.value = _excisionSO.SubsetProgress;
        }

        public ZoneControlCaptureExcisionSO ExcisionSO => _excisionSO;
    }
}
