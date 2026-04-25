using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureFermatLastTheoremController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureFermatLastTheoremSO _fermatSO;
        [SerializeField] private PlayerWallet                          _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFermatLastTheoremProved;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _modularFormLabel;
        [SerializeField] private Text       _proofLabel;
        [SerializeField] private Slider     _modularFormBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleProofDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleProofDelegate        = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFermatLastTheoremProved?.RegisterCallback(_handleProofDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFermatLastTheoremProved?.UnregisterCallback(_handleProofDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_fermatSO == null) return;
            int bonus = _fermatSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_fermatSO == null) return;
            _fermatSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _fermatSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_fermatSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_modularFormLabel != null)
                _modularFormLabel.text = $"Modular Forms: {_fermatSO.ModularForms}/{_fermatSO.ModularFormsNeeded}";

            if (_proofLabel != null)
                _proofLabel.text = $"Proofs: {_fermatSO.ProofCount}";

            if (_modularFormBar != null)
                _modularFormBar.value = _fermatSO.ModularFormProgress;
        }

        public ZoneControlCaptureFermatLastTheoremSO FermatSO => _fermatSO;
    }
}
