using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T238:
    ///   <see cref="RespawnZoneRotatorSO"/> and
    ///   <see cref="RespawnZoneRotatorController"/>.
    ///
    /// RespawnZoneRotatorSOTests (6):
    ///   FreshInstance_DefaultSelectionModeRoundRobin    ×1
    ///   FreshInstance_DefaultAnchorRadius_IsOne         ×1
    ///   SelectionMode_CanBeRoundRobin                   ×1
    ///   SelectionMode_CanBeRandom                       ×1
    ///   AnchorRadius_LargeValue_StoredCorrectly         ×1
    ///   AnchorSelectionMode_RandomEnumValue_IsOne       ×1
    ///
    /// RespawnZoneRotatorControllerTests (8):
    ///   FreshInstance_ConfigNull                        ×1
    ///   FreshInstance_RobotTransformNull                ×1
    ///   FreshInstance_CurrentIndexZero                  ×1
    ///   OnEnable_NullRefs_DoesNotThrow                  ×1
    ///   OnDisable_NullRefs_DoesNotThrow                 ×1
    ///   OnDisable_Unregisters                           ×1
    ///   SelectNext_NullRobot_NoThrow                    ×1
    ///   SelectNext_RoundRobin_AdvancesIndex             ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class RespawnZoneRotatorTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static VoidGameEvent CreateVoidEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static RespawnZoneRotatorSO CreateRotatorSO() =>
            ScriptableObject.CreateInstance<RespawnZoneRotatorSO>();

        private static RespawnZoneRotatorController CreateController() =>
            new GameObject("RespawnRotator_Test").AddComponent<RespawnZoneRotatorController>();

        // ── RespawnZoneRotatorSOTests ─────────────────────────────────────────

        [Test]
        public void FreshInstance_DefaultSelectionModeRoundRobin()
        {
            var so = CreateRotatorSO();
            Assert.AreEqual(AnchorSelectionMode.RoundRobin, so.SelectionMode,
                "SelectionMode must default to RoundRobin on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_DefaultAnchorRadius_IsOne()
        {
            var so = CreateRotatorSO();
            Assert.AreEqual(1f, so.AnchorRadius, 0.001f,
                "AnchorRadius must default to 1 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SelectionMode_CanBeRoundRobin()
        {
            var so = CreateRotatorSO();
            SetField(so, "_selectionMode", AnchorSelectionMode.RoundRobin);
            Assert.AreEqual(AnchorSelectionMode.RoundRobin, so.SelectionMode,
                "SelectionMode must return RoundRobin when configured as such.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SelectionMode_CanBeRandom()
        {
            var so = CreateRotatorSO();
            SetField(so, "_selectionMode", AnchorSelectionMode.Random);
            Assert.AreEqual(AnchorSelectionMode.Random, so.SelectionMode,
                "SelectionMode must return Random when configured as such.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AnchorRadius_LargeValue_StoredCorrectly()
        {
            var so = CreateRotatorSO();
            SetField(so, "_anchorRadius", 25f);
            Assert.AreEqual(25f, so.AnchorRadius, 0.001f,
                "AnchorRadius must store and return a large float value correctly.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AnchorSelectionMode_RandomEnumValue_IsOne()
        {
            Assert.AreEqual(1, (int)AnchorSelectionMode.Random,
                "AnchorSelectionMode.Random must have integer value 1.");
        }

        // ── RespawnZoneRotatorControllerTests ─────────────────────────────────

        [Test]
        public void FreshInstance_ConfigNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Config,
                "Config must be null on a fresh instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_RobotTransformNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.RobotTransform,
                "RobotTransform must be null on a fresh instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_CurrentIndexZero()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.AreEqual(0, ctrl.CurrentIndex,
                "CurrentIndex must be 0 after Awake.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with all-null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with all-null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateVoidEvent();
            SetField(ctrl, "_onRespawnReady", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            ch.Raise();

            Assert.AreEqual(1, count,
                "After OnDisable only the manually registered callback must fire.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void SelectNext_NullRobot_NoThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            // _robotTransform remains null

            Assert.DoesNotThrow(() => ctrl.SelectNext(),
                "SelectNext with null RobotTransform must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void SelectNext_RoundRobin_AdvancesIndex()
        {
            var ctrl = CreateController();

            // Set up three anchor Transforms at distinct positions.
            var anchorA = new GameObject("AnchorA");
            var anchorB = new GameObject("AnchorB");
            var anchorC = new GameObject("AnchorC");
            anchorA.transform.position = new Vector3(1f, 0f, 0f);
            anchorB.transform.position = new Vector3(2f, 0f, 0f);
            anchorC.transform.position = new Vector3(3f, 0f, 0f);

            var robot = new GameObject("Robot");

            SetField(ctrl, "_respawnAnchors", new Transform[]
            {
                anchorA.transform,
                anchorB.transform,
                anchorC.transform,
            });
            SetField(ctrl, "_robotTransform", robot.transform);
            // Leave _config null → defaults to RoundRobin.
            InvokePrivate(ctrl, "Awake"); // sets _currentIndex = 0

            ctrl.SelectNext(); // uses anchor[0], advances to 1
            Assert.AreEqual(anchorA.transform.position, robot.transform.position,
                "First SelectNext (RoundRobin) must teleport to anchor[0].");
            Assert.AreEqual(1, ctrl.CurrentIndex,
                "CurrentIndex must advance to 1 after first SelectNext.");

            ctrl.SelectNext(); // uses anchor[1], advances to 2
            Assert.AreEqual(anchorB.transform.position, robot.transform.position,
                "Second SelectNext (RoundRobin) must teleport to anchor[1].");
            Assert.AreEqual(2, ctrl.CurrentIndex,
                "CurrentIndex must advance to 2 after second SelectNext.");

            ctrl.SelectNext(); // uses anchor[2], wraps back to 0
            Assert.AreEqual(anchorC.transform.position, robot.transform.position,
                "Third SelectNext (RoundRobin) must teleport to anchor[2].");
            Assert.AreEqual(0, ctrl.CurrentIndex,
                "CurrentIndex must wrap back to 0 after three SelectNext calls with 3 anchors.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(anchorA);
            Object.DestroyImmediate(anchorB);
            Object.DestroyImmediate(anchorC);
            Object.DestroyImmediate(robot);
        }
    }
}
