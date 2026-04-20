using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureLantern", order = 227)]
    public sealed class ZoneControlCaptureLanternSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _lanternsNeeded       = 5;
        [SerializeField, Min(0)] private int _bonusPerIllumination = 375;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onIlluminated;

        private int _litLanterns;
        private int _illuminationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   LanternsNeeded       => _lanternsNeeded;
        public int   BonusPerIllumination => _bonusPerIllumination;
        public int   LitLanterns          => _litLanterns;
        public int   IlluminationCount    => _illuminationCount;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public float LanternProgress      => _lanternsNeeded > 0
            ? Mathf.Clamp01(_litLanterns / (float)_lanternsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _litLanterns++;
            if (_litLanterns >= _lanternsNeeded)
                return Illuminate();
            return 0;
        }

        public void RecordBotCapture()
        {
            _litLanterns = Mathf.Max(0, _litLanterns - 1);
        }

        private int Illuminate()
        {
            _illuminationCount++;
            _totalBonusAwarded += _bonusPerIllumination;
            _litLanterns        = 0;
            _onIlluminated?.Raise();
            return _bonusPerIllumination;
        }

        public void Reset()
        {
            _litLanterns       = 0;
            _illuminationCount = 0;
            _totalBonusAwarded = 0;
        }
    }
}
