using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureFrost", order = 171)]
    public sealed class ZoneControlCaptureFrostSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _freezeThreshold = 3;
        [SerializeField, Min(1)] private int _thawCaptures    = 2;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFrozen;
        [SerializeField] private VoidGameEvent _onThawed;

        private int  _consecutiveBotCaptures;
        private bool _isFrozen;
        private int  _thawProgress;
        private int  _totalFreezes;

        private void OnEnable() => Reset();

        public int   FreezeThreshold        => _freezeThreshold;
        public int   ThawCaptures           => _thawCaptures;
        public int   ConsecutiveBotCaptures => _consecutiveBotCaptures;
        public bool  IsFrozen               => _isFrozen;
        public int   ThawProgress           => _thawProgress;
        public int   TotalFreezes           => _totalFreezes;

        public float FrostProgress => _isFrozen
            ? Mathf.Clamp01((float)_thawProgress / Mathf.Max(1, _thawCaptures))
            : Mathf.Clamp01((float)_consecutiveBotCaptures / Mathf.Max(1, _freezeThreshold));

        public void RecordBotCapture()
        {
            if (_isFrozen) return;
            _consecutiveBotCaptures++;
            if (_consecutiveBotCaptures >= _freezeThreshold)
                SetFrozen();
        }

        public void RecordPlayerCapture()
        {
            _consecutiveBotCaptures = 0;
            if (!_isFrozen) return;
            _thawProgress++;
            if (_thawProgress >= _thawCaptures)
                Thaw();
        }

        private void SetFrozen()
        {
            _isFrozen = true;
            _totalFreezes++;
            _onFrozen?.Raise();
        }

        private void Thaw()
        {
            _isFrozen     = false;
            _thawProgress = 0;
            _onThawed?.Raise();
        }

        public void Reset()
        {
            _consecutiveBotCaptures = 0;
            _isFrozen               = false;
            _thawProgress           = 0;
            _totalFreezes           = 0;
        }
    }
}
