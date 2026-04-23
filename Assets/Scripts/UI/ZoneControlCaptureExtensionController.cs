using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureExtensionController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureExtensionSO _extensionSO;
        [SerializeField] private PlayerWallet                   _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onExtensionApplied;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _elementLabel;
        [SerializeField] private Text       _extensionLabel;
        [SerializeField] private Slider     _elementBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleAppliedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleAppliedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onExtensionApplied?.RegisterCallback(_handleAppliedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onExtensionApplied?.UnregisterCallback(_handleAppliedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_extensionSO == null) return;
            int bonus = _extensionSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_extensionSO == null) return;
            _extensionSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _extensionSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_extensionSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_elementLabel != null)
                _elementLabel.text = $"Elements: {_extensionSO.Elements}/{_extensionSO.ElementsNeeded}";

            if (_extensionLabel != null)
                _extensionLabel.text = $"Extensions: {_extensionSO.ExtensionCount}";

            if (_elementBar != null)
                _elementBar.value = _extensionSO.ElementProgress;
        }

        public ZoneControlCaptureExtensionSO ExtensionSO => _extensionSO;
    }
}
