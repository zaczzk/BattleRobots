using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T230:
    ///   <see cref="RespawnZoneSO"/> and <see cref="RespawnZoneController"/>.
    ///
    /// RespawnZoneSOTests (4):
    ///   FreshInstance_AnchorRadius_DefaultIsOne     ×1
    ///   AnchorRadius_PropertyRoundTrip              ×1
    ///   AnchorRadius_ZeroAllowed                    ×1
    ///   AnchorRadius_PositiveValue                  ×1
    ///
    /// RespawnZoneControllerTests (12):
    ///   FreshInstance_ZoneNull                      ×1
    ///   FreshInstance_RobotTransformNull            ×1
    ///   FreshInstance_AnchorsEmpty                  ×1
    ///   OnEnable_NullRefs_DoesNotThrow              ×1
    ///   OnDisable_NullRefs_DoesNotThrow             ×1
    ///   OnDisable_Unregisters                       ×1
    ///   OnRespawnReady_NullRobotTransform_NoThrow   ×1
    ///   OnRespawnReady_NoAnchors_UsesOwnPosition    ×1
    ///   OnRespawnReady_WithOneAnchor_TeleportsToIt  ×1
    ///   OnRespawnReady_WithTwoAnchors_SelectsNearest×1
    ///   Zone_Property_ReturnsAssigned               ×1
    ///   RobotTransform_Property_ReturnsAssigned     ×1
    ///
    /// Total: 16 new EditMode tests.
    /// </summary>
    public class RespawnZoneTests
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

        private static RespawnZoneSO CreateZoneSO() =>
            ScriptableObject.CreateInstance<RespawnZoneSO>();

        private static VoidGameEvent CreateVoidEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static RespawnZoneController CreateController() =>
            new GameObject("RespawnZoneCtrl_Test").AddComponent<RespawnZoneController>();

        // ── RespawnZoneSOTests ────────────────────────────────────────────────

        [Test]
        public void FreshInstance_AnchorRadius_DefaultIsOne()
        {
            var so = CreateZoneSO();
            Assert.AreEqual(1f, so.AnchorRadius, 0.001f,
                "Default AnchorRadius must be 1f.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AnchorRadius_PropertyRoundTrip()
        {
            var so = CreateZoneSO();
            SetField(so, "_anchorRadius", 3.5f);
            Assert.AreEqual(3.5f, so.AnchorRadius, 0.001f,
                "AnchorRadius property must reflect the serialised field.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AnchorRadius_ZeroAllowed()
        {
            var so = CreateZoneSO();
            SetField(so, "_anchorRadius", 0f);
            Assert.AreEqual(0f, so.AnchorRadius, 0.001f,
                "AnchorRadius of 0 must be permitted (Min(0f) attribute).");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AnchorRadius_PositiveValue()
        {
            var so = CreateZoneSO();
            SetField(so, "_anchorRadius", 5f);
            Assert.Greater(so.AnchorRadius, 0f,
                "A positive AnchorRadius must be stored correctly.");
            Object.DestroyImmediate(so);
        }

        // ── RespawnZoneControllerTests ────────────────────────────────────────

        [Test]
        public void FreshInstance_ZoneNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Zone, "Zone must be null on a fresh instance.");
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
        public void FreshInstance_AnchorsEmpty()
        {
            var ctrl = CreateController();
            // _respawnAnchors defaults to null in Unity; accessing via reflection
            FieldInfo fi = ctrl.GetType()
                .GetField("_respawnAnchors", BindingFlags.Instance | BindingFlags.NonPublic);
            var anchors = fi.GetValue(ctrl) as Transform[];
            // Either null or empty is acceptable
            Assert.IsTrue(anchors == null || anchors.Length == 0,
                "_respawnAnchors must be null or empty on a fresh instance.");
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
        public void OnRespawnReady_NullRobotTransform_NoThrow()
        {
            var ctrl = CreateController();
            var ch   = CreateVoidEvent();
            SetField(ctrl, "_onRespawnReady", ch);
            // _robotTransform remains null
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            Assert.DoesNotThrow(() => ch.Raise(),
                "OnRespawnReady must not throw when _robotTransform is null.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void OnRespawnReady_NoAnchors_UsesOwnPosition()
        {
            var ctrl     = CreateController();
            var robotGO  = new GameObject("Robot");
            robotGO.transform.position = new Vector3(10f, 0f, 0f);

            // Place controller at origin (0,0,0) — fallback destination
            ctrl.transform.position = Vector3.zero;

            SetField(ctrl, "_robotTransform", robotGO.transform);
            // _respawnAnchors left null

            var ch = CreateVoidEvent();
            SetField(ctrl, "_onRespawnReady", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            ch.Raise(); // triggers OnRespawnReady

            Assert.AreEqual(Vector3.zero, robotGO.transform.position,
                "With no anchors the robot must teleport to the controller's own position.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(robotGO);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void OnRespawnReady_WithOneAnchor_TeleportsToIt()
        {
            var ctrl    = CreateController();
            var robotGO = new GameObject("Robot");
            var anchor  = new GameObject("Anchor");
            anchor.transform.position = new Vector3(5f, 0f, 0f);

            SetField(ctrl, "_robotTransform",  robotGO.transform);
            SetField(ctrl, "_respawnAnchors", new Transform[] { anchor.transform });

            var ch = CreateVoidEvent();
            SetField(ctrl, "_onRespawnReady", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            ch.Raise();

            Assert.AreEqual(anchor.transform.position, robotGO.transform.position,
                "Robot must teleport to the single anchor's position.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(robotGO);
            Object.DestroyImmediate(anchor);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void OnRespawnReady_WithTwoAnchors_SelectsNearest()
        {
            var ctrl    = CreateController();
            var robotGO = new GameObject("Robot");
            robotGO.transform.position = new Vector3(1f, 0f, 0f); // close to anchor1

            var anchor1 = new GameObject("NearAnchor");
            var anchor2 = new GameObject("FarAnchor");
            anchor1.transform.position = new Vector3(2f, 0f, 0f);   // 1 unit away
            anchor2.transform.position = new Vector3(20f, 0f, 0f);  // 19 units away

            SetField(ctrl, "_robotTransform",  robotGO.transform);
            SetField(ctrl, "_respawnAnchors",
                new Transform[] { anchor2.transform, anchor1.transform }); // far first

            var ch = CreateVoidEvent();
            SetField(ctrl, "_onRespawnReady", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            ch.Raise();

            Assert.AreEqual(anchor1.transform.position, robotGO.transform.position,
                "Robot must teleport to the nearest anchor.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(robotGO);
            Object.DestroyImmediate(anchor1);
            Object.DestroyImmediate(anchor2);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Zone_Property_ReturnsAssigned()
        {
            var ctrl = CreateController();
            var so   = CreateZoneSO();
            SetField(ctrl, "_zone", so);
            Assert.AreSame(so, ctrl.Zone,
                "Zone property must return the assigned RespawnZoneSO.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void RobotTransform_Property_ReturnsAssigned()
        {
            var ctrl    = CreateController();
            var robotGO = new GameObject("Robot");
            SetField(ctrl, "_robotTransform", robotGO.transform);
            Assert.AreSame(robotGO.transform, ctrl.RobotTransform,
                "RobotTransform property must return the assigned Transform.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(robotGO);
        }
    }
}
