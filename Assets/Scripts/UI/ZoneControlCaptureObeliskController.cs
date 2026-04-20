using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureObeliskController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureObeliskSO _obeliskSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onObeliskInscribed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _runeLabel;
        [SerializeField] private Text       _inscriptionLabel;
        [SerializeField] private Slider     _runeBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleObeliskInscribedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate          = HandlePlayerCaptured;
            _handleBotDelegate             = HandleBotCaptured;
            _handleMatchStartedDelegate    = HandleMatchStarted;
            _handleObeliskInscribedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onObeliskInscribed?.RegisterCallback(_handleObeliskInscribedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onObeliskInscribed?.UnregisterCallback(_handleObeliskInscribedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_obeliskSO == null) return;
            int bonus = _obeliskSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_obeliskSO == null) return;
            _obeliskSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _obeliskSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_obeliskSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_runeLabel != null)
                _runeLabel.text = $"Runes: {_obeliskSO.Runes}/{_obeliskSO.RunesNeeded}";

            if (_inscriptionLabel != null)
                _inscriptionLabel.text = $"Inscriptions: {_obeliskSO.InscriptionCount}";

            if (_runeBar != null)
                _runeBar.value = _obeliskSO.RuneProgress;
        }

        public ZoneControlCaptureObeliskSO ObeliskSO => _obeliskSO;
    }
}
