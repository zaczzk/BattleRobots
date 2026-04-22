using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureArrayController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureArraySO _arraySO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onArrayFilled;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _elementLabel;
        [SerializeField] private Text       _fillLabel;
        [SerializeField] private Slider     _elementBar;
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
            _onArrayFilled?.RegisterCallback(_handleFilledDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onArrayFilled?.UnregisterCallback(_handleFilledDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_arraySO == null) return;
            int bonus = _arraySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_arraySO == null) return;
            _arraySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _arraySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_arraySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_elementLabel != null)
                _elementLabel.text = $"Elements: {_arraySO.Elements}/{_arraySO.ElementsNeeded}";

            if (_fillLabel != null)
                _fillLabel.text = $"Fills: {_arraySO.FillCount}";

            if (_elementBar != null)
                _elementBar.value = _arraySO.ElementProgress;
        }

        public ZoneControlCaptureArraySO ArraySO => _arraySO;
    }
}
