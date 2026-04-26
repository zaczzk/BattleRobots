using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureLinearTypeController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureLinearTypeSO _linearTypeSO;
        [SerializeField] private PlayerWallet                   _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onLinearTypeCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _linearUseLabel;
        [SerializeField] private Text       _linearUseCountLabel;
        [SerializeField] private Slider     _linearUseBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCompletedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCompletedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onLinearTypeCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onLinearTypeCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_linearTypeSO == null) return;
            int bonus = _linearTypeSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_linearTypeSO == null) return;
            _linearTypeSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _linearTypeSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_linearTypeSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_linearUseLabel != null)
                _linearUseLabel.text =
                    $"Linear Uses: {_linearTypeSO.LinearUses}/{_linearTypeSO.LinearUsesNeeded}";

            if (_linearUseCountLabel != null)
                _linearUseCountLabel.text = $"Linear Uses: {_linearTypeSO.LinearUseCount}";

            if (_linearUseBar != null)
                _linearUseBar.value = _linearTypeSO.LinearUseProgress;
        }

        public ZoneControlCaptureLinearTypeSO LinearTypeSO => _linearTypeSO;
    }
}
