using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureGerbeController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureGerbeSO _gerbeSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onGerbeBound;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _torsorLabel;
        [SerializeField] private Text       _bindingLabel;
        [SerializeField] private Slider     _torsorBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleBoundDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleBoundDelegate        = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onGerbeBound?.RegisterCallback(_handleBoundDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onGerbeBound?.UnregisterCallback(_handleBoundDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_gerbeSO == null) return;
            int bonus = _gerbeSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_gerbeSO == null) return;
            _gerbeSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _gerbeSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_gerbeSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_torsorLabel != null)
                _torsorLabel.text = $"Torsors: {_gerbeSO.Torsors}/{_gerbeSO.TorsorsNeeded}";

            if (_bindingLabel != null)
                _bindingLabel.text = $"Bindings: {_gerbeSO.BindingCount}";

            if (_torsorBar != null)
                _torsorBar.value = _gerbeSO.TorsorProgress;
        }

        public ZoneControlCaptureGerbeSO GerbeSO => _gerbeSO;
    }
}
