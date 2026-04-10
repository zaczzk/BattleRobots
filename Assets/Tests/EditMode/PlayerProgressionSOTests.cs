using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PlayerProgressionSO"/>.
    ///
    /// Covers:
    ///   • Fresh-instance defaults (Level=1, TotalXP=0, IsMaxLevel false).
    ///   • <see cref="PlayerProgressionSO.TotalXPForLevel"/> static helper.
    ///   • <see cref="PlayerProgressionSO.AddXP"/>: accumulation, single/multi level-up,
    ///     no-op at max level, event firing, max-level XP clamp.
    ///   • Computed properties: XpInCurrentLevel, XpRequiredForNextLevel, XpProgressFraction.
    ///   • <see cref="PlayerProgressionSO.LoadSnapshot"/>: silent rehydration,
    ///     level-0 clamp-to-1, negative XP clamp.
    ///   • <see cref="PlayerProgressionSO.Reset"/>: silent clear (no events).
    /// </summary>
    public class PlayerProgressionSOTests
    {
        private PlayerProgressionSO _so;
        private VoidGameEvent       _onLevelUp;
        private IntGameEvent        _onXPGained;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private void WireEvents()
        {
            SetField(_so, "_onLevelUp",  _onLevelUp);
            SetField(_so, "_onXPGained", _onXPGained);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _so         = ScriptableObject.CreateInstance<PlayerProgressionSO>();
            _onLevelUp  = ScriptableObject.CreateInstance<VoidGameEvent>();
            _onXPGained = ScriptableObject.CreateInstance<IntGameEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_so);
            Object.DestroyImmediate(_onLevelUp);
            Object.DestroyImmediate(_onXPGained);
            _so = null; _onLevelUp = null; _onXPGained = null;
        }

        // ── TotalXPForLevel static helper ─────────────────────────────────────

        [Test]
        public void TotalXPForLevel_Level1_ReturnsZero()
        {
            Assert.AreEqual(0, PlayerProgressionSO.TotalXPForLevel(1));
        }

        [Test]
        public void TotalXPForLevel_Level2_Returns100()
        {
            Assert.AreEqual(100, PlayerProgressionSO.TotalXPForLevel(2));
        }

        [Test]
        public void TotalXPForLevel_Level3_Returns300()
        {
            Assert.AreEqual(300, PlayerProgressionSO.TotalXPForLevel(3));
        }

        [Test]
        public void TotalXPForLevel_Level5_Returns1000()
        {
            Assert.AreEqual(1000, PlayerProgressionSO.TotalXPForLevel(5));
        }

        [Test]
        public void TotalXPForLevel_Level0OrNegative_ReturnsZero()
        {
            Assert.AreEqual(0, PlayerProgressionSO.TotalXPForLevel(0));
            Assert.AreEqual(0, PlayerProgressionSO.TotalXPForLevel(-5));
        }

        // ── Fresh instance ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_CurrentLevel_IsOne()
        {
            Assert.AreEqual(1, _so.CurrentLevel);
        }

        [Test]
        public void FreshInstance_TotalXP_IsZero()
        {
            Assert.AreEqual(0, _so.TotalXP);
        }

        [Test]
        public void FreshInstance_IsMaxLevel_False()
        {
            Assert.IsFalse(_so.IsMaxLevel);
        }

        [Test]
        public void FreshInstance_XpProgressFraction_IsZero()
        {
            Assert.AreEqual(0f, _so.XpProgressFraction, delta: 0.001f);
        }

        // ── AddXP: basic accumulation ─────────────────────────────────────────

        [Test]
        public void AddXP_PositiveAmount_IncreasesTotalXP()
        {
            _so.AddXP(50);
            Assert.AreEqual(50, _so.TotalXP);
        }

        [Test]
        public void AddXP_ZeroAmount_NoChange()
        {
            _so.AddXP(0);
            Assert.AreEqual(0, _so.TotalXP);
            Assert.AreEqual(1, _so.CurrentLevel);
        }

        [Test]
        public void AddXP_NegativeAmount_NoChange()
        {
            _so.AddXP(-50);
            Assert.AreEqual(0, _so.TotalXP);
            Assert.AreEqual(1, _so.CurrentLevel);
        }

        [Test]
        public void AddXP_BelowThreshold_NoLevelUp()
        {
            _so.AddXP(99); // level 1→2 requires 100
            Assert.AreEqual(1, _so.CurrentLevel);
        }

        [Test]
        public void AddXP_ExactThreshold_LevelsUpToTwo()
        {
            _so.AddXP(100); // exactly reaches level 2 threshold
            Assert.AreEqual(2, _so.CurrentLevel);
        }

        [Test]
        public void AddXP_ExceedsThreshold_LevelsUp_XpInCurrentLevelIsRemainder()
        {
            _so.AddXP(150); // 150 - 100 = 50 XP into level 2
            Assert.AreEqual(2, _so.CurrentLevel);
            Assert.AreEqual(50, _so.XpInCurrentLevel);
        }

        [Test]
        public void AddXP_MultiLevelUp_InOneCall()
        {
            // Level 1→2=100, 2→3=200 extra (300 total). Adding 300 should reach level 3.
            _so.AddXP(300);
            Assert.AreEqual(3, _so.CurrentLevel);
        }

        [Test]
        public void AddXP_FiresOnXPGainedEvent_WithCorrectAmount()
        {
            WireEvents();
            int received = 0;
            _onXPGained.RegisterCallback(amt => received = amt);

            _so.AddXP(75);

            Assert.AreEqual(75, received, "OnXPGained payload must equal the amount added.");
        }

        [Test]
        public void AddXP_FiresOnLevelUpEvent_OncePerLevelGained()
        {
            WireEvents();
            int levelUps = 0;
            _onLevelUp.RegisterCallback(() => levelUps++);

            _so.AddXP(300); // gains 2 levels (1→2 + 2→3)

            Assert.AreEqual(2, levelUps, "OnLevelUp must fire once per level gained.");
        }

        [Test]
        public void AddXP_AtMaxLevel_IsNoOp()
        {
            // Force to max level by loading snapshot.
            _so.LoadSnapshot(PlayerProgressionSO.TotalXPForLevel(_so.MaxLevel), _so.MaxLevel);
            int xpBefore = _so.TotalXP;

            WireEvents();
            int levelUps = 0;
            _onLevelUp.RegisterCallback(() => levelUps++);

            _so.AddXP(1000);

            Assert.AreEqual(xpBefore, _so.TotalXP, "TotalXP must not change at max level.");
            Assert.AreEqual(0, levelUps, "OnLevelUp must not fire at max level.");
        }

        [Test]
        public void AddXP_ClampsTotalXP_WhenLevelingToMax()
        {
            // Set max level to 2 via LoadSnapshot approach — use a tiny snapshot then add.
            // Simpler: just add enough XP to reach max level (default maxLevel=10).
            // Add enough XP to skip to level 10 (TotalXPForLevel(10) = 50*10*9 = 4500).
            _so.AddXP(5000); // more than needed for level 10
            Assert.AreEqual(_so.MaxLevel, _so.CurrentLevel);
            Assert.AreEqual(PlayerProgressionSO.TotalXPForLevel(_so.MaxLevel), _so.TotalXP,
                "TotalXP must be clamped to max-level threshold when at cap.");
        }

        // ── Computed properties ───────────────────────────────────────────────

        [Test]
        public void XpRequiredForNextLevel_AtLevel1_Returns100()
        {
            Assert.AreEqual(100, _so.XpRequiredForNextLevel);
        }

        [Test]
        public void XpRequiredForNextLevel_AtMaxLevel_ReturnsZero()
        {
            _so.LoadSnapshot(PlayerProgressionSO.TotalXPForLevel(_so.MaxLevel), _so.MaxLevel);
            Assert.AreEqual(0, _so.XpRequiredForNextLevel);
        }

        [Test]
        public void XpProgressFraction_AtMaxLevel_ReturnsOne()
        {
            _so.LoadSnapshot(PlayerProgressionSO.TotalXPForLevel(_so.MaxLevel), _so.MaxLevel);
            Assert.AreEqual(1f, _so.XpProgressFraction, delta: 0.001f);
        }

        [Test]
        public void XpProgressFraction_HalfwayToNextLevel_ReturnsHalf()
        {
            _so.AddXP(50); // halfway to level 2 (needs 100)
            Assert.AreEqual(0.5f, _so.XpProgressFraction, delta: 0.001f);
        }

        // ── LoadSnapshot ──────────────────────────────────────────────────────

        [Test]
        public void LoadSnapshot_SetsBothFields()
        {
            _so.LoadSnapshot(totalXP: 300, level: 3);
            Assert.AreEqual(300, _so.TotalXP);
            Assert.AreEqual(3,   _so.CurrentLevel);
        }

        [Test]
        public void LoadSnapshot_ZeroLevel_ClampedToOne()
        {
            _so.LoadSnapshot(totalXP: 0, level: 0);
            Assert.AreEqual(1, _so.CurrentLevel,
                "Level 0 (old-save default) must be clamped to 1.");
        }

        [Test]
        public void LoadSnapshot_NegativeXP_ClampedToZero()
        {
            _so.LoadSnapshot(totalXP: -50, level: 1);
            Assert.AreEqual(0, _so.TotalXP);
        }

        [Test]
        public void LoadSnapshot_DoesNotFireEvents()
        {
            WireEvents();
            int levelUps = 0;
            int xpGains  = 0;
            _onLevelUp.RegisterCallback(() => levelUps++);
            _onXPGained.RegisterCallback(_ => xpGains++);

            _so.LoadSnapshot(totalXP: 500, level: 4);

            Assert.AreEqual(0, levelUps, "LoadSnapshot must not fire _onLevelUp.");
            Assert.AreEqual(0, xpGains,  "LoadSnapshot must not fire _onXPGained.");
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsToLevel1AndZeroXP()
        {
            _so.LoadSnapshot(totalXP: 1000, level: 5);
            _so.Reset();
            Assert.AreEqual(1, _so.CurrentLevel);
            Assert.AreEqual(0, _so.TotalXP);
        }

        [Test]
        public void Reset_DoesNotFireEvents()
        {
            WireEvents();
            _so.LoadSnapshot(totalXP: 500, level: 4); // set non-default state
            int levelUps = 0;
            int xpGains  = 0;
            _onLevelUp.RegisterCallback(() => levelUps++);
            _onXPGained.RegisterCallback(_ => xpGains++);

            _so.Reset();

            Assert.AreEqual(0, levelUps, "Reset() must not fire _onLevelUp.");
            Assert.AreEqual(0, xpGains,  "Reset() must not fire _onXPGained.");
        }
    }
}
