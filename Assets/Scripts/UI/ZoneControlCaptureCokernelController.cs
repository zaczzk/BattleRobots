using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCokernelController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCokernelSO _cokernelSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCokernelProjected;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _vectorLabel;
        [SerializeField] private Text       _cokernelLabel;
        [SerializeField] private Slider     _vectorBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleProjectedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleProjectedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCokernelProjected?.RegisterCallback(_handleProjectedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCokernelProjected?.UnregisterCallback(_handleProjectedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_cokernelSO == null) return;
            int bonus = _cokernelSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_cokernelSO == null) return;
            _cokernelSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _cokernelSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_cokernelSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_vectorLabel != null)
                _vectorLabel.text = $"Vectors: {_cokernelSO.Vectors}/{_cokernelSO.VectorsNeeded}";

            if (_cokernelLabel != null)
                _cokernelLabel.text = $"Cokernels: {_cokernelSO.CokernelCount}";

            if (_vectorBar != null)
                _vectorBar.value = _cokernelSO.VectorProgress;
        }

        public ZoneControlCaptureCokernelSO CokernelSO => _cokernelSO;
    }
}
