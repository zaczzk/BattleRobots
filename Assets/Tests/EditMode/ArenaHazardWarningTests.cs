using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T227: <see cref="ArenaHazardWarningController"/>.
    ///
    /// ArenaHazardWarningControllerTests (12):
    ///   FreshInstance_PlayerTransformNull                        ×1
    ///   FreshInstance_WarningDistance_IsDefault                  ×1
    ///   FreshInstance_IsInDanger_False                           ×1
    ///   CheckProximity_NullPlayer_DoesNotThrow                   ×1
    ///   CheckProximity_NullPlayer_HidesPanel                     ×1
    ///   CheckProximity_EmptyHazardArrays_DoesNotThrow            ×1
    ///   CheckProximity_PlayerFarFromHazard_IsInDanger_False      ×1
    ///   CheckProximity_PlayerNearHazard_IsInDanger_True          ×1
    ///   CheckProximity_PlayerNearHazard_ShowsPanel               ×1
    ///   CheckProximity_EntersDanger_FiresOnEnterDanger           ×1
    ///   CheckProximity_AlreadyInDanger_DoesNotFireEventAgain     ×1
    ///   CheckProximity_NullPanel_DoesNotThrow                    ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ArenaHazardWarningTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static ArenaHazardWarningController MakeController()
        {
            var go = new GameObject("ArenaHazardWarningCtrl_Test");
            go.SetActive(false);
            return go.AddComponent<ArenaHazardWarningController>();
        }

        private static VoidGameEvent MakeEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static HazardZoneSO MakeHazardZone() =>
            ScriptableObject.CreateInstance<HazardZoneSO>();

        private static Text AddText(GameObject parent)
        {
            var child = new GameObject("Label");
            child.transform.SetParent(parent.transform);
            return child.AddComponent<Text>();
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_PlayerTransformNull()
        {
            var ctrl = MakeController();
            Assert.IsNull(ctrl.PlayerTransform,
                "Fresh instance must have null PlayerTransform.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_WarningDistance_IsDefault()
        {
            var ctrl = MakeController();
            Assert.AreEqual(5f, ctrl.WarningDistance, 0.001f,
                "Default WarningDistance must be 5.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_IsInDanger_False()
        {
            var ctrl = MakeController();
            Assert.IsFalse(ctrl.IsInDanger,
                "Fresh instance must not be in danger.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        // ── Null-player guards ────────────────────────────────────────────────

        [Test]
        public void CheckProximity_NullPlayer_DoesNotThrow()
        {
            var ctrl = MakeController();
            // _playerTransform null by default
            Assert.DoesNotThrow(() => ctrl.CheckProximity());
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void CheckProximity_NullPlayer_HidesPanel()
        {
            var ctrl  = MakeController();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            SetField(ctrl, "_warningPanel", panel);

            ctrl.CheckProximity();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when _playerTransform is null.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        // ── Empty hazard arrays ───────────────────────────────────────────────

        [Test]
        public void CheckProximity_EmptyHazardArrays_DoesNotThrow()
        {
            var ctrl   = MakeController();
            var player = new GameObject("Player");
            ctrl.PlayerTransform = player.transform;

            SetField(ctrl, "_hazardZones",      new HazardZoneSO[0]);
            SetField(ctrl, "_hazardTransforms", new Transform[0]);

            Assert.DoesNotThrow(() => ctrl.CheckProximity());
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(player);
        }

        // ── Distance logic ────────────────────────────────────────────────────

        [Test]
        public void CheckProximity_PlayerFarFromHazard_IsInDanger_False()
        {
            var ctrl   = MakeController();
            var player = new GameObject("Player");
            player.transform.position = Vector3.zero;

            var hazardGo = new GameObject("Hazard");
            hazardGo.transform.position = new Vector3(100f, 0f, 0f); // far away

            var zone = MakeHazardZone();

            SetField(ctrl, "_warningDistance",  4f);
            SetField(ctrl, "_hazardZones",      new HazardZoneSO[] { zone });
            SetField(ctrl, "_hazardTransforms", new Transform[] { hazardGo.transform });
            ctrl.PlayerTransform = player.transform;

            ctrl.CheckProximity();

            Assert.IsFalse(ctrl.IsInDanger,
                "Player far from hazard must not be in danger.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(hazardGo);
            Object.DestroyImmediate(zone);
        }

        [Test]
        public void CheckProximity_PlayerNearHazard_IsInDanger_True()
        {
            var ctrl   = MakeController();
            var player = new GameObject("Player");
            player.transform.position = Vector3.zero;

            var hazardGo = new GameObject("Hazard");
            hazardGo.transform.position = new Vector3(3f, 0f, 0f); // within 5 m

            var zone = MakeHazardZone();

            SetField(ctrl, "_warningDistance",  5f);
            SetField(ctrl, "_hazardZones",      new HazardZoneSO[] { zone });
            SetField(ctrl, "_hazardTransforms", new Transform[] { hazardGo.transform });
            ctrl.PlayerTransform = player.transform;

            ctrl.CheckProximity();

            Assert.IsTrue(ctrl.IsInDanger,
                "Player within warning distance must be in danger.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(hazardGo);
            Object.DestroyImmediate(zone);
        }

        [Test]
        public void CheckProximity_PlayerNearHazard_ShowsPanel()
        {
            var ctrl  = MakeController();
            var panel = new GameObject("Panel");
            panel.SetActive(false);

            var player   = new GameObject("Player");
            var hazardGo = new GameObject("Hazard");
            hazardGo.transform.position = new Vector3(2f, 0f, 0f);

            var zone = MakeHazardZone();

            SetField(ctrl, "_warningDistance",  5f);
            SetField(ctrl, "_warningPanel",     panel);
            SetField(ctrl, "_hazardZones",      new HazardZoneSO[] { zone });
            SetField(ctrl, "_hazardTransforms", new Transform[] { hazardGo.transform });
            ctrl.PlayerTransform = player.transform;

            ctrl.CheckProximity();

            Assert.IsTrue(panel.activeSelf,
                "Warning panel must be shown when player is within hazard range.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(hazardGo);
            Object.DestroyImmediate(zone);
        }

        // ── Event firing ──────────────────────────────────────────────────────

        [Test]
        public void CheckProximity_EntersDanger_FiresOnEnterDanger()
        {
            var ctrl     = MakeController();
            var evt      = MakeEvent();
            var player   = new GameObject("Player");
            var hazardGo = new GameObject("Hazard");
            hazardGo.transform.position = new Vector3(2f, 0f, 0f);
            var zone = MakeHazardZone();

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            SetField(ctrl, "_warningDistance",  5f);
            SetField(ctrl, "_onEnterDanger",    evt);
            SetField(ctrl, "_hazardZones",      new HazardZoneSO[] { zone });
            SetField(ctrl, "_hazardTransforms", new Transform[] { hazardGo.transform });
            ctrl.PlayerTransform = player.transform;

            ctrl.CheckProximity(); // first call — enter danger

            Assert.AreEqual(1, fired,
                "_onEnterDanger must be raised once when entering danger range.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(evt);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(hazardGo);
            Object.DestroyImmediate(zone);
        }

        [Test]
        public void CheckProximity_AlreadyInDanger_DoesNotFireEventAgain()
        {
            var ctrl     = MakeController();
            var evt      = MakeEvent();
            var player   = new GameObject("Player");
            var hazardGo = new GameObject("Hazard");
            hazardGo.transform.position = new Vector3(2f, 0f, 0f);
            var zone = MakeHazardZone();

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            SetField(ctrl, "_warningDistance",  5f);
            SetField(ctrl, "_onEnterDanger",    evt);
            SetField(ctrl, "_hazardZones",      new HazardZoneSO[] { zone });
            SetField(ctrl, "_hazardTransforms", new Transform[] { hazardGo.transform });
            ctrl.PlayerTransform = player.transform;

            ctrl.CheckProximity(); // enter danger (fires once)
            ctrl.CheckProximity(); // still in danger (must NOT fire again)

            Assert.AreEqual(1, fired,
                "_onEnterDanger must fire only once per entry, not on subsequent frames.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(evt);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(hazardGo);
            Object.DestroyImmediate(zone);
        }

        // ── Null UI refs ──────────────────────────────────────────────────────

        [Test]
        public void CheckProximity_NullPanel_DoesNotThrow()
        {
            var ctrl   = MakeController();
            var player = new GameObject("Player");
            var hazardGo = new GameObject("Hazard");
            hazardGo.transform.position = new Vector3(2f, 0f, 0f);
            var zone = MakeHazardZone();

            // _warningPanel remains null
            SetField(ctrl, "_warningDistance",  5f);
            SetField(ctrl, "_hazardZones",      new HazardZoneSO[] { zone });
            SetField(ctrl, "_hazardTransforms", new Transform[] { hazardGo.transform });
            ctrl.PlayerTransform = player.transform;

            Assert.DoesNotThrow(() => ctrl.CheckProximity(),
                "Null _warningPanel must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(hazardGo);
            Object.DestroyImmediate(zone);
        }
    }
}
