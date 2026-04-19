using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureShadowController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureShadowSO _shadowSO;
        [SerializeField] private PlayerWalletSO             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onShadowCleared;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _debtLabel;
        [SerializeField] private Text       _clearedLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleBotDelegate;
        private Action _handlePlayerDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleShadowClearedDelegate;

        private void Awake()
        {
            _handleBotDelegate          = HandleBotCaptured;
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleShadowClearedDelegate = HandleShadowCleared;
        }

        private void OnEnable()
        {
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onShadowCleared?.RegisterCallback(_handleShadowClearedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onShadowCleared?.UnregisterCallback(_handleShadowClearedDelegate);
        }

        private void HandleBotCaptured()
        {
            if (_shadowSO == null) return;
            _shadowSO.RecordBotCapture();
            Refresh();
        }

        private void HandlePlayerCaptured()
        {
            if (_shadowSO == null) return;
            int prev = _shadowSO.ClearedCount;
            _shadowSO.RecordPlayerCapture();
            if (_shadowSO.ClearedCount > prev)
                _wallet?.AddFunds(_shadowSO.BonusPerClear);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _shadowSO?.Reset();
            Refresh();
        }

        private void HandleShadowCleared()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (_shadowSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_debtLabel != null)
                _debtLabel.text = $"Shadow Debt: {_shadowSO.ShadowDebt}";

            if (_clearedLabel != null)
                _clearedLabel.text = $"Cleared: {_shadowSO.ClearedCount}";
        }

        public ZoneControlCaptureShadowSO ShadowSO => _shadowSO;
    }
}
