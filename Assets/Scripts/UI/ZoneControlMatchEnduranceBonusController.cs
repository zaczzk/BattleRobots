using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchEnduranceBonusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchEnduranceBonusSO _enduranceSO;
        [SerializeField] private PlayerWallet                     _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onBonusApplied;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _captureLabel;
        [SerializeField] private Text       _tierLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMatchEndedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleMatchEndedDelegate   = HandleMatchEnded;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onBonusApplied?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onBonusApplied?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleZoneCaptured()
        {
            _enduranceSO?.RecordCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _enduranceSO?.Reset();
            Refresh();
        }

        private void HandleMatchEnded()
        {
            if (_enduranceSO == null) return;
            int bonus = _enduranceSO.ApplyEndBonus();
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        public void Refresh()
        {
            if (_enduranceSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_captureLabel != null)
                _captureLabel.text = $"Caps: {_enduranceSO.CaptureCount}";

            if (_tierLabel != null)
                _tierLabel.text = $"Tier: {_enduranceSO.ComputeTier()}";
        }

        public ZoneControlMatchEnduranceBonusSO EnduranceSO => _enduranceSO;
    }
}
