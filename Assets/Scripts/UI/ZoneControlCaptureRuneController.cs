using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureRuneController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureRuneSO _runeSO;
        [SerializeField] private PlayerWalletSO            _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onRuneScribed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _runeLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private Slider     _runeBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRuneScribedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleRuneScribedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onRuneScribed?.RegisterCallback(_handleRuneScribedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onRuneScribed?.UnregisterCallback(_handleRuneScribedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_runeSO == null) return;
            int bonus = _runeSO.RecordPlayerCapture();
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_runeSO == null) return;
            _runeSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _runeSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_runeSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_runeLabel != null)
                _runeLabel.text = $"Runes: {_runeSO.CurrentRunes}/{_runeSO.MaxRunes}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Rune Bonus: {_runeSO.RuneValue * _runeSO.CurrentRunes}";

            if (_runeBar != null)
                _runeBar.value = _runeSO.RuneProgress;
        }

        public ZoneControlCaptureRuneSO RuneSO => _runeSO;
    }
}
