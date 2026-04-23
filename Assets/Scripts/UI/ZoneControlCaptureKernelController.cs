using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureKernelController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureKernelSO _kernelSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onKernelComputed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _elementLabel;
        [SerializeField] private Text       _kernelLabel;
        [SerializeField] private Slider     _elementBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleComputedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleComputedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onKernelComputed?.RegisterCallback(_handleComputedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onKernelComputed?.UnregisterCallback(_handleComputedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_kernelSO == null) return;
            int bonus = _kernelSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_kernelSO == null) return;
            _kernelSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _kernelSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_kernelSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_elementLabel != null)
                _elementLabel.text = $"Elements: {_kernelSO.Elements}/{_kernelSO.ElementsNeeded}";

            if (_kernelLabel != null)
                _kernelLabel.text = $"Kernels: {_kernelSO.KernelCount}";

            if (_elementBar != null)
                _elementBar.value = _kernelSO.ElementProgress;
        }

        public ZoneControlCaptureKernelSO KernelSO => _kernelSO;
    }
}
