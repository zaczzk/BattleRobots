using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlComebackBonusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlComebackBonusSO  _comebackBonusSO;
        [SerializeField] private ZoneControlRivalTrackerSO   _rivalTrackerSO;
        [SerializeField] private PlayerWalletSO              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onComebackBonus;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _comebackLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onComebackBonus?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onComebackBonus?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleZoneCaptured()
        {
            if (_comebackBonusSO == null) return;
            if (_rivalTrackerSO != null && _rivalTrackerSO.IsRivalLeading)
            {
                _comebackBonusSO.RecordComeback();
                _wallet?.AddFunds(_comebackBonusSO.BonusOnComeback);
            }
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _comebackBonusSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_comebackBonusSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_comebackLabel != null)
                _comebackLabel.text = $"Comebacks: {_comebackBonusSO.ComebackCount}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Bonus: {_comebackBonusSO.TotalBonusAwarded}";
        }

        public ZoneControlComebackBonusSO ComebackBonusSO => _comebackBonusSO;
    }
}
