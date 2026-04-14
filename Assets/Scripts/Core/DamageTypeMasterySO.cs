using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that accumulates cumulative per-type damage totals across all matches
    /// and awards a mastery flag when a type's accumulation meets or exceeds the
    /// threshold defined in the optional <see cref="DamageTypeMasteryConfig"/>.
    ///
    /// ── Mastery mechanic ─────────────────────────────────────────────────────────────
    ///   • <see cref="AddDealt"/> increments the correct type accumulator and checks
    ///     whether the threshold has just been crossed.
    ///   • When a threshold is crossed for the first time, the mastery flag is set and
    ///     <c>_onMasteryUnlocked</c> is raised once.
    ///   • Already-mastered types are never re-flagged.
    ///   • If no <see cref="DamageTypeMasteryConfig"/> is assigned mastery is never
    ///     awarded (all thresholds are effectively infinite).
    ///
    /// ── Persistence ──────────────────────────────────────────────────────────────────
    ///   • <see cref="LoadSnapshot"/> restores state from SaveData mastery fields.
    ///   • <see cref="TakeSnapshot"/> writes state back to SaveData mastery fields.
    ///   • <see cref="MatchManager"/> calls <see cref="AddDealtFromStats"/> in EndMatch
    ///     and <see cref="TakeSnapshot"/> before <c>SaveSystem.Save()</c>.
    ///   • <see cref="GameBootstrapper"/> calls <see cref="LoadSnapshot"/> on startup.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime accumulators and flags are NOT serialized to the SO asset.
    ///   - Zero alloc hot path: float addition + switch + bool check.
    ///   - <c>_onMasteryUnlocked</c> fires at most once per type per career.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Combat ▶ DamageTypeMastery.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Combat/DamageTypeMastery",
        fileName = "DamageTypeMasterySO")]
    public sealed class DamageTypeMasterySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Config SO specifying cumulative damage thresholds per type. " +
                 "Leave null to disable mastery awards (no threshold checks performed).")]
        [SerializeField] private DamageTypeMasteryConfig _config;

        [Tooltip("VoidGameEvent raised once when any type first reaches its threshold. " +
                 "Leave null if no system needs to react.")]
        [SerializeField] private VoidGameEvent _onMasteryUnlocked;

        // ── Runtime state ─────────────────────────────────────────────────────
        // Not serialized — rehydrated from SaveData mastery fields via LoadSnapshot.

        private float _physicalAccum;
        private float _energyAccum;
        private float _thermalAccum;
        private float _shockAccum;

        private bool _physicalMastered;
        private bool _energyMastered;
        private bool _thermalMastered;
        private bool _shockMastered;

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>The optional config SO specifying mastery thresholds. May be null.</summary>
        public DamageTypeMasteryConfig Config => _config;

        /// <summary>Cumulative Physical damage dealt across all matches.</summary>
        public float PhysicalAccum => _physicalAccum;

        /// <summary>Cumulative Energy damage dealt across all matches.</summary>
        public float EnergyAccum   => _energyAccum;

        /// <summary>Cumulative Thermal damage dealt across all matches.</summary>
        public float ThermalAccum  => _thermalAccum;

        /// <summary>Cumulative Shock damage dealt across all matches.</summary>
        public float ShockAccum    => _shockAccum;

        /// <summary>
        /// Returns the cumulative accumulation for <paramref name="type"/>.
        /// Returns 0 for unknown types.
        /// </summary>
        public float GetAccumulation(DamageType type)
        {
            switch (type)
            {
                case DamageType.Physical: return _physicalAccum;
                case DamageType.Energy:   return _energyAccum;
                case DamageType.Thermal:  return _thermalAccum;
                case DamageType.Shock:    return _shockAccum;
                default:                  return 0f;
            }
        }

        /// <summary>
        /// Returns true when the player has mastered <paramref name="type"/>
        /// (accumulation ≥ threshold at some point in their career).
        /// Returns false for unknown types.
        /// </summary>
        public bool IsTypeMastered(DamageType type)
        {
            switch (type)
            {
                case DamageType.Physical: return _physicalMastered;
                case DamageType.Energy:   return _energyMastered;
                case DamageType.Thermal:  return _thermalMastered;
                case DamageType.Shock:    return _shockMastered;
                default:                  return false;
            }
        }

        /// <summary>
        /// Returns the mastery progress ratio for <paramref name="type"/> in [0, 1].
        /// 1 = mastered (or over threshold).  Returns 0 when no config is assigned
        /// or the threshold is zero.
        /// </summary>
        public float GetProgress(DamageType type)
        {
            if (_config == null) return 0f;
            float threshold = _config.GetThreshold(type);
            if (threshold <= 0f) return 0f;
            return Mathf.Clamp01(GetAccumulation(type) / threshold);
        }

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Adds <paramref name="amount"/> to the accumulator for <paramref name="type"/>
        /// and checks whether the mastery threshold has just been crossed.
        /// When a threshold is crossed for the first time, the mastery flag is set and
        /// <c>_onMasteryUnlocked</c> is raised.
        /// Ignores amounts ≤ 0 and unknown types.
        /// </summary>
        public void AddDealt(float amount, DamageType type)
        {
            if (amount <= 0f) return;

            switch (type)
            {
                case DamageType.Physical:
                    _physicalAccum += amount;
                    if (_config != null && !_physicalMastered
                        && _physicalAccum >= _config.GetThreshold(DamageType.Physical))
                    {
                        _physicalMastered = true;
                        _onMasteryUnlocked?.Raise();
                    }
                    break;

                case DamageType.Energy:
                    _energyAccum += amount;
                    if (_config != null && !_energyMastered
                        && _energyAccum >= _config.GetThreshold(DamageType.Energy))
                    {
                        _energyMastered = true;
                        _onMasteryUnlocked?.Raise();
                    }
                    break;

                case DamageType.Thermal:
                    _thermalAccum += amount;
                    if (_config != null && !_thermalMastered
                        && _thermalAccum >= _config.GetThreshold(DamageType.Thermal))
                    {
                        _thermalMastered = true;
                        _onMasteryUnlocked?.Raise();
                    }
                    break;

                case DamageType.Shock:
                    _shockAccum += amount;
                    if (_config != null && !_shockMastered
                        && _shockAccum >= _config.GetThreshold(DamageType.Shock))
                    {
                        _shockMastered = true;
                        _onMasteryUnlocked?.Raise();
                    }
                    break;
            }
        }

        /// <summary>
        /// Convenience method called by <see cref="MatchManager"/> at match end.
        /// Iterates all four types and calls <see cref="AddDealt"/> with the per-type
        /// totals from <paramref name="stats"/>.
        /// No-op when <paramref name="stats"/> is null.
        /// </summary>
        public void AddDealtFromStats(MatchStatisticsSO stats)
        {
            if (stats == null) return;
            AddDealt(stats.GetDealtByType(DamageType.Physical), DamageType.Physical);
            AddDealt(stats.GetDealtByType(DamageType.Energy),   DamageType.Energy);
            AddDealt(stats.GetDealtByType(DamageType.Thermal),  DamageType.Thermal);
            AddDealt(stats.GetDealtByType(DamageType.Shock),    DamageType.Shock);
        }

        /// <summary>
        /// Silently rehydrates state from SaveData mastery fields.
        /// Does NOT fire any events — safe to call from <see cref="GameBootstrapper"/>.
        /// Negative accumulators are clamped to 0.
        /// </summary>
        public void LoadSnapshot(
            float physicalAccum, float energyAccum,
            float thermalAccum,  float shockAccum,
            bool  physicalDone,  bool  energyDone,
            bool  thermalDone,   bool  shockDone)
        {
            _physicalAccum    = Mathf.Max(0f, physicalAccum);
            _energyAccum      = Mathf.Max(0f, energyAccum);
            _thermalAccum     = Mathf.Max(0f, thermalAccum);
            _shockAccum       = Mathf.Max(0f, shockAccum);
            _physicalMastered = physicalDone;
            _energyMastered   = energyDone;
            _thermalMastered  = thermalDone;
            _shockMastered    = shockDone;
        }

        /// <summary>
        /// Writes current runtime state to eight <c>out</c> parameters for SaveData storage.
        /// Called by <see cref="MatchManager"/> just before <c>SaveSystem.Save()</c>.
        /// </summary>
        public void TakeSnapshot(
            out float physicalAccum, out float energyAccum,
            out float thermalAccum,  out float shockAccum,
            out bool  physicalDone,  out bool  energyDone,
            out bool  thermalDone,   out bool  shockDone)
        {
            physicalAccum = _physicalAccum;
            energyAccum   = _energyAccum;
            thermalAccum  = _thermalAccum;
            shockAccum    = _shockAccum;
            physicalDone  = _physicalMastered;
            energyDone    = _energyMastered;
            thermalDone   = _thermalMastered;
            shockDone     = _shockMastered;
        }

        /// <summary>
        /// Silently resets all accumulators and mastery flags to zero / false.
        /// Does NOT fire any events.
        /// </summary>
        public void Reset()
        {
            _physicalAccum    = 0f;
            _energyAccum      = 0f;
            _thermalAccum     = 0f;
            _shockAccum       = 0f;
            _physicalMastered = false;
            _energyMastered   = false;
            _thermalMastered  = false;
            _shockMastered    = false;
        }
    }
}
