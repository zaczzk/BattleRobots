using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureProofNetsController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureProofNetsSO _proofNetsSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onProofNetsCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _netLinkLabel;
        [SerializeField] private Text       _netLinkCountLabel;
        [SerializeField] private Slider     _netLinkBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCompletedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCompletedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onProofNetsCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onProofNetsCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_proofNetsSO == null) return;
            int bonus = _proofNetsSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_proofNetsSO == null) return;
            _proofNetsSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _proofNetsSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_proofNetsSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_netLinkLabel != null)
                _netLinkLabel.text =
                    $"Net Links: {_proofNetsSO.NetLinks}/{_proofNetsSO.NetLinksNeeded}";

            if (_netLinkCountLabel != null)
                _netLinkCountLabel.text = $"Net Links: {_proofNetsSO.NetLinkCount}";

            if (_netLinkBar != null)
                _netLinkBar.value = _proofNetsSO.NetLinkProgress;
        }

        public ZoneControlCaptureProofNetsSO ProofNetsSO => _proofNetsSO;
    }
}
