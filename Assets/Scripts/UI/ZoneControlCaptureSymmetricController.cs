using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSymmetricController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSymmetricSO _symmetricSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSymmetrized;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _swapLabel;
        [SerializeField] private Text       _symmetryLabel;
        [SerializeField] private Slider     _swapBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleSymmetrizedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleSymmetrizedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSymmetrized?.RegisterCallback(_handleSymmetrizedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSymmetrized?.UnregisterCallback(_handleSymmetrizedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_symmetricSO == null) return;
            int bonus = _symmetricSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_symmetricSO == null) return;
            _symmetricSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _symmetricSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_symmetricSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_swapLabel != null)
                _swapLabel.text = $"Swaps: {_symmetricSO.Swaps}/{_symmetricSO.SwapsNeeded}";

            if (_symmetryLabel != null)
                _symmetryLabel.text = $"Symmetries: {_symmetricSO.SymmetryCount}";

            if (_swapBar != null)
                _swapBar.value = _symmetricSO.SwapProgress;
        }

        public ZoneControlCaptureSymmetricSO SymmetricSO => _symmetricSO;
    }
}
