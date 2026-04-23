using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureGerbe", order = 390)]
    public sealed class ZoneControlCaptureGerbeSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _torsorsNeeded   = 5;
        [SerializeField, Min(1)] private int _deflectPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerBinding = 2590;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onGerbeBound;

        private int _torsors;
        private int _bindingCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   TorsorsNeeded     => _torsorsNeeded;
        public int   DeflectPerBot     => _deflectPerBot;
        public int   BonusPerBinding   => _bonusPerBinding;
        public int   Torsors           => _torsors;
        public int   BindingCount      => _bindingCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float TorsorProgress    => _torsorsNeeded > 0
            ? Mathf.Clamp01(_torsors / (float)_torsorsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _torsors = Mathf.Min(_torsors + 1, _torsorsNeeded);
            if (_torsors >= _torsorsNeeded)
            {
                int bonus = _bonusPerBinding;
                _bindingCount++;
                _totalBonusAwarded += bonus;
                _torsors            = 0;
                _onGerbeBound?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _torsors = Mathf.Max(0, _torsors - _deflectPerBot);
        }

        public void Reset()
        {
            _torsors           = 0;
            _bindingCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}
