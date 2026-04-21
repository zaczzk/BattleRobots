using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureArmatureController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureArmatureSO _armatureSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onArmatureWound;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _coilLabel;
        [SerializeField] private Text       _windingLabel;
        [SerializeField] private Slider     _coilBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleWoundDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleWoundDelegate        = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onArmatureWound?.RegisterCallback(_handleWoundDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onArmatureWound?.UnregisterCallback(_handleWoundDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_armatureSO == null) return;
            int bonus = _armatureSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_armatureSO == null) return;
            _armatureSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _armatureSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_armatureSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_coilLabel != null)
                _coilLabel.text = $"Coils: {_armatureSO.Coils}/{_armatureSO.CoilsNeeded}";

            if (_windingLabel != null)
                _windingLabel.text = $"Windings: {_armatureSO.WindingCount}";

            if (_coilBar != null)
                _coilBar.value = _armatureSO.CoilProgress;
        }

        public ZoneControlCaptureArmatureSO ArmatureSO => _armatureSO;
    }
}
