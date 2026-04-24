using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureKTheoryController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureKTheorySO _kTheorySO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onKTheoryClassified;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _bundleLabel;
        [SerializeField] private Text       _classifyLabel;
        [SerializeField] private Slider     _bundleBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleClassifiedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleClassifiedDelegate   = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onKTheoryClassified?.RegisterCallback(_handleClassifiedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onKTheoryClassified?.UnregisterCallback(_handleClassifiedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_kTheorySO == null) return;
            int bonus = _kTheorySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_kTheorySO == null) return;
            _kTheorySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _kTheorySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_kTheorySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_bundleLabel != null)
                _bundleLabel.text = $"Bundles: {_kTheorySO.Bundles}/{_kTheorySO.BundlesNeeded}";

            if (_classifyLabel != null)
                _classifyLabel.text = $"Classifications: {_kTheorySO.ClassificationCount}";

            if (_bundleBar != null)
                _bundleBar.value = _kTheorySO.BundleProgress;
        }

        public ZoneControlCaptureKTheorySO KTheorySO => _kTheorySO;
    }
}
