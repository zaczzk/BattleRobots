using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSyracuseSequenceController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSyracuseSequenceSO _syracuseSO;
        [SerializeField] private PlayerWallet                         _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSyracuseSequenceDescended;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _descentLabel;
        [SerializeField] private Text       _descentCountLabel;
        [SerializeField] private Slider     _descentBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleDescentDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleDescentDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSyracuseSequenceDescended?.RegisterCallback(_handleDescentDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSyracuseSequenceDescended?.UnregisterCallback(_handleDescentDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_syracuseSO == null) return;
            int bonus = _syracuseSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_syracuseSO == null) return;
            _syracuseSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _syracuseSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_syracuseSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_descentLabel != null)
                _descentLabel.text = $"Descents: {_syracuseSO.Descents}/{_syracuseSO.DescentsNeeded}";

            if (_descentCountLabel != null)
                _descentCountLabel.text = $"Total Descents: {_syracuseSO.DescentCount}";

            if (_descentBar != null)
                _descentBar.value = _syracuseSO.DescentProgress;
        }

        public ZoneControlCaptureSyracuseSequenceSO SyracuseSO => _syracuseSO;
    }
}
