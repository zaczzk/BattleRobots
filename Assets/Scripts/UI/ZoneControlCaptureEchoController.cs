using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureEchoController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureEchoSO _echoSO;
        [SerializeField] private PlayerWalletSO           _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onEcho;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _echoLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleEchoDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleEchoDelegate         = HandleEcho;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onEcho?.RegisterCallback(_handleEchoDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onEcho?.UnregisterCallback(_handleEchoDelegate);
        }

        private void HandleZoneCaptured()
        {
            if (_echoSO == null) return;
            _echoSO.RecordCapture(Time.time);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _echoSO?.Reset();
            Refresh();
        }

        private void HandleEcho()
        {
            if (_echoSO == null) return;
            _wallet?.AddFunds(_echoSO.BonusPerEcho);
            Refresh();
        }

        public void Refresh()
        {
            if (_echoSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_echoLabel != null)
                _echoLabel.text = $"Echo: {_echoSO.EchoCount}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Echo Bonus: {_echoSO.TotalBonusAwarded}";
        }

        public ZoneControlCaptureEchoSO EchoSO => _echoSO;
    }
}
