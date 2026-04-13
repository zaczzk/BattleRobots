using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="WaveManagerSO"/>.
    ///
    /// Covers:
    ///   • Fresh-instance defaults (IsActive, CurrentWave, BestWave are all 0/false).
    ///   • StartSurvival null config — no-op.
    ///   • StartSurvival sets IsActive, CurrentWave=1, BotsRemainingInWave from config.
    ///   • StartSurvival raises _onWaveStarted.
    ///   • StartNextWave when inactive — no-op.
    ///   • StartNextWave increments CurrentWave and sets bots from config.
    ///   • StartNextWave raises _onWaveStarted.
    ///   • RecordBotDefeated when inactive — no-op.
    ///   • RecordBotDefeated decrements BotsRemainingInWave.
    ///   • RecordBotDefeated when last bot — fires _onWaveCompleted.
    ///   • RecordBotDefeated when last bot — updates BestWave.
    ///   • EndSurvival sets IsActive false and raises _onSurvivalEnded.
    ///   • EndSurvival when already inactive — no-op (no double fire).
    ///   • LoadSnapshot sets BestWave; negative clamped to 0.
    ///   • Reset clears runtime state; BestWave unchanged.
    /// </summary>
    public class WaveManagerSOTests
    {
        private WaveManagerSO _manager;
        private WaveConfigSO  _config;

        [SetUp]
        public void SetUp()
        {
            _manager = ScriptableObject.CreateInstance<WaveManagerSO>();
            _config  = ScriptableObject.CreateInstance<WaveConfigSO>();
            // default config: base=1, increment=1, max=10
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_manager);
            Object.DestroyImmediate(_config);
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_IsNotActive()
        {
            Assert.IsFalse(_manager.IsActive,
                "Fresh WaveManagerSO must not be active.");
        }

        [Test]
        public void FreshInstance_CurrentWave_IsZero()
        {
            Assert.AreEqual(0, _manager.CurrentWave,
                "Fresh WaveManagerSO.CurrentWave must be 0.");
        }

        [Test]
        public void FreshInstance_BestWave_IsZero()
        {
            Assert.AreEqual(0, _manager.BestWave,
                "Fresh WaveManagerSO.BestWave must be 0.");
        }

        // ── StartSurvival ─────────────────────────────────────────────────────

        [Test]
        public void StartSurvival_NullConfig_IsNoOp()
        {
            Assert.DoesNotThrow(() => _manager.StartSurvival(null),
                "StartSurvival(null) must not throw.");
            Assert.IsFalse(_manager.IsActive,
                "IsActive must remain false after StartSurvival(null).");
        }

        [Test]
        public void StartSurvival_SetsIsActive_True()
        {
            _manager.StartSurvival(_config);
            Assert.IsTrue(_manager.IsActive,
                "IsActive must be true after StartSurvival.");
        }

        [Test]
        public void StartSurvival_SetsCurrentWave_ToOne()
        {
            _manager.StartSurvival(_config);
            Assert.AreEqual(1, _manager.CurrentWave,
                "CurrentWave must be 1 immediately after StartSurvival.");
        }

        [Test]
        public void StartSurvival_SetsBotsRemainingInWave_FromConfig()
        {
            _manager.StartSurvival(_config);
            int expected = _config.GetBotsForWave(1);
            Assert.AreEqual(expected, _manager.BotsRemainingInWave,
                "BotsRemainingInWave must equal config.GetBotsForWave(1).");
        }

        [Test]
        public void StartSurvival_RaisesOnWaveStarted()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int raised  = 0;
            channel.RegisterCallback(() => raised++);
            SetField(_manager, "_onWaveStarted", channel);

            _manager.StartSurvival(_config);

            Object.DestroyImmediate(channel);
            Assert.AreEqual(1, raised,
                "_onWaveStarted must be raised exactly once by StartSurvival.");
        }

        // ── StartNextWave ─────────────────────────────────────────────────────

        [Test]
        public void StartNextWave_WhenInactive_IsNoOp()
        {
            // not started — IsActive is false
            Assert.DoesNotThrow(() => _manager.StartNextWave(_config));
            Assert.AreEqual(0, _manager.CurrentWave,
                "CurrentWave must remain 0 when StartNextWave called while inactive.");
        }

        [Test]
        public void StartNextWave_IncrementsCurrentWave()
        {
            _manager.StartSurvival(_config);
            _manager.StartNextWave(_config);
            Assert.AreEqual(2, _manager.CurrentWave,
                "CurrentWave must be 2 after StartSurvival + StartNextWave.");
        }

        [Test]
        public void StartNextWave_SetsBotsFromConfig()
        {
            _manager.StartSurvival(_config);
            _manager.StartNextWave(_config);
            int expected = _config.GetBotsForWave(2);
            Assert.AreEqual(expected, _manager.BotsRemainingInWave,
                "BotsRemainingInWave for wave 2 must equal config.GetBotsForWave(2).");
        }

        [Test]
        public void StartNextWave_RaisesOnWaveStarted()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int raised  = 0;
            channel.RegisterCallback(() => raised++);
            SetField(_manager, "_onWaveStarted", channel);

            _manager.StartSurvival(_config); // raises once (wave 1)
            raised = 0;                      // reset counter
            _manager.StartNextWave(_config); // should raise again

            Object.DestroyImmediate(channel);
            Assert.AreEqual(1, raised,
                "_onWaveStarted must be raised once by StartNextWave.");
        }

        // ── RecordBotDefeated ─────────────────────────────────────────────────

        [Test]
        public void RecordBotDefeated_WhenInactive_IsNoOp()
        {
            Assert.DoesNotThrow(() => _manager.RecordBotDefeated(),
                "RecordBotDefeated when inactive must not throw.");
            Assert.AreEqual(0, _manager.TotalBotsDefeated,
                "TotalBotsDefeated must remain 0 when RecordBotDefeated called while inactive.");
        }

        [Test]
        public void RecordBotDefeated_DecrementsBotsRemaining()
        {
            _manager.StartSurvival(_config); // wave 1 = 1 bot (default config)
            // Restore to 3 bots via another wave with a 3-bot config
            var config3 = ScriptableObject.CreateInstance<WaveConfigSO>();
            // base=3, so wave 1 = 3 bots — inject via reflection
            FieldInfo fi = config3.GetType()
                .GetField("_baseBotsPerWave", BindingFlags.Instance | BindingFlags.NonPublic);
            fi.SetValue(config3, 3);
            _manager.StartSurvival(config3);

            int before = _manager.BotsRemainingInWave;
            _manager.RecordBotDefeated();

            Object.DestroyImmediate(config3);
            Assert.AreEqual(before - 1, _manager.BotsRemainingInWave,
                "BotsRemainingInWave must decrement by 1 per RecordBotDefeated.");
        }

        [Test]
        public void RecordBotDefeated_WhenLastBot_FiresOnWaveCompleted()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int raised  = 0;
            channel.RegisterCallback(() => raised++);
            SetField(_manager, "_onWaveCompleted", channel);

            _manager.StartSurvival(_config); // wave 1 = 1 bot

            _manager.RecordBotDefeated(); // last bot

            Object.DestroyImmediate(channel);
            Assert.AreEqual(1, raised,
                "_onWaveCompleted must fire when the last bot in the wave is defeated.");
        }

        [Test]
        public void RecordBotDefeated_WhenLastBot_UpdatesBestWave()
        {
            _manager.StartSurvival(_config); // wave 1 = 1 bot
            _manager.RecordBotDefeated();    // completes wave 1

            Assert.AreEqual(1, _manager.BestWave,
                "BestWave must update to 1 after completing wave 1.");
        }

        // ── EndSurvival ───────────────────────────────────────────────────────

        [Test]
        public void EndSurvival_SetsIsActive_False()
        {
            _manager.StartSurvival(_config);
            _manager.EndSurvival();
            Assert.IsFalse(_manager.IsActive,
                "IsActive must be false after EndSurvival.");
        }

        [Test]
        public void EndSurvival_RaisesOnSurvivalEnded()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int raised  = 0;
            channel.RegisterCallback(() => raised++);
            SetField(_manager, "_onSurvivalEnded", channel);

            _manager.StartSurvival(_config);
            _manager.EndSurvival();

            Object.DestroyImmediate(channel);
            Assert.AreEqual(1, raised,
                "_onSurvivalEnded must fire exactly once when EndSurvival is called.");
        }

        [Test]
        public void EndSurvival_WhenInactive_IsNoOp()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int raised  = 0;
            channel.RegisterCallback(() => raised++);
            SetField(_manager, "_onSurvivalEnded", channel);

            _manager.EndSurvival(); // already inactive — must be no-op

            Object.DestroyImmediate(channel);
            Assert.AreEqual(0, raised,
                "_onSurvivalEnded must NOT fire when EndSurvival is called while inactive.");
        }

        // ── LoadSnapshot ──────────────────────────────────────────────────────

        [Test]
        public void LoadSnapshot_SetsBestWave()
        {
            _manager.LoadSnapshot(7);
            Assert.AreEqual(7, _manager.BestWave,
                "LoadSnapshot(7) must set BestWave to 7.");
        }

        [Test]
        public void LoadSnapshot_NegativeValue_ClampedToZero()
        {
            _manager.LoadSnapshot(-3);
            Assert.AreEqual(0, _manager.BestWave,
                "LoadSnapshot with negative value must clamp BestWave to 0.");
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsRuntimeState_BestWaveUnchanged()
        {
            _manager.LoadSnapshot(5); // BestWave = 5
            _manager.StartSurvival(_config);
            _manager.Reset();

            Assert.IsFalse(_manager.IsActive,    "IsActive must be false after Reset.");
            Assert.AreEqual(0, _manager.CurrentWave, "CurrentWave must be 0 after Reset.");
            Assert.AreEqual(5, _manager.BestWave,    "BestWave must be unchanged after Reset.");
        }
    }
}
