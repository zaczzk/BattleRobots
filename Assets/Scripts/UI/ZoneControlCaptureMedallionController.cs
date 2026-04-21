using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureMedallionController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureMedallionSO _medallionSO;
        [SerializeField] private PlayerWallet                   _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMedallionComplete;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _inscriptionLabel;
        [SerializeField] private Text       _medallionLabel;
        [SerializeField] private Slider     _inscriptionBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMedallionCompleteDelegate;

        private void Awake()
        {
            _handlePlayerDelegate            = HandlePlayerCaptured;
            _handleBotDelegate               = HandleBotCaptured;
            _handleMatchStartedDelegate      = HandleMatchStarted;
            _handleMedallionCompleteDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMedallionComplete?.RegisterCallback(_handleMedallionCompleteDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMedallionComplete?.UnregisterCallback(_handleMedallionCompleteDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_medallionSO == null) return;
            int bonus = _medallionSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_medallionSO == null) return;
            _medallionSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _medallionSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_medallionSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_inscriptionLabel != null)
                _inscriptionLabel.text = $"Inscriptions: {_medallionSO.Inscriptions}/{_medallionSO.InscriptionsNeeded}";

            if (_medallionLabel != null)
                _medallionLabel.text = $"Medallions: {_medallionSO.MedallionCount}";

            if (_inscriptionBar != null)
                _inscriptionBar.value = _medallionSO.InscriptionProgress;
        }

        public ZoneControlCaptureMedallionSO MedallionSO => _medallionSO;
    }
}
