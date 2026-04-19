using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureMirageController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureMirageSO _mirageSO;
        [SerializeField] private PlayerWalletSO             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMirageStack;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _stackLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private Slider     _stackProgressBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMirageStackDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleMirageStackDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMirageStack?.RegisterCallback(_handleMirageStackDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMirageStack?.UnregisterCallback(_handleMirageStackDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_mirageSO == null) return;
            int bonus = _mirageSO.RecordPlayerCapture();
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_mirageSO == null) return;
            _mirageSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _mirageSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_mirageSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_stackLabel != null)
                _stackLabel.text = $"Stacks: {_mirageSO.CurrentStacks}/{_mirageSO.MaxStacks}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Mirage Bonus: {_mirageSO.TotalMirageBonus}";

            if (_stackProgressBar != null)
                _stackProgressBar.value = _mirageSO.StackProgress;
        }

        public ZoneControlCaptureMirageSO MirageSO => _mirageSO;
    }
}
