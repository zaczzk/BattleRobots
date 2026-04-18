using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneHoldRatioController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneHoldRatioSO _holdRatioSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onMajorityAchieved;
        [SerializeField] private VoidGameEvent _onMajorityLost;
        [SerializeField] private VoidGameEvent _onRatioUpdated;

        [Header("UI References (optional)")]
        [SerializeField] private Text   _holdRatioLabel;
        [SerializeField] private Slider _holdRatioBar;
        [SerializeField] private GameObject _panel;

        private Action _handleMatchStartedDelegate;
        private Action _handleMatchEndedDelegate;
        private Action _handleMajorityAchievedDelegate;
        private Action _handleMajorityLostDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleMatchStartedDelegate    = HandleMatchStarted;
            _handleMatchEndedDelegate      = HandleMatchEnded;
            _handleMajorityAchievedDelegate = HandleMajorityAchieved;
            _handleMajorityLostDelegate    = HandleMajorityLost;
            _refreshDelegate               = Refresh;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onMajorityAchieved?.RegisterCallback(_handleMajorityAchievedDelegate);
            _onMajorityLost?.RegisterCallback(_handleMajorityLostDelegate);
            _onRatioUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onMajorityAchieved?.UnregisterCallback(_handleMajorityAchievedDelegate);
            _onMajorityLost?.UnregisterCallback(_handleMajorityLostDelegate);
            _onRatioUpdated?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            if (_holdRatioSO == null || !_holdRatioSO.IsRunning) return;
            _holdRatioSO.Tick(Time.deltaTime);
        }

        private void HandleMatchStarted()
        {
            _holdRatioSO?.StartMatch();
            Refresh();
        }

        private void HandleMatchEnded()
        {
            _holdRatioSO?.EndMatch();
            Refresh();
        }

        private void HandleMajorityAchieved()
        {
            _holdRatioSO?.SetMajority(true);
            Refresh();
        }

        private void HandleMajorityLost()
        {
            _holdRatioSO?.SetMajority(false);
            Refresh();
        }

        public void Refresh()
        {
            if (_holdRatioSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_holdRatioLabel != null)
                _holdRatioLabel.text = $"Hold Ratio: {Mathf.RoundToInt(_holdRatioSO.HoldRatio * 100f)}%";

            if (_holdRatioBar != null)
                _holdRatioBar.value = _holdRatioSO.HoldRatio;
        }

        public ZoneControlZoneHoldRatioSO HoldRatioSO => _holdRatioSO;
    }
}
