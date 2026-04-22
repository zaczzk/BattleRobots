using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureAmplifierController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureAmplifierSO _amplifierSO;
        [SerializeField] private PlayerWallet                   _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onAmplifierBoosted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _stageLabel;
        [SerializeField] private Text       _amplifyLabel;
        [SerializeField] private Slider     _stageBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleBoostedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleBoostedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onAmplifierBoosted?.RegisterCallback(_handleBoostedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onAmplifierBoosted?.UnregisterCallback(_handleBoostedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_amplifierSO == null) return;
            int bonus = _amplifierSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_amplifierSO == null) return;
            _amplifierSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _amplifierSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_amplifierSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_stageLabel != null)
                _stageLabel.text = $"Stages: {_amplifierSO.Stages}/{_amplifierSO.StagesNeeded}";

            if (_amplifyLabel != null)
                _amplifyLabel.text = $"Amplifications: {_amplifierSO.AmplificationCount}";

            if (_stageBar != null)
                _stageBar.value = _amplifierSO.StageProgress;
        }

        public ZoneControlCaptureAmplifierSO AmplifierSO => _amplifierSO;
    }
}
