using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureAnchorController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureAnchorSO _anchorSO;
        [SerializeField] private PlayerWalletSO             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onAnchorBonus;
        [SerializeField] private VoidGameEvent _onAnchorBroken;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _chainLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleAnchorBonusDelegate;
        private Action _handleAnchorBrokenDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleAnchorBonusDelegate  = Refresh;
            _handleAnchorBrokenDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onAnchorBonus?.RegisterCallback(_handleAnchorBonusDelegate);
            _onAnchorBroken?.RegisterCallback(_handleAnchorBrokenDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onAnchorBonus?.UnregisterCallback(_handleAnchorBonusDelegate);
            _onAnchorBroken?.UnregisterCallback(_handleAnchorBrokenDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_anchorSO == null) return;
            int bonus = _anchorSO.RecordPlayerCapture();
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_anchorSO == null) return;
            _anchorSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _anchorSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_anchorSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
                _statusLabel.text = _anchorSO.IsAnchored ? "ANCHORED!" : "No Anchor";

            if (_chainLabel != null)
                _chainLabel.text = $"Chain: {_anchorSO.AnchorChainLength}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Anchor Bonus: {_anchorSO.TotalAnchorBonus}";
        }

        public ZoneControlCaptureAnchorSO AnchorSO => _anchorSO;
    }
}
