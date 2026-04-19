using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureComboFinisherController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureComboFinisherSO _comboFinisherSO;
        [SerializeField] private PlayerWalletSO                    _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onComboFinished;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _comboLabel;
        [SerializeField] private Text       _completedLabel;
        [SerializeField] private Slider     _comboProgressBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleComboFinishedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate        = HandlePlayerCaptured;
            _handleBotDelegate           = HandleBotCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleComboFinishedDelegate = HandleComboFinished;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onComboFinished?.RegisterCallback(_handleComboFinishedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onComboFinished?.UnregisterCallback(_handleComboFinishedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_comboFinisherSO == null) return;
            int prev = _comboFinisherSO.CompletedCombos;
            _comboFinisherSO.RecordPlayerCapture();
            if (_comboFinisherSO.CompletedCombos > prev)
                _wallet?.AddFunds(_comboFinisherSO.ComboBonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_comboFinisherSO == null) return;
            _comboFinisherSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _comboFinisherSO?.Reset();
            Refresh();
        }

        private void HandleComboFinished()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (_comboFinisherSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_comboLabel != null)
                _comboLabel.text = $"Combo: {_comboFinisherSO.CurrentCombo}/{_comboFinisherSO.ComboTarget}";

            if (_completedLabel != null)
                _completedLabel.text = $"Combos: {_comboFinisherSO.CompletedCombos}";

            if (_comboProgressBar != null)
                _comboProgressBar.value = _comboFinisherSO.ComboProgress;
        }

        public ZoneControlCaptureComboFinisherSO ComboFinisherSO => _comboFinisherSO;
    }
}
