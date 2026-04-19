using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureWindfallController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureWindfallSO _windfallSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onWindfall;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _streakLabel;
        [SerializeField] private Text       _windfallLabel;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleWindfallDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate = HandlePlayerCaptured;
            _handleBotCapturedDelegate    = HandleBotCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handleWindfallDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onWindfall?.RegisterCallback(_handleWindfallDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onWindfall?.UnregisterCallback(_handleWindfallDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_windfallSO == null) return;
            int prevWindfalls = _windfallSO.WindfallCount;
            _windfallSO.RecordPlayerCapture();
            if (_windfallSO.WindfallCount > prevWindfalls)
                _wallet?.AddFunds(_windfallSO.BonusPerWindfall);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_windfallSO == null) return;
            _windfallSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _windfallSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_windfallSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_streakLabel != null)
                _streakLabel.text = $"Streak: {_windfallSO.CurrentStreak}/{_windfallSO.RequiredStreak}";

            if (_windfallLabel != null)
                _windfallLabel.text = $"Windfalls: {_windfallSO.WindfallCount}";
        }

        public ZoneControlCaptureWindfallSO WindfallSO => _windfallSO;
    }
}
