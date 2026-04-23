using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureInternalController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureInternalSO _internalSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onInternalCategoryBuilt;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _objectLabel;
        [SerializeField] private Text       _internalLabel;
        [SerializeField] private Slider     _objectBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleBuiltDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleBuiltDelegate        = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onInternalCategoryBuilt?.RegisterCallback(_handleBuiltDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onInternalCategoryBuilt?.UnregisterCallback(_handleBuiltDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_internalSO == null) return;
            int bonus = _internalSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_internalSO == null) return;
            _internalSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _internalSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_internalSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_objectLabel != null)
                _objectLabel.text = $"Objects: {_internalSO.Objects}/{_internalSO.ObjectsNeeded}";

            if (_internalLabel != null)
                _internalLabel.text = $"Internal: {_internalSO.InternalCount}";

            if (_objectBar != null)
                _objectBar.value = _internalSO.ObjectProgress;
        }

        public ZoneControlCaptureInternalSO InternalSO => _internalSO;
    }
}
