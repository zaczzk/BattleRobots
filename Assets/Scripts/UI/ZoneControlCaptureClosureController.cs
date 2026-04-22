using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureClosureController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureClosureSO _closureSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onClosureSealed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _bindingLabel;
        [SerializeField] private Text       _closureLabel;
        [SerializeField] private Slider     _bindingBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleClosureSealedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleClosureSealedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onClosureSealed?.RegisterCallback(_handleClosureSealedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onClosureSealed?.UnregisterCallback(_handleClosureSealedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_closureSO == null) return;
            int bonus = _closureSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_closureSO == null) return;
            _closureSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _closureSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_closureSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_bindingLabel != null)
                _bindingLabel.text = $"Bindings: {_closureSO.Bindings}/{_closureSO.BindingsNeeded}";

            if (_closureLabel != null)
                _closureLabel.text = $"Closures: {_closureSO.ClosureCount}";

            if (_bindingBar != null)
                _bindingBar.value = _closureSO.BindingProgress;
        }

        public ZoneControlCaptureClosureSO ClosureSO => _closureSO;
    }
}
