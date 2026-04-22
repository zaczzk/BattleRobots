using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCacheController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCacheSO _cacheSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCacheHit;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _slotLabel;
        [SerializeField] private Text       _hitLabel;
        [SerializeField] private Slider     _slotBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleHitDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleHitDelegate          = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCacheHit?.RegisterCallback(_handleHitDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCacheHit?.UnregisterCallback(_handleHitDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_cacheSO == null) return;
            int bonus = _cacheSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_cacheSO == null) return;
            _cacheSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _cacheSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_cacheSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_slotLabel != null)
                _slotLabel.text = $"Slots: {_cacheSO.Slots}/{_cacheSO.SlotsNeeded}";

            if (_hitLabel != null)
                _hitLabel.text = $"Cache Hits: {_cacheSO.HitCount}";

            if (_slotBar != null)
                _slotBar.value = _cacheSO.SlotProgress;
        }

        public ZoneControlCaptureCacheSO CacheSO => _cacheSO;
    }
}
