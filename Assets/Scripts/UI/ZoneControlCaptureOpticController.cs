using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureOpticController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureOpticSO _opticSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onOpticFocused;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _focusLabel;
        [SerializeField] private Text       _focusCountLabel;
        [SerializeField] private Slider     _focusBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFocusedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleFocusedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onOpticFocused?.RegisterCallback(_handleFocusedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onOpticFocused?.UnregisterCallback(_handleFocusedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_opticSO == null) return;
            int bonus = _opticSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_opticSO == null) return;
            _opticSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _opticSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_opticSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_focusLabel != null)
                _focusLabel.text = $"Focus: {_opticSO.Focus}/{_opticSO.FocusNeeded}";

            if (_focusCountLabel != null)
                _focusCountLabel.text = $"Focuses: {_opticSO.FocusCount}";

            if (_focusBar != null)
                _focusBar.value = _opticSO.FocusProgress;
        }

        public ZoneControlCaptureOpticSO OpticSO => _opticSO;
    }
}
