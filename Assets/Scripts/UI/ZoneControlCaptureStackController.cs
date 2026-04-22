using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureStackController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureStackSO _stackSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onStackReturned;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _frameLabel;
        [SerializeField] private Text       _returnLabel;
        [SerializeField] private Slider     _frameBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleReturnedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleReturnedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onStackReturned?.RegisterCallback(_handleReturnedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onStackReturned?.UnregisterCallback(_handleReturnedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_stackSO == null) return;
            int bonus = _stackSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_stackSO == null) return;
            _stackSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _stackSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_stackSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_frameLabel != null)
                _frameLabel.text = $"Frames: {_stackSO.Frames}/{_stackSO.FramesNeeded}";

            if (_returnLabel != null)
                _returnLabel.text = $"Returns: {_stackSO.ReturnCount}";

            if (_frameBar != null)
                _frameBar.value = _stackSO.FrameProgress;
        }

        public ZoneControlCaptureStackSO StackSO => _stackSO;
    }
}
