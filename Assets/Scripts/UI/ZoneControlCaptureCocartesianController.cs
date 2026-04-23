using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCocartesianController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCocartesianSO _cocartesianSO;
        [SerializeField] private PlayerWallet                    _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCodiagonalized;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _injectionLabel;
        [SerializeField] private Text       _codiagonalLabel;
        [SerializeField] private Slider     _injectionBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCodiagonalizedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate         = HandlePlayerCaptured;
            _handleBotDelegate            = HandleBotCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handleCodiagonalizedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCodiagonalized?.RegisterCallback(_handleCodiagonalizedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCodiagonalized?.UnregisterCallback(_handleCodiagonalizedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_cocartesianSO == null) return;
            int bonus = _cocartesianSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_cocartesianSO == null) return;
            _cocartesianSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _cocartesianSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_cocartesianSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_injectionLabel != null)
                _injectionLabel.text = $"Injections: {_cocartesianSO.Injections}/{_cocartesianSO.InjectionsNeeded}";

            if (_codiagonalLabel != null)
                _codiagonalLabel.text = $"Codiagonalizations: {_cocartesianSO.CodiagonalizeCount}";

            if (_injectionBar != null)
                _injectionBar.value = _cocartesianSO.InjectionProgress;
        }

        public ZoneControlCaptureCocartesianSO CocartesianSO => _cocartesianSO;
    }
}
