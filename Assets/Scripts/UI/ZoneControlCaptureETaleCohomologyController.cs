using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureETaleCohomologyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureETaleCohomologySO _etaleSO;
        [SerializeField] private PlayerWallet                         _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onETaleSheafified;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _stalkLabel;
        [SerializeField] private Text       _sheafifyLabel;
        [SerializeField] private Slider     _stalkBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleSheafifiedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleSheafifiedDelegate   = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onETaleSheafified?.RegisterCallback(_handleSheafifiedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onETaleSheafified?.UnregisterCallback(_handleSheafifiedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_etaleSO == null) return;
            int bonus = _etaleSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_etaleSO == null) return;
            _etaleSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _etaleSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_etaleSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_stalkLabel != null)
                _stalkLabel.text = $"Stalks: {_etaleSO.Stalks}/{_etaleSO.StalksNeeded}";

            if (_sheafifyLabel != null)
                _sheafifyLabel.text = $"Sheafifications: {_etaleSO.SheafifyCount}";

            if (_stalkBar != null)
                _stalkBar.value = _etaleSO.StalkProgress;
        }

        public ZoneControlCaptureETaleCohomologySO EtaleSO => _etaleSO;
    }
}
