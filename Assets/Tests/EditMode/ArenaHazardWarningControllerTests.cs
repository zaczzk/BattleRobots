using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T226: <see cref="ArenaHazardWarningController"/>.
    ///
    /// ArenaHazardWarningControllerTests (14):
    ///   FreshInstance_WarningDistance_IsDefault                    ×1
    ///   FreshInstance_RobotTransform_IsNull                        ×1
    ///   CheckProximity_NullRobot_DoesNotThrow                      ×1
    ///   CheckProximity_NullArrays_DoesNotThrow                     ×1
    ///   CheckProximity_EmptyArrays_HidesPanel                      ×1
    ///   CheckProximity_RobotFarFromHazard_HidesPanel               ×1
    ///   CheckProximity_RobotWithinWarningDistance_ShowsPanel        ×1
    ///   CheckProximity_SetsHazardNameLabel                         ×1
    ///   CheckProximity_SetsDistanceLabel                           ×1
    ///   CheckProximity_FindsNearestOfTwoHazards                    ×1
    ///   CheckProximity_NullHazardSO_SkipsEntry                     ×1
    ///   CheckProximity_NullTransform_SkipsEntry                    ×1
    ///   CheckProximity_MismatchedArrayLengths_UsesShortestLength   ×1
    ///   CheckProximity_AfterLeaving_HidesPanel                     ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class ArenaHazardWarningControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static ArenaHazardWarningController CreateController()
        {
            var go = new GameObject("ArenaHazardWarningCtrl_Test");
            go.SetActive(false);
            return go.AddComponent<ArenaHazardWarningController>();
        }

        private static HazardZoneSO CreateHazardSO(HazardZoneType type = HazardZoneType.Lava)
        {
            var so = ScriptableObject.CreateInstance<HazardZoneSO>();
            // HazardZoneSO fields default to Lava type; use reflection to set type
            FieldInfo fi = so.GetType()
                .GetField("_hazardType", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fi != null) fi.SetValue(so, type);
            return so;
        }

        private static Text AddText(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            return child.AddComponent<Text>();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_WarningDistance_IsDefault()
        {
            var ctrl = CreateController();
            Assert.AreEqual(10f, ctrl.WarningDistance, 0.001f,
                "Default WarningDistance must be 10f.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_RobotTransform_IsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.RobotTransform,
                "RobotTransform must be null when not assigned.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void CheckProximity_NullRobot_DoesNotThrow()
        {
            var ctrl = CreateController();
            // _robotTransform remains null
            Assert.DoesNotThrow(() => ctrl.CheckProximity());
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void CheckProximity_NullArrays_DoesNotThrow()
        {
            var ctrl  = CreateController();
            var robot = new GameObject("Robot");
            SetField(ctrl, "_robotTransform", robot.transform);
            // hazard arrays remain null
            Assert.DoesNotThrow(() => ctrl.CheckProximity());
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(robot);
        }

        [Test]
        public void CheckProximity_EmptyArrays_HidesPanel()
        {
            var ctrl  = CreateController();
            var robot = new GameObject("Robot");
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_robotTransform", robot.transform);
            SetField(ctrl, "_hazardZones",      new HazardZoneSO[0]);
            SetField(ctrl, "_hazardTransforms", new Transform[0]);
            SetField(ctrl, "_bannerPanel",      panel);

            ctrl.CheckProximity();

            Assert.IsFalse(panel.activeSelf, "Empty arrays: panel must be hidden.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(robot);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void CheckProximity_RobotFarFromHazard_HidesPanel()
        {
            var ctrl     = CreateController();
            var robot    = new GameObject("Robot");
            var hazardGo = new GameObject("Hazard");
            var panel    = new GameObject("Panel");
            var hazardSO = CreateHazardSO();

            // Place hazard 50 units away — far beyond default 10f warning distance
            hazardGo.transform.position = new Vector3(50f, 0f, 0f);
            robot.transform.position    = Vector3.zero;
            panel.SetActive(true);

            SetField(ctrl, "_robotTransform",  robot.transform);
            SetField(ctrl, "_hazardZones",      new[] { hazardSO });
            SetField(ctrl, "_hazardTransforms", new[] { hazardGo.transform });
            SetField(ctrl, "_bannerPanel",      panel);

            ctrl.CheckProximity();

            Assert.IsFalse(panel.activeSelf, "Panel must be hidden when robot is far from hazard.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(robot);
            Object.DestroyImmediate(hazardGo);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(hazardSO);
        }

        [Test]
        public void CheckProximity_RobotWithinWarningDistance_ShowsPanel()
        {
            var ctrl     = CreateController();
            var robot    = new GameObject("Robot");
            var hazardGo = new GameObject("Hazard");
            var panel    = new GameObject("Panel");
            var hazardSO = CreateHazardSO();

            // Place hazard 5 units away — within default 10f warning distance
            hazardGo.transform.position = new Vector3(5f, 0f, 0f);
            robot.transform.position    = Vector3.zero;
            panel.SetActive(false);

            SetField(ctrl, "_robotTransform",  robot.transform);
            SetField(ctrl, "_hazardZones",      new[] { hazardSO });
            SetField(ctrl, "_hazardTransforms", new[] { hazardGo.transform });
            SetField(ctrl, "_bannerPanel",      panel);

            ctrl.CheckProximity();

            Assert.IsTrue(panel.activeSelf, "Panel must be shown when robot is near a hazard.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(robot);
            Object.DestroyImmediate(hazardGo);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(hazardSO);
        }

        [Test]
        public void CheckProximity_SetsHazardNameLabel()
        {
            var ctrl     = CreateController();
            var robot    = new GameObject("Robot");
            var hazardGo = new GameObject("Hazard");
            var hazardSO = CreateHazardSO(HazardZoneType.Lava);
            var label    = AddText(ctrl.gameObject, "HazardNameLabel");

            hazardGo.transform.position = new Vector3(3f, 0f, 0f);
            robot.transform.position    = Vector3.zero;

            SetField(ctrl, "_robotTransform",  robot.transform);
            SetField(ctrl, "_hazardZones",      new[] { hazardSO });
            SetField(ctrl, "_hazardTransforms", new[] { hazardGo.transform });
            SetField(ctrl, "_hazardNameLabel",  label);

            ctrl.CheckProximity();

            Assert.AreEqual("Lava", label.text,
                "Hazard name label must show the HazardType name.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(robot);
            Object.DestroyImmediate(hazardGo);
            Object.DestroyImmediate(hazardSO);
        }

        [Test]
        public void CheckProximity_SetsDistanceLabel()
        {
            var ctrl     = CreateController();
            var robot    = new GameObject("Robot");
            var hazardGo = new GameObject("Hazard");
            var hazardSO = CreateHazardSO();
            var label    = AddText(ctrl.gameObject, "DistLabel");

            hazardGo.transform.position = new Vector3(7f, 0f, 0f);
            robot.transform.position    = Vector3.zero;

            SetField(ctrl, "_robotTransform",  robot.transform);
            SetField(ctrl, "_hazardZones",      new[] { hazardSO });
            SetField(ctrl, "_hazardTransforms", new[] { hazardGo.transform });
            SetField(ctrl, "_distanceLabel",    label);

            ctrl.CheckProximity();

            Assert.AreEqual("7m", label.text,
                "Distance label must show rounded distance in metres.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(robot);
            Object.DestroyImmediate(hazardGo);
            Object.DestroyImmediate(hazardSO);
        }

        [Test]
        public void CheckProximity_FindsNearestOfTwoHazards()
        {
            var ctrl      = CreateController();
            var robot     = new GameObject("Robot");
            var hazard1Go = new GameObject("Hazard1");
            var hazard2Go = new GameObject("Hazard2");
            var so1       = CreateHazardSO(HazardZoneType.Lava);
            var so2       = CreateHazardSO(HazardZoneType.Electric);
            var label     = AddText(ctrl.gameObject, "HazardNameLabel");

            robot.transform.position    = Vector3.zero;
            hazard1Go.transform.position = new Vector3(8f, 0f, 0f);  // farther
            hazard2Go.transform.position = new Vector3(3f, 0f, 0f);  // nearer

            SetField(ctrl, "_robotTransform",  robot.transform);
            SetField(ctrl, "_hazardZones",      new[] { so1, so2 });
            SetField(ctrl, "_hazardTransforms", new[] { hazard1Go.transform, hazard2Go.transform });
            SetField(ctrl, "_hazardNameLabel",  label);

            ctrl.CheckProximity();

            Assert.AreEqual("Electric", label.text,
                "Nearest hazard (Electric at 3m) must be selected over Lava at 8m.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(robot);
            Object.DestroyImmediate(hazard1Go);
            Object.DestroyImmediate(hazard2Go);
            Object.DestroyImmediate(so1);
            Object.DestroyImmediate(so2);
        }

        [Test]
        public void CheckProximity_NullHazardSO_SkipsEntry()
        {
            var ctrl     = CreateController();
            var robot    = new GameObject("Robot");
            var hazardGo = new GameObject("Hazard");
            var panel    = new GameObject("Panel");
            panel.SetActive(true);

            hazardGo.transform.position = new Vector3(3f, 0f, 0f);
            robot.transform.position    = Vector3.zero;

            // Null SO entry — should be skipped; panel must stay hidden
            SetField(ctrl, "_robotTransform",  robot.transform);
            SetField(ctrl, "_hazardZones",      new HazardZoneSO[] { null });
            SetField(ctrl, "_hazardTransforms", new[] { hazardGo.transform });
            SetField(ctrl, "_bannerPanel",      panel);

            Assert.DoesNotThrow(() => ctrl.CheckProximity(),
                "Null HazardZoneSO entry must be silently skipped.");
            Assert.IsFalse(panel.activeSelf, "Panel must stay hidden when all SOs are null.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(robot);
            Object.DestroyImmediate(hazardGo);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void CheckProximity_NullTransform_SkipsEntry()
        {
            var ctrl     = CreateController();
            var robot    = new GameObject("Robot");
            var panel    = new GameObject("Panel");
            var hazardSO = CreateHazardSO();
            panel.SetActive(true);

            robot.transform.position = Vector3.zero;

            // Null Transform entry — should be skipped
            SetField(ctrl, "_robotTransform",  robot.transform);
            SetField(ctrl, "_hazardZones",      new[] { hazardSO });
            SetField(ctrl, "_hazardTransforms", new Transform[] { null });
            SetField(ctrl, "_bannerPanel",      panel);

            Assert.DoesNotThrow(() => ctrl.CheckProximity(),
                "Null Transform entry must be silently skipped.");
            Assert.IsFalse(panel.activeSelf, "Panel must stay hidden when all transforms are null.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(robot);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(hazardSO);
        }

        [Test]
        public void CheckProximity_MismatchedArrayLengths_UsesShortestLength()
        {
            var ctrl      = CreateController();
            var robot     = new GameObject("Robot");
            var hazard1Go = new GameObject("Hazard1");
            var so1       = CreateHazardSO(HazardZoneType.Lava);
            var so2       = CreateHazardSO(HazardZoneType.Electric);
            var panel     = new GameObject("Panel");
            panel.SetActive(false);

            robot.transform.position    = Vector3.zero;
            hazard1Go.transform.position = new Vector3(3f, 0f, 0f);

            // 2 SOs but only 1 transform — shortest=1, only index 0 checked
            SetField(ctrl, "_robotTransform",  robot.transform);
            SetField(ctrl, "_hazardZones",      new[] { so1, so2 });
            SetField(ctrl, "_hazardTransforms", new[] { hazard1Go.transform });
            SetField(ctrl, "_bannerPanel",      panel);

            Assert.DoesNotThrow(() => ctrl.CheckProximity(),
                "Mismatched array lengths must not throw.");
            Assert.IsTrue(panel.activeSelf, "Panel must show for the one valid matched pair.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(robot);
            Object.DestroyImmediate(hazard1Go);
            Object.DestroyImmediate(so1);
            Object.DestroyImmediate(so2);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void CheckProximity_AfterLeaving_HidesPanel()
        {
            var ctrl     = CreateController();
            var robot    = new GameObject("Robot");
            var hazardGo = new GameObject("Hazard");
            var panel    = new GameObject("Panel");
            var hazardSO = CreateHazardSO();

            hazardGo.transform.position = new Vector3(3f, 0f, 0f);
            robot.transform.position    = Vector3.zero;
            panel.SetActive(false);

            SetField(ctrl, "_robotTransform",  robot.transform);
            SetField(ctrl, "_hazardZones",      new[] { hazardSO });
            SetField(ctrl, "_hazardTransforms", new[] { hazardGo.transform });
            SetField(ctrl, "_bannerPanel",      panel);

            ctrl.CheckProximity();
            Assert.IsTrue(panel.activeSelf, "Panel must appear when robot is near.");

            // Move robot away
            robot.transform.position = new Vector3(50f, 0f, 0f);
            ctrl.CheckProximity();
            Assert.IsFalse(panel.activeSelf, "Panel must hide when robot moves out of range.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(robot);
            Object.DestroyImmediate(hazardGo);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(hazardSO);
        }
    }
}
