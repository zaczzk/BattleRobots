using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T219:
    ///   <see cref="PartDurabilityTrackerSO"/> and <see cref="PartDurabilityHUDController"/>.
    ///
    /// PartDurabilityTrackerSOTests (10):
    ///   FreshInstance_PartCount_Zero                              ×1
    ///   AddPart_IncrementCount                                    ×1
    ///   AddPart_GetDurability_ReturnsMax                          ×1
    ///   ApplyDamage_ReducesDurability                             ×1
    ///   ApplyDamage_Clamps_ToZero                                 ×1
    ///   GetDurabilityRatio_FullHealth                             ×1
    ///   GetDurabilityRatio_HalfHealth                             ×1
    ///   IsDestroyed_FalseWhenAlive                                ×1
    ///   IsDestroyed_TrueWhenZero                                  ×1
    ///   Reset_RestoresAllToMax                                    ×1
    ///
    /// PartDurabilityHUDControllerTests (8):
    ///   FreshInstance_TrackerNull                                 ×1
    ///   OnEnable_NullRefs_DoesNotThrow                            ×1
    ///   OnDisable_NullRefs_DoesNotThrow                           ×1
    ///   OnDisable_Unregisters                                     ×1
    ///   Refresh_NullTracker_ShowsEmptyLabel                       ×1
    ///   Refresh_NullContainer_DoesNotThrow                        ×1
    ///   Refresh_WithParts_BuildsRows                              ×1
    ///   Refresh_NoParts_ShowsEmptyLabel                           ×1
    ///
    /// Total: 18 new EditMode tests.
    /// </summary>
    public class PartDurabilityTests
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

        private static PartDurabilityTrackerSO CreateTracker()
        {
            var so = ScriptableObject.CreateInstance<PartDurabilityTrackerSO>();
            InvokePrivate(so, "OnEnable");
            return so;
        }

        private static PartDurabilityHUDController CreateController() =>
            new GameObject("PartDurabilityHUD_Test").AddComponent<PartDurabilityHUDController>();

        private static VoidGameEvent CreateVoidEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        // Creates a row prefab with one Text child and one Slider child.
        private static GameObject CreateRowPrefab()
        {
            var prefab = new GameObject("RowPrefab");

            var textChild = new GameObject("Text");
            textChild.transform.SetParent(prefab.transform);
            textChild.AddComponent<Text>();

            var sliderChild = new GameObject("Slider");
            sliderChild.transform.SetParent(prefab.transform);
            sliderChild.AddComponent<Slider>();

            return prefab;
        }

        // ── PartDurabilityTrackerSOTests ───────────────────────────────────────

        [Test]
        public void FreshInstance_PartCount_Zero()
        {
            var so = CreateTracker();
            Assert.AreEqual(0, so.PartCount);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AddPart_IncrementCount()
        {
            var so = CreateTracker();
            so.AddPart("leg_left", 100f);
            Assert.AreEqual(1, so.PartCount);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AddPart_GetDurability_ReturnsMax()
        {
            var so = CreateTracker();
            so.AddPart("arm_right", 80f);
            Assert.AreEqual(80f, so.GetDurability("arm_right"), 0.001f);
            Assert.AreEqual(80f, so.GetMaxDurability("arm_right"), 0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void ApplyDamage_ReducesDurability()
        {
            var so = CreateTracker();
            so.AddPart("chassis", 100f);
            so.ApplyDamage("chassis", 30f);
            Assert.AreEqual(70f, so.GetDurability("chassis"), 0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void ApplyDamage_Clamps_ToZero()
        {
            var so = CreateTracker();
            so.AddPart("leg", 20f);
            so.ApplyDamage("leg", 999f);
            Assert.AreEqual(0f, so.GetDurability("leg"), 0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetDurabilityRatio_FullHealth()
        {
            var so = CreateTracker();
            so.AddPart("arm", 50f);
            Assert.AreEqual(1f, so.GetDurabilityRatio("arm"), 0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetDurabilityRatio_HalfHealth()
        {
            var so = CreateTracker();
            so.AddPart("arm", 100f);
            so.ApplyDamage("arm", 50f);
            Assert.AreEqual(0.5f, so.GetDurabilityRatio("arm"), 0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void IsDestroyed_FalseWhenAlive()
        {
            var so = CreateTracker();
            so.AddPart("head", 100f);
            Assert.IsFalse(so.IsDestroyed("head"));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void IsDestroyed_TrueWhenZero()
        {
            var so = CreateTracker();
            so.AddPart("head", 100f);
            so.ApplyDamage("head", 100f);
            Assert.IsTrue(so.IsDestroyed("head"));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Reset_RestoresAllToMax()
        {
            var so = CreateTracker();
            so.AddPart("partA", 100f);
            so.AddPart("partB", 50f);
            so.ApplyDamage("partA", 40f);
            so.ApplyDamage("partB", 25f);
            so.Reset();

            Assert.AreEqual(100f, so.GetDurability("partA"), 0.001f,
                "partA should be restored to max after Reset.");
            Assert.AreEqual(50f,  so.GetDurability("partB"), 0.001f,
                "partB should be restored to max after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── PartDurabilityHUDControllerTests ──────────────────────────────────

        [Test]
        public void FreshInstance_TrackerNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Tracker);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateVoidEvent();
            SetField(ctrl, "_onDurabilityChanged", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, count, "After OnDisable only the manually registered callback fires.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Refresh_NullTracker_ShowsEmptyLabel()
        {
            var ctrl     = CreateController();
            var emptyLbl = new GameObject("empty");
            emptyLbl.SetActive(false);
            SetField(ctrl, "_emptyLabel", emptyLbl);
            // _tracker left null
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(emptyLbl.activeSelf,
                "Empty label should show when tracker is null.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(emptyLbl);
        }

        [Test]
        public void Refresh_NullContainer_DoesNotThrow()
        {
            var ctrl    = CreateController();
            var tracker = CreateTracker();
            tracker.AddPart("arm", 100f);
            SetField(ctrl, "_tracker", tracker);
            // _rowContainer left null
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.Refresh());
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(tracker);
        }

        [Test]
        public void Refresh_WithParts_BuildsRows()
        {
            var ctrl      = CreateController();
            var container = new GameObject("container");
            var tracker   = CreateTracker();
            var prefab    = CreateRowPrefab();

            tracker.AddPart("leg_left",  100f);
            tracker.AddPart("leg_right", 80f);

            SetField(ctrl, "_tracker",      tracker);
            SetField(ctrl, "_rowContainer", container.transform);
            SetField(ctrl, "_rowPrefab",    prefab);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual(2, container.transform.childCount,
                "Refresh should create one row per registered part.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(tracker);
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Refresh_NoParts_ShowsEmptyLabel()
        {
            var ctrl     = CreateController();
            var emptyLbl = new GameObject("empty");
            emptyLbl.SetActive(false);
            var tracker  = CreateTracker();   // no parts added

            SetField(ctrl, "_tracker",    tracker);
            SetField(ctrl, "_emptyLabel", emptyLbl);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(emptyLbl.activeSelf,
                "Empty label should show when tracker has zero parts.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(emptyLbl);
            Object.DestroyImmediate(tracker);
        }
    }
}
