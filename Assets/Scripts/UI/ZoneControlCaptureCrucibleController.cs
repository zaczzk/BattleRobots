using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCrucibleController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCrucibleSO _crucibleSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCrucibleAlloyed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _oreLabel;
        [SerializeField] private Text       _alloyLabel;
        [SerializeField] private Slider     _oreBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleAlloyedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleAlloyedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCrucibleAlloyed?.RegisterCallback(_handleAlloyedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCrucibleAlloyed?.UnregisterCallback(_handleAlloyedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_crucibleSO == null) return;
            int bonus = _crucibleSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_crucibleSO == null) return;
            _crucibleSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _crucibleSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_crucibleSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_oreLabel != null)
                _oreLabel.text = $"Ore: {_crucibleSO.Ore}/{_crucibleSO.OreNeeded}";

            if (_alloyLabel != null)
                _alloyLabel.text = $"Alloys: {_crucibleSO.AlloyCount}";

            if (_oreBar != null)
                _oreBar.value = _crucibleSO.OreProgress;
        }

        public ZoneControlCaptureCrucibleSO CrucibleSO => _crucibleSO;
    }
}
