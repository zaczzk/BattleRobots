using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureVaultController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureVaultSO _vaultSO;
        [SerializeField] private PlayerWalletSO            _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onVaultPayout;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _vaultLabel;
        [SerializeField] private Text       _payoutLabel;
        [SerializeField] private Slider     _vaultBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleVaultPayoutDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleVaultPayoutDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onVaultPayout?.RegisterCallback(_handleVaultPayoutDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onVaultPayout?.UnregisterCallback(_handleVaultPayoutDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_vaultSO == null) return;
            _vaultSO.RecordPlayerCapture();
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_vaultSO == null) return;
            int payout = _vaultSO.RecordBotCapture();
            if (payout > 0)
                _wallet?.AddFunds(payout);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _vaultSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_vaultSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_vaultLabel != null)
                _vaultLabel.text = $"Vault: {_vaultSO.VaultBalance}/{_vaultSO.MaxVault}";

            if (_payoutLabel != null)
                _payoutLabel.text = $"Last Payout: {_vaultSO.LastPayoutAmount}";

            if (_vaultBar != null)
                _vaultBar.value = _vaultSO.VaultProgress;
        }

        public ZoneControlCaptureVaultSO VaultSO => _vaultSO;
    }
}
