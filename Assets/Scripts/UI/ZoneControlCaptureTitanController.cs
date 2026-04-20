using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureTitanController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureTitanSO _titanSO;
        [SerializeField] private PlayerWalletSO             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onTitanRisen;
        [SerializeField] private VoidGameEvent _onTitanComplete;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _titanLabel;
        [SerializeField] private Text       _completionCountLabel;
        [SerializeField] private Slider     _buildBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleTitanRisenDelegate;
        private Action _handleTitanCompleteDelegate;

        private void Awake()
        {
            _handlePlayerDelegate         = HandlePlayerCaptured;
            _handleBotDelegate            = HandleBotCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handleTitanRisenDelegate     = Refresh;
            _handleTitanCompleteDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onTitanRisen?.RegisterCallback(_handleTitanRisenDelegate);
            _onTitanComplete?.RegisterCallback(_handleTitanCompleteDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onTitanRisen?.UnregisterCallback(_handleTitanRisenDelegate);
            _onTitanComplete?.UnregisterCallback(_handleTitanCompleteDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_titanSO == null) return;
            int prevCompletions = _titanSO.CompletionCount;
            int prevTitans      = _titanSO.TitanCount;
            int bonus           = _titanSO.RecordPlayerCapture();
            if (_titanSO.CompletionCount > prevCompletions)
                _wallet?.AddFunds(_titanSO.CompletionBonus);
            else if (_titanSO.TitanCount > prevTitans && bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_titanSO == null) return;
            _titanSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _titanSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_titanSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_titanLabel != null)
                _titanLabel.text = $"Titans: {_titanSO.TitanCount}/{_titanSO.MaxTitans}";

            if (_completionCountLabel != null)
                _completionCountLabel.text = $"Completions: {_titanSO.CompletionCount}";

            if (_buildBar != null)
                _buildBar.value = _titanSO.BuildProgress;
        }

        public ZoneControlCaptureTitanSO TitanSO => _titanSO;
    }
}
