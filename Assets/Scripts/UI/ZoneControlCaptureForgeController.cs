using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureForgeController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureForgeSO _forgeSO;
        [SerializeField] private PlayerWalletSO             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onForge;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _ingotLabel;
        [SerializeField] private Text       _rawLabel;
        [SerializeField] private Text       _forgeCountLabel;
        [SerializeField] private Slider     _ingotBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleForgeDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleForgeDelegate        = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onForge?.RegisterCallback(_handleForgeDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onForge?.UnregisterCallback(_handleForgeDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_forgeSO == null) return;
            int prev = _forgeSO.ForgeCount;
            _forgeSO.RecordPlayerCapture();
            if (_forgeSO.ForgeCount > prev)
                _wallet?.AddFunds(_forgeSO.BonusPerForge);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_forgeSO == null) return;
            _forgeSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _forgeSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_forgeSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_ingotLabel != null)
                _ingotLabel.text = $"Ingots: {_forgeSO.IngotCount}/{_forgeSO.IngotsForForge}";

            if (_rawLabel != null)
                _rawLabel.text = $"Raw: {_forgeSO.RawCaptures}/{_forgeSO.CapturesPerIngot}";

            if (_forgeCountLabel != null)
                _forgeCountLabel.text = $"Forges: {_forgeSO.ForgeCount}";

            if (_ingotBar != null)
                _ingotBar.value = _forgeSO.IngotProgress;
        }

        public ZoneControlCaptureForgeSO ForgeSO => _forgeSO;
    }
}
