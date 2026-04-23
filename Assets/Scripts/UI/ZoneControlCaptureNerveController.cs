using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureNerveController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureNerveSO _nerveSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onNerveRealized;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _simplexLabel;
        [SerializeField] private Text       _realizationLabel;
        [SerializeField] private Slider     _simplexBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRealizedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleRealizedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onNerveRealized?.RegisterCallback(_handleRealizedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onNerveRealized?.UnregisterCallback(_handleRealizedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_nerveSO == null) return;
            int bonus = _nerveSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_nerveSO == null) return;
            _nerveSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _nerveSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_nerveSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_simplexLabel != null)
                _simplexLabel.text = $"Simplices: {_nerveSO.Simplices}/{_nerveSO.SimplicesNeeded}";

            if (_realizationLabel != null)
                _realizationLabel.text = $"Realizations: {_nerveSO.RealizationCount}";

            if (_simplexBar != null)
                _simplexBar.value = _nerveSO.SimplexProgress;
        }

        public ZoneControlCaptureNerveSO NerveSO => _nerveSO;
    }
}
