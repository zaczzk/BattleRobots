using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureGodelIncompletenessController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureGodelIncompletenessSO _godelSO;
        [SerializeField] private PlayerWallet                            _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onExtensionCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _extensionLabel;
        [SerializeField] private Text       _completionLabel;
        [SerializeField] private Slider     _extensionBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleExtensionDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleExtensionDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onExtensionCompleted?.RegisterCallback(_handleExtensionDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onExtensionCompleted?.UnregisterCallback(_handleExtensionDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_godelSO == null) return;
            int bonus = _godelSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_godelSO == null) return;
            _godelSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _godelSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_godelSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_extensionLabel != null)
                _extensionLabel.text = $"Extensions: {_godelSO.ConsistentExtensions}/{_godelSO.ConsistentExtensionsNeeded}";

            if (_completionLabel != null)
                _completionLabel.text = $"Completions: {_godelSO.ExtensionCount}";

            if (_extensionBar != null)
                _extensionBar.value = _godelSO.ExtensionProgress;
        }

        public ZoneControlCaptureGodelIncompletenessSO GodelSO => _godelSO;
    }
}
