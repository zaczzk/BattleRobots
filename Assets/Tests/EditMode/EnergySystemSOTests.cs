using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="EnergySystemSO"/>.
    ///
    /// Covers:
    ///   • Fresh-instance defaults (MaxEnergy=100, RechargeRate=10, EnergyRatio=1, CurrentEnergy=MaxEnergy).
    ///   • Consume: negative amount → false; insufficient energy → false; exact amount → zero;
    ///     reduces CurrentEnergy; fires _onEnergyChanged; fires _onEnergyDepleted on empty.
    ///   • Recharge: zero/negative delta → no-op; increases energy; caps at MaxEnergy;
    ///     fires _onEnergyFull on full transition.
    ///   • LoadSnapshot: clamps above max; clamps negative to zero.
    ///   • TakeSnapshot: returns CurrentEnergy.
    ///   • Reset: fills to MaxEnergy.
    /// </summary>
    public class EnergySystemSOTests
    {
        private EnergySystemSO _energy;

        [SetUp]
        public void SetUp()
        {
            _energy = ScriptableObject.CreateInstance<EnergySystemSO>();
            // OnEnable fires here — fills _currentEnergy to _maxEnergy (100f)
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_energy);
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_MaxEnergy_IsOneHundred()
        {
            Assert.AreEqual(100f, _energy.MaxEnergy,
                "Default MaxEnergy must be 100.");
        }

        [Test]
        public void FreshInstance_RechargeRate_IsTen()
        {
            Assert.AreEqual(10f, _energy.RechargeRate,
                "Default RechargeRate must be 10.");
        }

        [Test]
        public void FreshInstance_CurrentEnergy_EqualsMaxEnergy()
        {
            Assert.AreEqual(_energy.MaxEnergy, _energy.CurrentEnergy,
                "Fresh instance must start with CurrentEnergy == MaxEnergy (OnEnable fills to max).");
        }

        [Test]
        public void FreshInstance_EnergyRatio_IsOne()
        {
            Assert.AreEqual(1f, _energy.EnergyRatio,
                "Fresh instance EnergyRatio must be 1 (full pool).");
        }

        // ── Consume ───────────────────────────────────────────────────────────

        [Test]
        public void Consume_NegativeAmount_ReturnsFalse()
        {
            Assert.IsFalse(_energy.Consume(-1f),
                "Consume with a negative amount must return false.");
        }

        [Test]
        public void Consume_SufficientEnergy_ReturnsTrue()
        {
            Assert.IsTrue(_energy.Consume(50f),
                "Consume with sufficient energy must return true.");
        }

        [Test]
        public void Consume_InsufficientEnergy_ReturnsFalse()
        {
            Assert.IsFalse(_energy.Consume(101f),
                "Consume when energy < amount must return false.");
        }

        [Test]
        public void Consume_ExactAmount_CurrentEnergyBecomesZero()
        {
            _energy.Consume(_energy.MaxEnergy);
            Assert.AreEqual(0f, _energy.CurrentEnergy,
                "Consuming exactly MaxEnergy must leave CurrentEnergy at 0.");
        }

        [Test]
        public void Consume_ReducesCurrentEnergy()
        {
            float before = _energy.CurrentEnergy;
            _energy.Consume(30f);
            Assert.AreEqual(before - 30f, _energy.CurrentEnergy, 0.001f,
                "Consume must reduce CurrentEnergy by the consumed amount.");
        }

        [Test]
        public void Consume_FiresOnEnergyChanged()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int raised = 0;
            channel.RegisterCallback(() => raised++);

            // Inject via reflection
            typeof(EnergySystemSO)
                .GetField("_onEnergyChanged",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(_energy, channel);

            _energy.Consume(10f);

            Object.DestroyImmediate(channel);
            Assert.AreEqual(1, raised,
                "_onEnergyChanged must be raised once when Consume succeeds.");
        }

        [Test]
        public void Consume_ToZero_FiresOnEnergyDepleted()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int raised = 0;
            channel.RegisterCallback(() => raised++);

            typeof(EnergySystemSO)
                .GetField("_onEnergyDepleted",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(_energy, channel);

            _energy.Consume(_energy.MaxEnergy); // drains to 0

            Object.DestroyImmediate(channel);
            Assert.AreEqual(1, raised,
                "_onEnergyDepleted must be raised when CurrentEnergy reaches 0.");
        }

        // ── Recharge ─────────────────────────────────────────────────────────

        [Test]
        public void Recharge_ZeroDelta_IsNoOp()
        {
            _energy.Consume(50f); // reduce to 50
            float before = _energy.CurrentEnergy;
            _energy.Recharge(0f);
            Assert.AreEqual(before, _energy.CurrentEnergy,
                "Recharge(0) must not change CurrentEnergy.");
        }

        [Test]
        public void Recharge_NegativeDelta_IsNoOp()
        {
            _energy.Consume(50f);
            float before = _energy.CurrentEnergy;
            _energy.Recharge(-5f);
            Assert.AreEqual(before, _energy.CurrentEnergy,
                "Recharge with negative delta must not change CurrentEnergy.");
        }

        [Test]
        public void Recharge_IncreasesCurrentEnergy()
        {
            _energy.Consume(50f); // now at 50
            _energy.Recharge(1f); // rate=10 → adds 10 → 60
            Assert.Greater(_energy.CurrentEnergy, 50f,
                "Recharge with positive delta must increase CurrentEnergy.");
        }

        [Test]
        public void Recharge_CapsAtMaxEnergy()
        {
            _energy.Consume(5f); // 95
            _energy.Recharge(100f); // would add 1000 — must cap at 100
            Assert.AreEqual(_energy.MaxEnergy, _energy.CurrentEnergy, 0.001f,
                "Recharge must cap CurrentEnergy at MaxEnergy.");
        }

        [Test]
        public void Recharge_FullTransition_FiresOnEnergyFull()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int raised = 0;
            channel.RegisterCallback(() => raised++);

            typeof(EnergySystemSO)
                .GetField("_onEnergyFull",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(_energy, channel);

            _energy.Consume(10f);  // 90 / 100
            _energy.Recharge(100f); // fills to 100 → should raise _onEnergyFull

            Object.DestroyImmediate(channel);
            Assert.AreEqual(1, raised,
                "_onEnergyFull must be raised exactly once when Recharge fills the pool.");
        }

        // ── LoadSnapshot ─────────────────────────────────────────────────────

        [Test]
        public void LoadSnapshot_AboveMax_ClampsToMax()
        {
            _energy.LoadSnapshot(999f);
            Assert.AreEqual(_energy.MaxEnergy, _energy.CurrentEnergy, 0.001f,
                "LoadSnapshot must clamp values above MaxEnergy to MaxEnergy.");
        }

        [Test]
        public void LoadSnapshot_Negative_ClampsToZero()
        {
            _energy.LoadSnapshot(-10f);
            Assert.AreEqual(0f, _energy.CurrentEnergy, 0.001f,
                "LoadSnapshot must clamp negative values to 0.");
        }

        // ── TakeSnapshot ─────────────────────────────────────────────────────

        [Test]
        public void TakeSnapshot_ReturnsCurrentEnergy()
        {
            _energy.Consume(35f);
            Assert.AreEqual(_energy.CurrentEnergy, _energy.TakeSnapshot(), 0.001f,
                "TakeSnapshot must return the current energy level.");
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_FillsCurrentEnergyToMax()
        {
            _energy.Consume(80f); // deplete
            _energy.Reset();
            Assert.AreEqual(_energy.MaxEnergy, _energy.CurrentEnergy, 0.001f,
                "Reset must restore CurrentEnergy to MaxEnergy.");
        }
    }
}
