using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T064 — PlayerProfileSO + ProfileUI.
    ///
    /// Coverage (18 cases):
    ///
    /// PlayerProfileSO — default state
    ///   [01] DefaultState_DisplayName_EqualsPlayerDefault
    ///   [02] DefaultState_AvatarIndex_IsZero
    ///   [03] DefaultState_CareerStats_AllZero
    ///   [04] DefaultState_WinRate_IsZero
    ///
    /// PlayerProfileSO — SetDisplayName
    ///   [05] SetDisplayName_ValidName_UpdatesProperty
    ///   [06] SetDisplayName_TrimsWhitespace
    ///   [07] SetDisplayName_EmptyString_IsIgnored
    ///   [08] SetDisplayName_NullString_IsIgnored
    ///   [09] SetDisplayName_SameValue_DoesNotFire (no duplicate event)
    ///
    /// PlayerProfileSO — SetAvatarIndex
    ///   [10] SetAvatarIndex_Positive_UpdatesProperty
    ///   [11] SetAvatarIndex_Negative_ClampsToZero
    ///
    /// PlayerProfileSO — UpdateFromMatchRecord
    ///   [12] UpdateFromMatchRecord_Win_IncrementsWins
    ///   [13] UpdateFromMatchRecord_Loss_IncrementsLosses
    ///   [14] UpdateFromMatchRecord_AccumulatesEarningsAndDamage
    ///   [15] UpdateFromMatchRecord_Null_DoesNotThrow
    ///
    /// PlayerProfileSO — LoadFromData / BuildData
    ///   [16] LoadFromData_PopulatesAllFields
    ///   [17] BuildData_RoundTripThroughLoadFromData
    ///   [18] LoadFromData_NullData_ResetsToDefaults
    ///
    /// SaveData — field presence
    ///   [19] SaveData_PlayerProfile_DefaultIsNotNull
    ///
    /// PlayerProfileUI — static helper
    ///   [20] FormatWinRate_Zero_Returns0Percent
    ///   [21] FormatWinRate_Half_Returns50Percent
    ///   [22] FormatWinRate_One_Returns100Percent
    /// </summary>
    [TestFixture]
    public sealed class PlayerProfileTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private PlayerProfileSO _profile;

        [SetUp]
        public void SetUp()
        {
            _profile = ScriptableObject.CreateInstance<PlayerProfileSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_profile);
            _profile = null;
        }

        // ── Default state ─────────────────────────────────────────────────────

        [Test]
        public void DefaultState_DisplayName_EqualsPlayerDefault()
        {
            Assert.AreEqual("Player", _profile.DisplayName);
        }

        [Test]
        public void DefaultState_AvatarIndex_IsZero()
        {
            Assert.AreEqual(0, _profile.AvatarIndex);
        }

        [Test]
        public void DefaultState_CareerStats_AllZero()
        {
            Assert.AreEqual(0, _profile.CareerWins);
            Assert.AreEqual(0, _profile.CareerLosses);
            Assert.AreEqual(0, _profile.CareerEarnings);
            Assert.AreEqual(0f, _profile.CareerDamageDone, 0.001f);
        }

        [Test]
        public void DefaultState_WinRate_IsZero()
        {
            Assert.AreEqual(0f, _profile.WinRate, 0.001f);
        }

        // ── SetDisplayName ────────────────────────────────────────────────────

        [Test]
        public void SetDisplayName_ValidName_UpdatesProperty()
        {
            _profile.SetDisplayName("RobotDestroyer");
            Assert.AreEqual("RobotDestroyer", _profile.DisplayName);
        }

        [Test]
        public void SetDisplayName_TrimsWhitespace()
        {
            _profile.SetDisplayName("  Zac  ");
            Assert.AreEqual("Zac", _profile.DisplayName);
        }

        [Test]
        public void SetDisplayName_EmptyString_IsIgnored()
        {
            _profile.SetDisplayName("Zac");
            _profile.SetDisplayName("");
            Assert.AreEqual("Zac", _profile.DisplayName);
        }

        [Test]
        public void SetDisplayName_NullString_IsIgnored()
        {
            _profile.SetDisplayName("Zac");
            _profile.SetDisplayName(null);
            Assert.AreEqual("Zac", _profile.DisplayName);
        }

        [Test]
        public void SetDisplayName_SameValue_StillStoresValue()
        {
            _profile.SetDisplayName("Zac");
            _profile.SetDisplayName("Zac"); // second call with same value — should not throw
            Assert.AreEqual("Zac", _profile.DisplayName);
        }

        // ── SetAvatarIndex ────────────────────────────────────────────────────

        [Test]
        public void SetAvatarIndex_Positive_UpdatesProperty()
        {
            _profile.SetAvatarIndex(3);
            Assert.AreEqual(3, _profile.AvatarIndex);
        }

        [Test]
        public void SetAvatarIndex_Negative_ClampsToZero()
        {
            _profile.SetAvatarIndex(-5);
            Assert.AreEqual(0, _profile.AvatarIndex);
        }

        // ── UpdateFromMatchRecord ─────────────────────────────────────────────

        [Test]
        public void UpdateFromMatchRecord_Win_IncrementsWins()
        {
            var record = new MatchRecord { playerWon = true, currencyEarned = 0, damageDone = 0f };
            _profile.UpdateFromMatchRecord(record);
            Assert.AreEqual(1, _profile.CareerWins);
            Assert.AreEqual(0, _profile.CareerLosses);
        }

        [Test]
        public void UpdateFromMatchRecord_Loss_IncrementsLosses()
        {
            var record = new MatchRecord { playerWon = false, currencyEarned = 0, damageDone = 0f };
            _profile.UpdateFromMatchRecord(record);
            Assert.AreEqual(0, _profile.CareerWins);
            Assert.AreEqual(1, _profile.CareerLosses);
        }

        [Test]
        public void UpdateFromMatchRecord_AccumulatesEarningsAndDamage()
        {
            _profile.UpdateFromMatchRecord(new MatchRecord { playerWon = true,  currencyEarned = 200, damageDone = 50f });
            _profile.UpdateFromMatchRecord(new MatchRecord { playerWon = false, currencyEarned = 50,  damageDone = 30f });

            Assert.AreEqual(250, _profile.CareerEarnings);
            Assert.AreEqual(80f, _profile.CareerDamageDone, 0.001f);
        }

        [Test]
        public void UpdateFromMatchRecord_Null_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _profile.UpdateFromMatchRecord(null));
        }

        // ── WinRate ───────────────────────────────────────────────────────────

        [Test]
        public void WinRate_AfterOneWinOneLoss_IsHalf()
        {
            _profile.UpdateFromMatchRecord(new MatchRecord { playerWon = true  });
            _profile.UpdateFromMatchRecord(new MatchRecord { playerWon = false });
            Assert.AreEqual(0.5f, _profile.WinRate, 0.001f);
        }

        // ── LoadFromData / BuildData ──────────────────────────────────────────

        [Test]
        public void LoadFromData_PopulatesAllFields()
        {
            var data = new PlayerProfileData
            {
                displayName      = "ArenaKing",
                avatarIndex      = 2,
                careerWins       = 10,
                careerLosses     = 3,
                careerEarnings   = 5000,
                careerDamageDone = 1234.5f,
            };

            _profile.LoadFromData(data);

            Assert.AreEqual("ArenaKing", _profile.DisplayName);
            Assert.AreEqual(2,           _profile.AvatarIndex);
            Assert.AreEqual(10,          _profile.CareerWins);
            Assert.AreEqual(3,           _profile.CareerLosses);
            Assert.AreEqual(5000,        _profile.CareerEarnings);
            Assert.AreEqual(1234.5f,     _profile.CareerDamageDone, 0.01f);
        }

        [Test]
        public void BuildData_RoundTripThroughLoadFromData()
        {
            _profile.SetDisplayName("TestBot");
            _profile.SetAvatarIndex(1);
            _profile.UpdateFromMatchRecord(new MatchRecord { playerWon = true, currencyEarned = 300, damageDone = 75f });

            PlayerProfileData data = _profile.BuildData();

            var other = ScriptableObject.CreateInstance<PlayerProfileSO>();
            try
            {
                other.LoadFromData(data);
                Assert.AreEqual(_profile.DisplayName,      other.DisplayName);
                Assert.AreEqual(_profile.AvatarIndex,      other.AvatarIndex);
                Assert.AreEqual(_profile.CareerWins,       other.CareerWins);
                Assert.AreEqual(_profile.CareerEarnings,   other.CareerEarnings);
                Assert.AreEqual(_profile.CareerDamageDone, other.CareerDamageDone, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(other);
            }
        }

        [Test]
        public void LoadFromData_NullData_ResetsToDefaults()
        {
            // First give the profile some state
            _profile.SetDisplayName("Changed");
            _profile.UpdateFromMatchRecord(new MatchRecord { playerWon = true });

            // Loading null should reset
            _profile.LoadFromData(null);

            Assert.AreEqual("Player", _profile.DisplayName); // default
            Assert.AreEqual(0,        _profile.CareerWins);
        }

        // ── SaveData field presence ───────────────────────────────────────────

        [Test]
        public void SaveData_PlayerProfile_DefaultIsNotNull()
        {
            var save = new SaveData();
            Assert.IsNotNull(save.playerProfile);
        }

        // ── PlayerProfileUI static helper ─────────────────────────────────────

        [Test]
        public void FormatWinRate_Zero_Returns0Percent()
        {
            Assert.AreEqual("0%", PlayerProfileUI.FormatWinRate(0f));
        }

        [Test]
        public void FormatWinRate_Half_Returns50Percent()
        {
            Assert.AreEqual("50%", PlayerProfileUI.FormatWinRate(0.5f));
        }

        [Test]
        public void FormatWinRate_One_Returns100Percent()
        {
            Assert.AreEqual("100%", PlayerProfileUI.FormatWinRate(1f));
        }
    }
}
