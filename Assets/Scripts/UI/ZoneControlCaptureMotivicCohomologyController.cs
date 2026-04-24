using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureMotivicCohomologyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureMotivicCohomologySO _motivicSO;
        [SerializeField] private PlayerWallet                           _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMotivicCohomologyMotivated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _cycleLabel;
        [SerializeField] private Text       _motivateLabel;
        [SerializeField] private Slider     _cycleBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMotivatedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate      = HandlePlayerCaptured;
            _handleBotDelegate         = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleMotivatedDelegate   = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMotivicCohomologyMotivated?.RegisterCallback(_handleMotivatedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMotivicCohomologyMotivated?.UnregisterCallback(_handleMotivatedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_motivicSO == null) return;
            int bonus = _motivicSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_motivicSO == null) return;
            _motivicSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _motivicSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_motivicSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_cycleLabel != null)
                _cycleLabel.text = $"Cycles: {_motivicSO.Cycles}/{_motivicSO.CyclesNeeded}";

            if (_motivateLabel != null)
                _motivateLabel.text = $"Motivations: {_motivicSO.MotivateCount}";

            if (_cycleBar != null)
                _cycleBar.value = _motivicSO.CycleProgress;
        }

        public ZoneControlCaptureMotivicCohomologySO MotivicSO => _motivicSO;
    }
}
