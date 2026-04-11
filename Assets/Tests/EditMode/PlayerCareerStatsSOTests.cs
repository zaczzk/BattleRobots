using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PlayerCareerStatsSO"/>.
    ///
    /// Covers:
    ///   • Fresh-instance defaults (all properties zero).
    ///   • <see cref="PlayerCareerStatsSO.RecordMatch(float,float,int,float)"/>:
    ///       accumulation with positive values, zero-value no-op, negative-value clamp,
    ///       event firing, multi-call summation.
    ///   • <see cref="PlayerCareerStatsSO.RecordMatch(MatchRecord)"/>:
    ///       reads record fields correctly, null record no-throw (no event).
    ///   • <see cref="PlayerCareerStatsSO.LoadSnapshot"/>: sets values, clamps negatives,
    ///       does NOT fire event.
    ///   • <see cref="PlayerCareerStatsSO.PatchSaveData"/>: writes all four fields to
    ///       SaveData, null save no-throw.
    ///   • <see cref="PlayerCareerStatsSO.Reset"/>: clears all values, does NOT fire event.
    /// </summary>
    public class PlayerCareerStatsSOTests
    {
        private PlayerCareerStatsSO _so;
        private VoidGameEvent       _onUpdated;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private void WireEvent()
        {
            SetField(_so, "_onStatsUpdated", _onUpdated);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _so        = ScriptableObject.CreateInstance<PlayerCareerStatsSO>();
            _onUpdated = ScriptableObject.CreateInstance<VoidGameEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_so);
            Object.DestroyImmediate(_onUpdated);
            _so        = null;
            _onUpdated = null;
        }

        // ── Fresh instance ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_TotalDamageDealt_IsZero()
        {
            Assert.AreEqual(0f, _so.TotalDamageDealt);
        }

        [Test]
        public void FreshInstance_TotalDamageTaken_IsZero()
        {
            Assert.AreEqual(0f, _so.TotalDamageTaken);
        }

        [Test]
        public void FreshInstance_TotalCurrencyEarned_IsZero()
        {
            Assert.AreEqual(0, _so.TotalCurrencyEarned);
        }

        [Test]
        public void FreshInstance_TotalPlaytimeSeconds_IsZero()
        {
            Assert.AreEqual(0f, _so.TotalPlaytimeSeconds);
        }

        // ── RecordMatch(float, float, int, float) ─────────────────────────────

        [Test]
        public void RecordMatch_Float_AccumulatesAllFields()
        {
            _so.RecordMatch(100f, 50f, 200, 90f);

            Assert.AreEqual(100f, _so.TotalDamageDealt,    1e-4f);
            Assert.AreEqual(50f,  _so.TotalDamageTaken,    1e-4f);
            Assert.AreEqual(200,  _so.TotalCurrencyEarned);
            Assert.AreEqual(90f,  _so.TotalPlaytimeSeconds, 1e-4f);
        }

        [Test]
        public void RecordMatch_Float_CalledTwice_Sums()
        {
            _so.RecordMatch(100f, 40f, 200, 60f);
            _so.RecordMatch(50f,  10f, 100, 30f);

            Assert.AreEqual(150f, _so.TotalDamageDealt,    1e-4f);
            Assert.AreEqual(50f,  _so.TotalDamageTaken,    1e-4f);
            Assert.AreEqual(300,  _so.TotalCurrencyEarned);
            Assert.AreEqual(90f,  _so.TotalPlaytimeSeconds, 1e-4f);
        }

        [Test]
        public void RecordMatch_Float_ZeroValues_NoChange()
        {
            _so.RecordMatch(10f, 5f, 50, 30f);
            _so.RecordMatch(0f,  0f,  0,  0f);

            Assert.AreEqual(10f, _so.TotalDamageDealt,    1e-4f);
            Assert.AreEqual(5f,  _so.TotalDamageTaken,    1e-4f);
            Assert.AreEqual(50,  _so.TotalCurrencyEarned);
            Assert.AreEqual(30f, _so.TotalPlaytimeSeconds, 1e-4f);
        }

        [Test]
        public void RecordMatch_Float_NegativeValues_ClampedToZero()
        {
            _so.RecordMatch(-100f, -50f, -200, -90f);

            Assert.AreEqual(0f, _so.TotalDamageDealt);
            Assert.AreEqual(0f, _so.TotalDamageTaken);
            Assert.AreEqual(0,  _so.TotalCurrencyEarned);
            Assert.AreEqual(0f, _so.TotalPlaytimeSeconds);
        }

        [Test]
        public void RecordMatch_Float_FiresStatsUpdatedEvent()
        {
            WireEvent();
            int fireCount = 0;
            _onUpdated.RegisterCallback(() => fireCount++);

            _so.RecordMatch(10f, 5f, 50, 30f);

            Assert.AreEqual(1, fireCount, "RecordMatch() must raise _onStatsUpdated.");
        }

        [Test]
        public void RecordMatch_Float_NullEventWired_DoesNotThrow()
        {
            // _onStatsUpdated left null — must be null-safe.
            Assert.DoesNotThrow(() => _so.RecordMatch(10f, 5f, 50, 30f));
        }

        // ── RecordMatch(MatchRecord) ───────────────────────────────────────────

        [Test]
        public void RecordMatch_Record_AccumulatesFromRecord()
        {
            var record = new MatchRecord
            {
                damageDone      = 80f,
                damageTaken     = 30f,
                currencyEarned  = 150,
                durationSeconds = 75f,
            };

            _so.RecordMatch(record);

            Assert.AreEqual(80f,  _so.TotalDamageDealt,    1e-4f);
            Assert.AreEqual(30f,  _so.TotalDamageTaken,    1e-4f);
            Assert.AreEqual(150,  _so.TotalCurrencyEarned);
            Assert.AreEqual(75f,  _so.TotalPlaytimeSeconds, 1e-4f);
        }

        [Test]
        public void RecordMatch_Record_Null_DoesNotThrow_AndDoesNotFire()
        {
            WireEvent();
            int fireCount = 0;
            _onUpdated.RegisterCallback(() => fireCount++);

            Assert.DoesNotThrow(() => _so.RecordMatch((MatchRecord)null));
            Assert.AreEqual(0, fireCount, "Null record must not fire _onStatsUpdated.");
        }

        // ── LoadSnapshot() ────────────────────────────────────────────────────

        [Test]
        public void LoadSnapshot_SetsAllFields()
        {
            _so.LoadSnapshot(200f, 100f, 500, 300f);

            Assert.AreEqual(200f, _so.TotalDamageDealt,    1e-4f);
            Assert.AreEqual(100f, _so.TotalDamageTaken,    1e-4f);
            Assert.AreEqual(500,  _so.TotalCurrencyEarned);
            Assert.AreEqual(300f, _so.TotalPlaytimeSeconds, 1e-4f);
        }

        [Test]
        public void LoadSnapshot_NegativeValues_ClampedToZero()
        {
            _so.LoadSnapshot(-10f, -5f, -20, -30f);

            Assert.AreEqual(0f, _so.TotalDamageDealt);
            Assert.AreEqual(0f, _so.TotalDamageTaken);
            Assert.AreEqual(0,  _so.TotalCurrencyEarned);
            Assert.AreEqual(0f, _so.TotalPlaytimeSeconds);
        }

        [Test]
        public void LoadSnapshot_DoesNotFireStatsUpdatedEvent()
        {
            WireEvent();
            int fireCount = 0;
            _onUpdated.RegisterCallback(() => fireCount++);

            _so.LoadSnapshot(100f, 50f, 200, 120f);

            Assert.AreEqual(0, fireCount, "LoadSnapshot() must not fire _onStatsUpdated.");
        }

        // ── PatchSaveData() ───────────────────────────────────────────────────

        [Test]
        public void PatchSaveData_WritesAllCareerFields()
        {
            _so.RecordMatch(150f, 70f, 400, 180f);
            var save = new SaveData();

            _so.PatchSaveData(save);

            Assert.AreEqual(150f, save.careerDamageDealt,    1e-4f);
            Assert.AreEqual(70f,  save.careerDamageTaken,    1e-4f);
            Assert.AreEqual(400,  save.careerCurrencyEarned);
            Assert.AreEqual(180f, save.careerPlaytimeSeconds, 1e-4f);
        }

        [Test]
        public void PatchSaveData_NullSave_DoesNotThrow()
        {
            _so.RecordMatch(100f, 50f, 200, 60f);
            Assert.DoesNotThrow(() => _so.PatchSaveData(null));
        }

        // ── Reset() ───────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsAllFields()
        {
            _so.RecordMatch(100f, 50f, 200, 90f);
            _so.Reset();

            Assert.AreEqual(0f, _so.TotalDamageDealt);
            Assert.AreEqual(0f, _so.TotalDamageTaken);
            Assert.AreEqual(0,  _so.TotalCurrencyEarned);
            Assert.AreEqual(0f, _so.TotalPlaytimeSeconds);
        }

        [Test]
        public void Reset_DoesNotFireStatsUpdatedEvent()
        {
            WireEvent();
            int fireCount = 0;
            _onUpdated.RegisterCallback(() => fireCount++);

            _so.RecordMatch(100f, 50f, 200, 90f); // fireCount = 1
            _so.Reset();                           // must NOT fire again

            Assert.AreEqual(1, fireCount, "Reset() must not fire _onStatsUpdated.");
        }
    }
}
