using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureGlyph", order = 225)]
    public sealed class ZoneControlCaptureGlyphSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _glyphsNeeded       = 4;
        [SerializeField, Min(0)] private int _bonusPerInscription = 200;
        [SerializeField, Min(1)] private int _empoweredCaptures   = 3;
        [SerializeField, Min(0)] private int _empoweredBonus      = 80;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onGlyphInscribed;
        [SerializeField] private VoidGameEvent _onEmpowermentEnded;

        private int  _glyphCount;
        private int  _inscriptionCount;
        private int  _empoweredRemaining;
        private bool _isEmpowered;
        private int  _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int  GlyphsNeeded        => _glyphsNeeded;
        public int  BonusPerInscription => _bonusPerInscription;
        public int  EmpoweredCaptures   => _empoweredCaptures;
        public int  EmpoweredBonus      => _empoweredBonus;
        public int  GlyphCount          => _glyphCount;
        public int  InscriptionCount    => _inscriptionCount;
        public bool IsEmpowered         => _isEmpowered;
        public int  EmpoweredRemaining  => _empoweredRemaining;
        public int  TotalBonusAwarded   => _totalBonusAwarded;
        public float GlyphProgress      => _glyphsNeeded > 0
            ? Mathf.Clamp01(_glyphCount / (float)_glyphsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            if (_isEmpowered)
            {
                _totalBonusAwarded += _empoweredBonus;
                _empoweredRemaining--;
                if (_empoweredRemaining <= 0)
                    EndEmpowerment();
                return _empoweredBonus;
            }

            _glyphCount++;
            if (_glyphCount >= _glyphsNeeded)
            {
                Inscribe();
                return _bonusPerInscription;
            }

            return 0;
        }

        public void RecordBotCapture()
        {
            if (_isEmpowered)
                EndEmpowerment();
            else
                _glyphCount = Mathf.Max(0, _glyphCount - 1);
        }

        private void Inscribe()
        {
            _inscriptionCount++;
            _glyphCount         = 0;
            _isEmpowered        = true;
            _empoweredRemaining = _empoweredCaptures;
            _totalBonusAwarded += _bonusPerInscription;
            _onGlyphInscribed?.Raise();
        }

        private void EndEmpowerment()
        {
            _isEmpowered        = false;
            _empoweredRemaining = 0;
            _onEmpowermentEnded?.Raise();
        }

        public void Reset()
        {
            _glyphCount         = 0;
            _inscriptionCount   = 0;
            _empoweredRemaining = 0;
            _isEmpowered        = false;
            _totalBonusAwarded  = 0;
        }
    }
}
