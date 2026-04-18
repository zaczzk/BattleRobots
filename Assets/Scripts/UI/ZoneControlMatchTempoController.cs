using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchTempoController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchTempoSO   _tempoSO;
        [SerializeField] private ZoneControlCaptureRateSO  _rateSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onTempoChanged;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _tempoLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleMatchStartedDelegate;
        private Action _handleZoneCapturedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onTempoChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onTempoChanged?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleMatchStarted()
        {
            _tempoSO?.Reset();
            Refresh();
        }

        private void HandleZoneCaptured()
        {
            if (_tempoSO == null || _rateSO == null) return;
            float rate = _rateSO.GetAverageRate(Time.time);
            _tempoSO.EvaluateTempo(rate);
            Refresh();
        }

        public void Refresh()
        {
            if (_tempoSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_tempoLabel != null)
                _tempoLabel.text = $"Tempo: {_tempoSO.GetTempoLabel()}";
        }

        public ZoneControlMatchTempoSO TempoSO => _tempoSO;
    }
}
