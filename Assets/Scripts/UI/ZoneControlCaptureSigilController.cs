using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSigilController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSigilSO _sigilSO;
        [SerializeField] private PlayerWalletSO              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSigilAwakened;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _sigilLabel;
        [SerializeField] private Text       _awakeningCountLabel;
        [SerializeField] private Slider     _sigilBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleSigilAwakenedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleSigilAwakenedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSigilAwakened?.RegisterCallback(_handleSigilAwakenedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSigilAwakened?.UnregisterCallback(_handleSigilAwakenedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_sigilSO == null) return;
            int prev  = _sigilSO.SigilCount;
            int bonus = _sigilSO.RecordPlayerCapture();
            if (_sigilSO.SigilCount > prev)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_sigilSO == null) return;
            _sigilSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _sigilSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_sigilSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_sigilLabel != null)
                _sigilLabel.text = $"Sigil: {_sigilSO.SigilCharges}/{_sigilSO.CapturesForSigil}";

            if (_awakeningCountLabel != null)
                _awakeningCountLabel.text = $"Awakenings: {_sigilSO.SigilCount}";

            if (_sigilBar != null)
                _sigilBar.value = _sigilSO.SigilProgress;
        }

        public ZoneControlCaptureSigilSO SigilSO => _sigilSO;
    }
}
