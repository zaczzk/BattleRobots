using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureLocale", order = 437)]
    public sealed class ZoneControlCaptureLocaleSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _opensNeeded    = 7;
        [SerializeField, Min(1)] private int _closePerBot    = 2;
        [SerializeField, Min(0)] private int _bonusPerCover  = 3295;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLocaleCovered;

        private int _opens;
        private int _coverCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   OpensNeeded       => _opensNeeded;
        public int   ClosePerBot       => _closePerBot;
        public int   BonusPerCover     => _bonusPerCover;
        public int   Opens             => _opens;
        public int   CoverCount        => _coverCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float OpenProgress      => _opensNeeded > 0
            ? Mathf.Clamp01(_opens / (float)_opensNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _opens = Mathf.Min(_opens + 1, _opensNeeded);
            if (_opens >= _opensNeeded)
            {
                int bonus = _bonusPerCover;
                _coverCount++;
                _totalBonusAwarded += bonus;
                _opens              = 0;
                _onLocaleCovered?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _opens = Mathf.Max(0, _opens - _closePerBot);
        }

        public void Reset()
        {
            _opens             = 0;
            _coverCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}
