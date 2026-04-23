using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCoproductController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCoproductSO _coproductSO;
        [SerializeField] private PlayerWallet                   _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCoproductInjected;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _objectLabel;
        [SerializeField] private Text       _coproductLabel;
        [SerializeField] private Slider     _objectBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleInjectedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleInjectedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCoproductInjected?.RegisterCallback(_handleInjectedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCoproductInjected?.UnregisterCallback(_handleInjectedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_coproductSO == null) return;
            int bonus = _coproductSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_coproductSO == null) return;
            _coproductSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _coproductSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_coproductSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_objectLabel != null)
                _objectLabel.text = $"Objects: {_coproductSO.Objects}/{_coproductSO.ObjectsNeeded}";

            if (_coproductLabel != null)
                _coproductLabel.text = $"Coproducts: {_coproductSO.CoproductCount}";

            if (_objectBar != null)
                _objectBar.value = _coproductSO.ObjectProgress;
        }

        public ZoneControlCaptureCoproductSO CoproductSO => _coproductSO;
    }
}
