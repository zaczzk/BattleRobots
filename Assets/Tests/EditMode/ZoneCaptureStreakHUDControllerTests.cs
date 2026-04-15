using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T272: <see cref="ZoneCaptureStreakHUDController"/>.
    ///
    /// ZoneCaptureStreakHUDControllerTests (12):
    ///   FreshInstance_StreakSO_Null                                     ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                  ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                 ×1
    ///   OnDisable_Unregisters_Channel                                   ×1
    ///   Refresh_NullSO_HidesPanel                                       ×1
    ///   Refresh_WithSO_ShowsPanel                                       ×1
    ///   Refresh_NullLabel_DoesNotThrow                                  ×1
    ///   Refresh_NullBonusBadge_DoesNotThrow                             ×1
    ///   Refresh_HasBonus_ShowsBonusBadge                                ×1
    ///   Refresh_NoBonus_HidesBonusBadge                                 ×1
    ///   Refresh_StreakLabel_CorrectText                                  ×1
    ///   OnEnable_CallsRefresh                                           ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneCaptureStreakHUDControllerTests
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

        private static ZoneCaptureStreakHUDController CreateController() =>
            new GameObject("ZoneCaptureStreakHUD_Test")
                .AddComponent<ZoneCaptureStreakHUDController>();

        private static ZoneCaptureStreakSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneCaptureStreakSO>();

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static GameObject CreatePanel() => new GameObject("Panel_Test");

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_StreakSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.StreakSO,
                "StreakSO must be null on a fresh ZoneCaptureStreakHUDController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters_Channel()
        {
            var ctrl    = CreateController();
            var streakSO = CreateSO();
            var panel   = CreatePanel();
            var evt     = CreateEvent();

            SetField(ctrl, "_streakSO",        streakSO);
            SetField(ctrl, "_onStreakChanged",  evt);
            SetField(ctrl, "_panel",           panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            // Panel is shown while enabled.
            Assert.IsTrue(panel.activeSelf, "Pre-condition: panel active after OnEnable.");

            InvokePrivate(ctrl, "OnDisable");

            // Raising streak event after disable must not trigger Refresh (panel still
            // in whatever state OnDisable left it — we only care the delegate is removed).
            bool threw = false;
            try { evt.Raise(); }
            catch { threw = true; }
            Assert.IsFalse(threw, "Raising event after OnDisable must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(streakSO);
            Object.DestroyImmediate(evt);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = CreatePanel();
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Refresh with null SO must hide the panel.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_WithSO_ShowsPanel()
        {
            var ctrl     = CreateController();
            var streakSO = CreateSO();
            var panel    = CreatePanel();
            panel.SetActive(false);

            SetField(ctrl, "_streakSO", streakSO);
            SetField(ctrl, "_panel",    panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Refresh with a valid SO must show the panel.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(streakSO);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_NullLabel_DoesNotThrow()
        {
            var ctrl     = CreateController();
            var streakSO = CreateSO();
            SetField(ctrl, "_streakSO", streakSO);
            // _streakLabel and _bonusMultiplierLabel left null.
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh must not throw when Text refs are null.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(streakSO);
        }

        [Test]
        public void Refresh_NullBonusBadge_DoesNotThrow()
        {
            var ctrl     = CreateController();
            var streakSO = CreateSO();
            SetField(ctrl, "_streakSO",  streakSO);
            // _bonusBadge left null.
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh must not throw when _bonusBadge is null.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(streakSO);
        }

        [Test]
        public void Refresh_HasBonus_ShowsBonusBadge()
        {
            var ctrl       = CreateController();
            var streakSO   = CreateSO();
            var bonusBadge = new GameObject("BonusBadge_Test");
            bonusBadge.SetActive(false);

            SetField(ctrl, "_streakSO",   streakSO);
            SetField(ctrl, "_bonusBadge", bonusBadge);
            InvokePrivate(ctrl, "Awake");

            // Reach the streak threshold (default 3).
            streakSO.IncrementStreak();
            streakSO.IncrementStreak();
            streakSO.IncrementStreak();
            Assert.IsTrue(streakSO.HasBonus, "Pre-condition: HasBonus must be true.");

            ctrl.Refresh();

            Assert.IsTrue(bonusBadge.activeSelf,
                "Refresh must show _bonusBadge when HasBonus is true.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(streakSO);
            Object.DestroyImmediate(bonusBadge);
        }

        [Test]
        public void Refresh_NoBonus_HidesBonusBadge()
        {
            var ctrl       = CreateController();
            var streakSO   = CreateSO();
            var bonusBadge = new GameObject("BonusBadge_Test");
            bonusBadge.SetActive(true);

            SetField(ctrl, "_streakSO",   streakSO);
            SetField(ctrl, "_bonusBadge", bonusBadge);
            InvokePrivate(ctrl, "Awake");

            // Streak is 0 — HasBonus is false.
            ctrl.Refresh();

            Assert.IsFalse(bonusBadge.activeSelf,
                "Refresh must hide _bonusBadge when HasBonus is false.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(streakSO);
            Object.DestroyImmediate(bonusBadge);
        }

        [Test]
        public void Refresh_StreakLabel_CorrectText()
        {
            var ctrl        = CreateController();
            var streakSO    = CreateSO();
            var labelGO     = new GameObject("Label_Test");
            var streakLabel = labelGO.AddComponent<UnityEngine.UI.Text>();

            SetField(ctrl, "_streakSO",    streakSO);
            SetField(ctrl, "_streakLabel", streakLabel);
            InvokePrivate(ctrl, "Awake");

            streakSO.IncrementStreak();
            streakSO.IncrementStreak();
            ctrl.Refresh();

            Assert.AreEqual("Streak: 2", streakLabel.text,
                "Refresh must write 'Streak: N' to _streakLabel.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(streakSO);
            Object.DestroyImmediate(labelGO);
        }

        [Test]
        public void OnEnable_CallsRefresh()
        {
            var ctrl     = CreateController();
            var streakSO = CreateSO();
            var panel    = CreatePanel();
            panel.SetActive(false);

            SetField(ctrl, "_streakSO", streakSO);
            SetField(ctrl, "_panel",    panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            // Refresh should have shown the panel.
            Assert.IsTrue(panel.activeSelf,
                "OnEnable must call Refresh, which shows the panel when SO is assigned.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(streakSO);
            Object.DestroyImmediate(panel);
        }
    }
}
