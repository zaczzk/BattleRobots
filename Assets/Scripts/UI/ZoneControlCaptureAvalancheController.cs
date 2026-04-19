using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureAvalancheController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureAvalancheSO _avalancheSO;
        [SerializeField] private PlayerWalletSO                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onAvalanche;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _multiplierLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onAvalanche?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onAvalanche?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleZoneCaptured()
        {
            if (_avalancheSO == null) return;
            int bonus = _avalancheSO.RecordCapture(Time.time);
            _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _avalancheSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_avalancheSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_multiplierLabel != null)
                _multiplierLabel.text = $"x{_avalancheSO.CurrentMultiplier:F1}\u00d7";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Avalanche: {_avalancheSO.TotalBonusAwarded}";
        }

        public ZoneControlCaptureAvalancheSO AvalancheSO => _avalancheSO;
    }
}
