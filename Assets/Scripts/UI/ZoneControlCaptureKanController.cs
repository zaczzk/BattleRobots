using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureKanController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureKanSO _kanSO;
        [SerializeField] private PlayerWallet            _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onKanExtended;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _extensionLabel;
        [SerializeField] private Text       _extendLabel;
        [SerializeField] private Slider     _extensionBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleExtendedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleExtendedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onKanExtended?.RegisterCallback(_handleExtendedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onKanExtended?.UnregisterCallback(_handleExtendedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_kanSO == null) return;
            int bonus = _kanSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_kanSO == null) return;
            _kanSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _kanSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_kanSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_extensionLabel != null)
                _extensionLabel.text = $"Extensions: {_kanSO.Extensions}/{_kanSO.ExtensionsNeeded}";

            if (_extendLabel != null)
                _extendLabel.text = $"Extends: {_kanSO.ExtensionCount}";

            if (_extensionBar != null)
                _extensionBar.value = _kanSO.ExtensionProgress;
        }

        public ZoneControlCaptureKanSO KanSO => _kanSO;
    }
}
