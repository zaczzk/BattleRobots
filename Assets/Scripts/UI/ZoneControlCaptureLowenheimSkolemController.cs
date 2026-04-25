using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureLowenheimSkolemController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureLowenheimSkolemSO _lowenheimSkolemSO;
        [SerializeField] private PlayerWallet                         _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onLowenheimSkolemWitnessed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _downwardWitnessLabel;
        [SerializeField] private Text       _witnessingCountLabel;
        [SerializeField] private Slider     _downwardWitnessBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleWitnessedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleWitnessedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onLowenheimSkolemWitnessed?.RegisterCallback(_handleWitnessedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onLowenheimSkolemWitnessed?.UnregisterCallback(_handleWitnessedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_lowenheimSkolemSO == null) return;
            int bonus = _lowenheimSkolemSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_lowenheimSkolemSO == null) return;
            _lowenheimSkolemSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _lowenheimSkolemSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_lowenheimSkolemSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_downwardWitnessLabel != null)
                _downwardWitnessLabel.text =
                    $"Downward Witnesses: {_lowenheimSkolemSO.DownwardWitnesses}/{_lowenheimSkolemSO.DownwardWitnessesNeeded}";

            if (_witnessingCountLabel != null)
                _witnessingCountLabel.text = $"Witnessings: {_lowenheimSkolemSO.WitnessingCount}";

            if (_downwardWitnessBar != null)
                _downwardWitnessBar.value = _lowenheimSkolemSO.DownwardWitnessProgress;
        }

        public ZoneControlCaptureLowenheimSkolemSO LowenheimSkolemSO => _lowenheimSkolemSO;
    }
}
