using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T194 — <see cref="CombinedBonusHUDController"/>.
    ///
    /// CombinedBonusHUDControllerTests (14):
    ///   Ctrl_FreshInstance_CombinedBonusIsNull                        ×1
    ///   Ctrl_OnEnable_NullRefs_DoesNotThrow                           ×1
    ///   Ctrl_OnDisable_NullRefs_DoesNotThrow                          ×1
    ///   Ctrl_OnDisable_Unregisters_PrestigeChannel                    ×1
    ///   Ctrl_OnDisable_Unregisters_MasteryChannel                     ×1
    ///   Ctrl_Refresh_NullBonus_PrestigeLabelShowsOne                  ×1
    ///   Ctrl_Refresh_NullBonus_MasteryLabelShowsOne                   ×1
    ///   Ctrl_Refresh_NullBonus_TotalLabelShowsOne                     ×1
    ///   Ctrl_Refresh_NullPanel_DoesNotThrow                           ×1
    ///   Ctrl_Refresh_NullLabels_DoesNotThrow                          ×1
    ///   Ctrl_OnPrestige_TriggersRefresh                               ×1
    ///   Ctrl_OnMasteryUnlocked_TriggersRefresh                        ×1
    ///   Ctrl_CombinedBonus_Property_ReturnsAssigned                   ×1
    ///   Ctrl_Refresh_WithBonus_PrestigeLabelFormatted                  ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class CombinedBonusHUDControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static CombinedBonusHUDController CreateCtrl()
        {
            var go = new GameObject("CombinedBonusHUD_Test");
            return go.AddComponent<CombinedBonusHUDController>();
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static Text CreateText()
        {
            var go = new GameObject("Text");
            return go.AddComponent<Text>();
        }

        // ── FreshInstance ─────────────────────────────────────────────────────

        [Test]
        public void Ctrl_FreshInstance_CombinedBonusIsNull()
        {
            var ctrl = CreateCtrl();
            Assert.IsNull(ctrl.CombinedBonus,
                "Fresh controller must have null CombinedBonus.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        // ── Lifecycle null safety ─────────────────────────────────────────────

        [Test]
        public void Ctrl_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateCtrl();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with all null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateCtrl();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with all null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_Unregisters_PrestigeChannel()
        {
            var ctrl = CreateCtrl();
            var ch   = CreateEvent();
            SetField(ctrl, "_onPrestige", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, count,
                "OnDisable must unregister from _onPrestige.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnDisable_Unregisters_MasteryChannel()
        {
            var ctrl = CreateCtrl();
            var ch   = CreateEvent();
            SetField(ctrl, "_onMasteryUnlocked", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, count,
                "OnDisable must unregister from _onMasteryUnlocked.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        // ── Refresh — null bonus ──────────────────────────────────────────────

        [Test]
        public void Ctrl_Refresh_NullBonus_PrestigeLabelShowsOne()
        {
            var ctrl  = CreateCtrl();
            var label = CreateText();
            SetField(ctrl, "_prestigeLabel", label);

            ctrl.Refresh();

            StringAssert.Contains("1.00", label.text,
                "Prestige label must show ×1.00 when _combinedBonus is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(label.gameObject);
        }

        [Test]
        public void Ctrl_Refresh_NullBonus_MasteryLabelShowsOne()
        {
            var ctrl  = CreateCtrl();
            var label = CreateText();
            SetField(ctrl, "_masteryLabel", label);

            ctrl.Refresh();

            StringAssert.Contains("1.00", label.text,
                "Mastery label must show ×1.00 when _combinedBonus is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(label.gameObject);
        }

        [Test]
        public void Ctrl_Refresh_NullBonus_TotalLabelShowsOne()
        {
            var ctrl  = CreateCtrl();
            var label = CreateText();
            SetField(ctrl, "_totalLabel", label);

            ctrl.Refresh();

            StringAssert.Contains("1.00", label.text,
                "Total label must show ×1.00 when _combinedBonus is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(label.gameObject);
        }

        // ── Refresh — null UI safety ──────────────────────────────────────────

        [Test]
        public void Ctrl_Refresh_NullPanel_DoesNotThrow()
        {
            var ctrl = CreateCtrl();
            // _panel is null by default
            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh with null _panel must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_Refresh_NullLabels_DoesNotThrow()
        {
            var ctrl = CreateCtrl();
            // All text labels are null by default
            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh with null text labels must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        // ── Channel triggers ──────────────────────────────────────────────────

        [Test]
        public void Ctrl_OnPrestige_TriggersRefresh()
        {
            var ctrl  = CreateCtrl();
            var ch    = CreateEvent();
            var label = CreateText();
            SetField(ctrl, "_onPrestige",    ch);
            SetField(ctrl, "_totalLabel",    label);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            label.text = string.Empty;
            ch.Raise();

            Assert.IsFalse(string.IsNullOrEmpty(label.text),
                "Raising _onPrestige must trigger Refresh and update _totalLabel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
            Object.DestroyImmediate(label.gameObject);
        }

        [Test]
        public void Ctrl_OnMasteryUnlocked_TriggersRefresh()
        {
            var ctrl  = CreateCtrl();
            var ch    = CreateEvent();
            var label = CreateText();
            SetField(ctrl, "_onMasteryUnlocked", ch);
            SetField(ctrl, "_totalLabel",        label);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            label.text = string.Empty;
            ch.Raise();

            Assert.IsFalse(string.IsNullOrEmpty(label.text),
                "Raising _onMasteryUnlocked must trigger Refresh and update _totalLabel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
            Object.DestroyImmediate(label.gameObject);
        }

        // ── Property ──────────────────────────────────────────────────────────

        [Test]
        public void Ctrl_CombinedBonus_Property_ReturnsAssigned()
        {
            var ctrl = CreateCtrl();
            var so   = ScriptableObject.CreateInstance<CombinedBonusCalculatorSO>();
            SetField(ctrl, "_combinedBonus", so);

            Assert.AreSame(so, ctrl.CombinedBonus,
                "CombinedBonus property must return the assigned SO.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }

        // ── Refresh with assigned bonus ───────────────────────────────────────

        [Test]
        public void Ctrl_Refresh_WithBonus_PrestigeLabelFormatted()
        {
            var ctrl       = CreateCtrl();
            var bonusSO    = ScriptableObject.CreateInstance<CombinedBonusCalculatorSO>();
            var scoreMult  = ScriptableObject.CreateInstance<ScoreMultiplierSO>();
            var label      = CreateText();

            // Set multiplier to 1.5 via SetMultiplier
            scoreMult.SetMultiplier(1.5f);
            SetField(bonusSO, "_scoreMultiplier", scoreMult);
            SetField(ctrl, "_combinedBonus",  bonusSO);
            SetField(ctrl, "_prestigeLabel",  label);

            ctrl.Refresh();

            StringAssert.Contains("1.50", label.text,
                "Prestige label must reflect the ScoreMultiplierSO value.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(bonusSO);
            Object.DestroyImmediate(scoreMult);
            Object.DestroyImmediate(label.gameObject);
        }
    }
}
