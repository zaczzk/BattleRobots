using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePullbackController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePullbackSO _pullbackSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPullbackPulled;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _morphismLabel;
        [SerializeField] private Text       _pullbackLabel;
        [SerializeField] private Slider     _morphismBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handlePulledDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handlePulledDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPullbackPulled?.RegisterCallback(_handlePulledDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPullbackPulled?.UnregisterCallback(_handlePulledDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_pullbackSO == null) return;
            int bonus = _pullbackSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_pullbackSO == null) return;
            _pullbackSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _pullbackSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_pullbackSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_morphismLabel != null)
                _morphismLabel.text = $"Morphisms: {_pullbackSO.Morphisms}/{_pullbackSO.MorphismsNeeded}";

            if (_pullbackLabel != null)
                _pullbackLabel.text = $"Pullbacks: {_pullbackSO.PullbackCount}";

            if (_morphismBar != null)
                _morphismBar.value = _pullbackSO.MorphismProgress;
        }

        public ZoneControlCapturePullbackSO PullbackSO => _pullbackSO;
    }
}
