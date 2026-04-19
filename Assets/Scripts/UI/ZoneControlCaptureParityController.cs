using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureParityController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureParitySO _paritySO;
        [SerializeField] private PlayerWalletSO             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onParity;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _tiesLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate = HandlePlayerCaptured;
            _handleBotCapturedDelegate    = HandleBotCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _refreshDelegate              = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onParity?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onParity?.UnregisterCallback(_refreshDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_paritySO == null) return;
            int prevParity = _paritySO.ParityCount;
            _paritySO.RecordPlayerCapture();
            if (_paritySO.ParityCount > prevParity)
                _wallet?.AddFunds(_paritySO.BonusPerParity);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_paritySO == null) return;
            int prevParity = _paritySO.ParityCount;
            _paritySO.RecordBotCapture();
            if (_paritySO.ParityCount > prevParity)
                _wallet?.AddFunds(_paritySO.BonusPerParity);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _paritySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_paritySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_tiesLabel != null)
                _tiesLabel.text = $"Ties: {_paritySO.ParityCount}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Parity Bonus: {_paritySO.TotalBonusAwarded}";

            if (_statusLabel != null)
                _statusLabel.text = _paritySO.AreTied ? "TIED!" : "Uneven";
        }

        public ZoneControlCaptureParitySO ParitySO => _paritySO;
    }
}
