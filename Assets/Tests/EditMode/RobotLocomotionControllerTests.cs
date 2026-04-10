using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="RobotLocomotionController"/>.
    ///
    /// Covers:
    ///   • SetInputs — values stored in MoveInput / TurnInput public fields;
    ///     inputs clamped to [−1, 1].
    ///   • Halt — MoveInput and TurnInput zeroed; does not throw in EditMode
    ///     (ArticulationBody.linearVelocity / angularVelocity writes are safe
    ///     even without PhysX ticking — confirmed by existing MatchFlowControllerTests).
    ///   • SetBaseSpeed — runtime speed override stored and returned by BaseSpeed;
    ///     negative / zero input clamped to 0.01; multiple calls replace (not compound).
    ///   • BaseSpeed — returns inspector default (5 m/s) when no override has been set.
    ///   • SetSpeedMultiplier — fractional difficulty multiplier stored in private
    ///     _speedMultiplier field; negative input clamped to 0.01.
    ///
    /// <c>RobotLocomotionController</c> has [RequireComponent(ArticulationBody)].
    /// Unity auto-adds ArticulationBody on AddComponent; Awake calls
    /// GetComponent&lt;ArticulationBody&gt;() and caches it in _rootBody.
    /// In EditMode the ArticulationBody is a data-container and does not simulate
    /// physics, so velocity writes in Halt() do not throw.
    /// </summary>
    public class RobotLocomotionControllerTests
    {
        private GameObject                _go;
        private RobotLocomotionController _loco;

        [SetUp]
        public void SetUp()
        {
            _go   = new GameObject("TestLoco");
            _loco = _go.AddComponent<RobotLocomotionController>();
            // RequireComponent auto-adds ArticulationBody.
            // Awake: _rootBody = GetComponent<ArticulationBody>().
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            _go   = null;
            _loco = null;
        }

        // ── SetInputs — value storage ──────────────────────────────────────────

        [Test]
        public void SetInputs_StoresMoveAndTurnValues()
        {
            _loco.SetInputs(0.5f, -0.3f);

            Assert.AreEqual(0.5f,  _loco.MoveInput, 1e-6f, "MoveInput must match supplied value.");
            Assert.AreEqual(-0.3f, _loco.TurnInput,  1e-6f, "TurnInput must match supplied value.");
        }

        // ── SetInputs — clamping ───────────────────────────────────────────────

        [Test]
        public void SetInputs_ClampsMoveAboveOne()
        {
            _loco.SetInputs(5f, 0f);
            Assert.AreEqual(1f, _loco.MoveInput, 1e-6f, "MoveInput > 1 must be clamped to 1.");
        }

        [Test]
        public void SetInputs_ClampsMoveBelowNegativeOne()
        {
            _loco.SetInputs(-5f, 0f);
            Assert.AreEqual(-1f, _loco.MoveInput, 1e-6f, "MoveInput < -1 must be clamped to -1.");
        }

        [Test]
        public void SetInputs_ClampsTurnAboveOne()
        {
            _loco.SetInputs(0f, 3f);
            Assert.AreEqual(1f, _loco.TurnInput, 1e-6f, "TurnInput > 1 must be clamped to 1.");
        }

        [Test]
        public void SetInputs_ClampsTurnBelowNegativeOne()
        {
            _loco.SetInputs(0f, -3f);
            Assert.AreEqual(-1f, _loco.TurnInput, 1e-6f, "TurnInput < -1 must be clamped to -1.");
        }

        // ── Halt ──────────────────────────────────────────────────────────────

        [Test]
        public void Halt_ZerosMoveInput()
        {
            _loco.SetInputs(1f, 1f);
            _loco.Halt();
            Assert.AreEqual(0f, _loco.MoveInput, 1e-6f, "Halt must zero MoveInput.");
        }

        [Test]
        public void Halt_ZerosTurnInput()
        {
            _loco.SetInputs(1f, 1f);
            _loco.Halt();
            Assert.AreEqual(0f, _loco.TurnInput, 1e-6f, "Halt must zero TurnInput.");
        }

        // ── SetBaseSpeed ───────────────────────────────────────────────────────

        [Test]
        public void SetBaseSpeed_StoresValue()
        {
            _loco.SetBaseSpeed(7f);
            Assert.AreEqual(7f, _loco.BaseSpeed, 1e-6f,
                "BaseSpeed must return the value passed to SetBaseSpeed.");
        }

        [Test]
        public void SetBaseSpeed_ClampsNegativeToMin()
        {
            _loco.SetBaseSpeed(-10f);
            Assert.AreEqual(0.01f, _loco.BaseSpeed, 1e-6f,
                "SetBaseSpeed must clamp negative values to 0.01.");
        }

        [Test]
        public void SetBaseSpeed_ClampsZeroToMin()
        {
            _loco.SetBaseSpeed(0f);
            Assert.AreEqual(0.01f, _loco.BaseSpeed, 1e-6f,
                "SetBaseSpeed must clamp zero to 0.01 to preserve minimum locomotion.");
        }

        [Test]
        public void SetBaseSpeed_CalledTwice_LastValueWins()
        {
            _loco.SetBaseSpeed(3f);
            _loco.SetBaseSpeed(9f);
            Assert.AreEqual(9f, _loco.BaseSpeed, 1e-6f,
                "SetBaseSpeed must replace (not compound) the stored override speed.");
        }

        [Test]
        public void BaseSpeed_WithoutOverride_ReturnsInspectorDefault()
        {
            // _runtimeMoveSpeed initialises to -1 (no override).
            // EffectiveMoveSpeed returns _moveSpeed inspector default (5f).
            Assert.AreEqual(5f, _loco.BaseSpeed, 1e-6f,
                "BaseSpeed without an override must equal the inspector default _moveSpeed (5).");
        }

        // ── SetSpeedMultiplier ─────────────────────────────────────────────────

        [Test]
        public void SetSpeedMultiplier_StoresValue()
        {
            _loco.SetSpeedMultiplier(1.5f);

            FieldInfo fi = typeof(RobotLocomotionController)
                .GetField("_speedMultiplier", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, "_speedMultiplier field not found on RobotLocomotionController.");

            float stored = (float)fi.GetValue(_loco);
            Assert.AreEqual(1.5f, stored, 1e-6f,
                "SetSpeedMultiplier must store the supplied multiplier in _speedMultiplier.");
        }

        [Test]
        public void SetSpeedMultiplier_ClampsNegativeToMin()
        {
            _loco.SetSpeedMultiplier(-1f);

            FieldInfo fi = typeof(RobotLocomotionController)
                .GetField("_speedMultiplier", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, "_speedMultiplier field not found on RobotLocomotionController.");

            float stored = (float)fi.GetValue(_loco);
            Assert.AreEqual(0.01f, stored, 1e-6f,
                "SetSpeedMultiplier must clamp negative input to 0.01.");
        }
    }
}
