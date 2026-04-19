using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlThreatCaptureBonusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlThreatCaptureBonusSO _threatBonusSO;
        [SerializeField] private ZoneControlZoneThreatLevelSO    _threatLevelSO;
        [SerializeField] private PlayerWalletSO                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onThreatCaptureBonus;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _countLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleThreatCaptureBonusDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate    = HandlePlayerCaptured;
            _handleMatchStartedDelegate      = HandleMatchStarted;
            _handleThreatCaptureBonusDelegate = HandleThreatCaptureBonus;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onThreatCaptureBonus?.RegisterCallback(_handleThreatCaptureBonusDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onThreatCaptureBonus?.UnregisterCallback(_handleThreatCaptureBonusDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_threatBonusSO == null) return;
            if (_threatLevelSO != null &&
                _threatLevelSO.CurrentThreat == ZoneControlThreatLevel.High)
            {
                _threatBonusSO.RecordThreatCapture();
            }
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _threatBonusSO?.Reset();
            Refresh();
        }

        private void HandleThreatCaptureBonus()
        {
            if (_threatBonusSO == null) return;
            _wallet?.AddFunds(_threatBonusSO.BonusPerThreatCapture);
            Refresh();
        }

        public void Refresh()
        {
            if (_threatBonusSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_countLabel != null)
                _countLabel.text = $"Threat Caps: {_threatBonusSO.ThreatCaptureCount}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Threat Bonus: {_threatBonusSO.TotalBonusAwarded}";
        }

        public ZoneControlThreatCaptureBonusSO ThreatBonusSO => _threatBonusSO;
    }
}
