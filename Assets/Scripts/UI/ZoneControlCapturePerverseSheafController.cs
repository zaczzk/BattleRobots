using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePerverseSheafController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePerverseSheafSO _perverseSheafSO;
        [SerializeField] private PlayerWallet                       _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPerverseSheafPerversified;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _stalkConditionLabel;
        [SerializeField] private Text       _perversificationLabel;
        [SerializeField] private Slider     _stalkConditionBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handlePerversifiedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handlePerversifiedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPerverseSheafPerversified?.RegisterCallback(_handlePerversifiedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPerverseSheafPerversified?.UnregisterCallback(_handlePerversifiedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_perverseSheafSO == null) return;
            int bonus = _perverseSheafSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_perverseSheafSO == null) return;
            _perverseSheafSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _perverseSheafSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_perverseSheafSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_stalkConditionLabel != null)
                _stalkConditionLabel.text = $"Stalk Conditions: {_perverseSheafSO.StalkConditions}/{_perverseSheafSO.StalkConditionsNeeded}";

            if (_perversificationLabel != null)
                _perversificationLabel.text = $"Perversifications: {_perverseSheafSO.PerversificationCount}";

            if (_stalkConditionBar != null)
                _stalkConditionBar.value = _perverseSheafSO.StalkConditionProgress;
        }

        public ZoneControlCapturePerverseSheafSO PerverseSheafSO => _perverseSheafSO;
    }
}
