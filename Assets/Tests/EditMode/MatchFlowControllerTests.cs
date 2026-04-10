using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchFlowController"/>.
    ///
    /// Covers:
    ///   • HandleMatchStarted — assembler routing: Assemble() is called for each
    ///     non-null RobotAssembler in the list; null entries are silently skipped.
    ///   • HandleMatchStarted — AI targeting: SetTarget(_playerRobotRoot) is applied
    ///     to each AI controller; skipped when _playerRobotRoot is null.
    ///   • HandleMatchStarted — CameraRig null safety: no throw when _cameraRig unset.
    ///   • HandleMatchEnded — AI disable: CurrentState transitions to Idle for each
    ///     controller; null entries skipped gracefully.
    ///   • HandleMatchEnded — locomotion halt: Halt() clears MoveInput / TurnInput;
    ///     ArticulationBody is a data container in EditMode and does not throw.
    ///   • SO event channel wiring: raising _matchStartedEvent via VoidGameEvent
    ///     triggers HandleMatchStarted and assembles robots.
    ///
    /// HandleMatchStarted and HandleMatchEnded are private methods; they are invoked
    /// via reflection using the same pattern as MatchManagerTests and PauseManagerTests.
    /// For the SO channel subscription test, the GO is created inactive so the event
    /// field can be wired before OnEnable registers the callback.
    ///
    /// RobotAssembler.Assemble() requires a non-null RobotDefinition to set
    /// IsAssembled = true; MakeAssembler() injects a minimal one automatically.
    /// </summary>
    public class MatchFlowControllerTests
    {
        // ── Scene objects ─────────────────────────────────────────────────────
        private GameObject          _go;
        private MatchFlowController _controller;

        // Tracked disposable assets — destroyed in TearDown.
        private readonly List<GameObject>       _extraGOs = new List<GameObject>();
        private readonly List<ScriptableObject> _extraSOs = new List<ScriptableObject>();

        // ── Reflection helpers ────────────────────────────────────────────────

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static T GetField<T>(object target, string fieldName)
        {
            FieldInfo fi = target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            return (T)fi.GetValue(target);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(mi, $"Method '{methodName}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go         = new GameObject("TestMatchFlowController");
            _controller = _go.AddComponent<MatchFlowController>();
            _extraGOs.Clear();
            _extraSOs.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            foreach (var obj in _extraGOs)
                if (obj != null) Object.DestroyImmediate(obj);
            foreach (var so in _extraSOs)
                if (so != null) Object.DestroyImmediate(so);
            _extraGOs.Clear();
            _extraSOs.Clear();
            _go         = null;
            _controller = null;
        }

        // ── Helper factories ──────────────────────────────────────────────────

        /// <summary>Creates a tracked disposable GameObject.</summary>
        private GameObject MakeGO(string name = "GO")
        {
            var go = new GameObject(name);
            _extraGOs.Add(go);
            return go;
        }

        /// <summary>
        /// Creates a RobotAssembler with a minimal RobotDefinition (empty slots)
        /// so Assemble() can run to completion and set IsAssembled = true.
        /// </summary>
        private RobotAssembler MakeAssembler(string name = "Assembler")
        {
            var go       = MakeGO(name);
            var assembler = go.AddComponent<RobotAssembler>();
            var robotDef  = ScriptableObject.CreateInstance<RobotDefinition>();
            _extraSOs.Add(robotDef);
            SetField(assembler, "_robotDefinition", robotDef);
            return assembler;
        }

        // ── HandleMatchStarted — Assembler routing ────────────────────────────

        [Test]
        public void HandleMatchStarted_EmptyAssemblerList_DoesNotThrow()
        {
            SetField(_controller, "_assemblers", new List<RobotAssembler>());

            Assert.DoesNotThrow(() => InvokePrivate(_controller, "HandleMatchStarted"));
        }

        [Test]
        public void HandleMatchStarted_SingleAssembler_IsAssembled()
        {
            var assembler = MakeAssembler();
            SetField(_controller, "_assemblers", new List<RobotAssembler> { assembler });

            InvokePrivate(_controller, "HandleMatchStarted");

            Assert.IsTrue(assembler.IsAssembled,
                "HandleMatchStarted must call Assemble() on each non-null RobotAssembler in the list.");
        }

        [Test]
        public void HandleMatchStarted_TwoAssemblers_BothAssembled()
        {
            var a1 = MakeAssembler("Assembler1");
            var a2 = MakeAssembler("Assembler2");
            SetField(_controller, "_assemblers", new List<RobotAssembler> { a1, a2 });

            InvokePrivate(_controller, "HandleMatchStarted");

            Assert.IsTrue(a1.IsAssembled, "First assembler must be assembled.");
            Assert.IsTrue(a2.IsAssembled, "Second assembler must be assembled.");
        }

        [Test]
        public void HandleMatchStarted_NullAssemblerInList_IsSkipped()
        {
            var a2 = MakeAssembler("Assembler2");

            // First entry is null — must be skipped; second must still be assembled.
            SetField(_controller, "_assemblers", new List<RobotAssembler> { null, a2 });

            Assert.DoesNotThrow(() => InvokePrivate(_controller, "HandleMatchStarted"));
            Assert.IsTrue(a2.IsAssembled,
                "Non-null assembler after a null list entry must still be assembled.");
        }

        // ── HandleMatchStarted — AI targeting ─────────────────────────────────

        [Test]
        public void HandleMatchStarted_SingleAIController_SetsPlayerRootAsTarget()
        {
            var playerRoot = MakeGO("PlayerRoot").transform;
            var ai         = MakeGO("AI").AddComponent<RobotAIController>();

            SetField(_controller, "_playerRobotRoot", playerRoot);
            SetField(_controller, "_aiControllers", new List<RobotAIController> { ai });

            InvokePrivate(_controller, "HandleMatchStarted");

            // Read private _target field to verify SetTarget() was called.
            var assigned = GetField<Transform>(ai, "_target");
            Assert.AreEqual(playerRoot, assigned,
                "SetTarget(_playerRobotRoot) must be called on each AI controller at match start.");
        }

        [Test]
        public void HandleMatchStarted_NullAIInList_IsSkipped()
        {
            var playerRoot = MakeGO("PlayerRoot").transform;
            var ai         = MakeGO("AI").AddComponent<RobotAIController>();

            SetField(_controller, "_playerRobotRoot", playerRoot);
            SetField(_controller, "_aiControllers", new List<RobotAIController> { null, ai });

            Assert.DoesNotThrow(() => InvokePrivate(_controller, "HandleMatchStarted"));

            var assigned = GetField<Transform>(ai, "_target");
            Assert.AreEqual(playerRoot, assigned,
                "Non-null AI after a null list entry must still receive the player target.");
        }

        [Test]
        public void HandleMatchStarted_NullPlayerRoot_SkipsAITargeting()
        {
            var ai = MakeGO("AI").AddComponent<RobotAIController>();

            SetField(_controller, "_playerRobotRoot", null);
            SetField(_controller, "_aiControllers", new List<RobotAIController> { ai });

            // Must not throw; AI _target must remain null (SetTarget was not called).
            Assert.DoesNotThrow(() => InvokePrivate(_controller, "HandleMatchStarted"));

            var assigned = GetField<Transform>(ai, "_target");
            Assert.IsNull(assigned,
                "When _playerRobotRoot is null, SetTarget must not be called on AI controllers.");
        }

        // ── HandleMatchStarted — CameraRig null safety ────────────────────────

        [Test]
        public void HandleMatchStarted_NullCameraRig_DoesNotThrow()
        {
            // _cameraRig defaults to null; no assemblers or AI controllers needed.
            Assert.DoesNotThrow(() => InvokePrivate(_controller, "HandleMatchStarted"));
        }

        // ── HandleMatchEnded — AI disable ─────────────────────────────────────

        [Test]
        public void HandleMatchEnded_SingleAIController_StateIsIdle()
        {
            var ai = MakeGO("AI").AddComponent<RobotAIController>();

            // Pre-set to a non-Idle state so we can confirm Disable() changed it.
            SetField(ai, "_state", AIState.Chase);
            Assert.AreEqual(AIState.Chase, ai.CurrentState);

            SetField(_controller, "_aiControllers", new List<RobotAIController> { ai });

            InvokePrivate(_controller, "HandleMatchEnded");

            Assert.AreEqual(AIState.Idle, ai.CurrentState,
                "Disable() must reset the AI FSM state to Idle.");
        }

        [Test]
        public void HandleMatchEnded_TwoAIControllers_BothIdle()
        {
            var ai1 = MakeGO("AI1").AddComponent<RobotAIController>();
            var ai2 = MakeGO("AI2").AddComponent<RobotAIController>();
            SetField(ai1, "_state", AIState.Chase);
            SetField(ai2, "_state", AIState.Attack);

            SetField(_controller, "_aiControllers", new List<RobotAIController> { ai1, ai2 });

            InvokePrivate(_controller, "HandleMatchEnded");

            Assert.AreEqual(AIState.Idle, ai1.CurrentState, "AI1 must be set to Idle.");
            Assert.AreEqual(AIState.Idle, ai2.CurrentState, "AI2 must be set to Idle.");
        }

        [Test]
        public void HandleMatchEnded_NullAIInList_IsSkipped()
        {
            var ai2 = MakeGO("AI2").AddComponent<RobotAIController>();
            SetField(ai2, "_state", AIState.Chase);

            // First entry null — must be skipped; second AI must still be disabled.
            SetField(_controller, "_aiControllers", new List<RobotAIController> { null, ai2 });

            Assert.DoesNotThrow(() => InvokePrivate(_controller, "HandleMatchEnded"));
            Assert.AreEqual(AIState.Idle, ai2.CurrentState,
                "Second AI must be disabled even when the first list entry is null.");
        }

        // ── HandleMatchEnded — Locomotion halt ────────────────────────────────

        [Test]
        public void HandleMatchEnded_LocoControllerPresent_InputsZeroedAfterHalt()
        {
            // [RequireComponent(typeof(ArticulationBody))] is auto-satisfied in EditMode.
            // ArticulationBody acts as a data container; setting velocity does not throw.
            var locoGo = MakeGO("Loco");
            var loco   = locoGo.AddComponent<RobotLocomotionController>();

            // Set non-zero inputs so we can verify Halt() zeroed them.
            loco.SetInputs(1f, 1f);
            Assert.AreEqual(1f, loco.MoveInput, 0.001f);

            SetField(_controller, "_locomotionControllers",
                new List<RobotLocomotionController> { loco });

            Assert.DoesNotThrow(() => InvokePrivate(_controller, "HandleMatchEnded"));

            Assert.AreEqual(0f, loco.MoveInput, 0.001f, "Halt() must zero MoveInput.");
            Assert.AreEqual(0f, loco.TurnInput, 0.001f, "Halt() must zero TurnInput.");
        }

        // ── SO event channel wiring ───────────────────────────────────────────

        [Test]
        public void MatchStartedSOChannel_WhenRaised_TriggersAssembly()
        {
            // Recreate GO as inactive so OnEnable does not fire during AddComponent.
            // This lets us wire _matchStartedEvent before the subscription is registered.
            Object.DestroyImmediate(_go);
            _go         = new GameObject("TestMFCEvent");
            _go.SetActive(false);
            _controller = _go.AddComponent<MatchFlowController>(); // Awake runs; OnEnable deferred.

            var matchStarted = ScriptableObject.CreateInstance<VoidGameEvent>();
            _extraSOs.Add(matchStarted);

            var assembler = MakeAssembler("Assembler");

            SetField(_controller, "_matchStartedEvent", matchStarted);
            SetField(_controller, "_assemblers", new List<RobotAssembler> { assembler });

            _go.SetActive(true); // OnEnable → RegisterCallback on matchStarted

            matchStarted.Raise();

            Assert.IsTrue(assembler.IsAssembled,
                "Raising the MatchStarted SO channel must invoke HandleMatchStarted and assemble robots.");
        }
    }
}
