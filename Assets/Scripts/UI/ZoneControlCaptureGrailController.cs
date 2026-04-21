using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureGrailController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureGrailSO _grailSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onGrailFilled;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _dropLabel;
        [SerializeField] private Text       _fillLabel;
        [SerializeField] private Slider     _dropBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFilledDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleFilledDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onGrailFilled?.RegisterCallback(_handleFilledDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onGrailFilled?.UnregisterCallback(_handleFilledDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_grailSO == null) return;
            int bonus = _grailSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_grailSO == null) return;
            _grailSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _grailSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_grailSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_dropLabel != null)
                _dropLabel.text = $"Drops: {_grailSO.Drops}/{_grailSO.DropsNeeded}";

            if (_fillLabel != null)
                _fillLabel.text = $"Fills: {_grailSO.FillCount}";

            if (_dropBar != null)
                _dropBar.value = _grailSO.DropProgress;
        }

        public ZoneControlCaptureGrailSO GrailSO => _grailSO;
    }
}
