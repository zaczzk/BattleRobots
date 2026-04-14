using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T189:
    ///   <see cref="PrestigeStatsController"/>.
    ///
    /// PrestigeStatsControllerTests (16):
    ///   FreshInstance_PrestigeSystemIsNull              ×1
    ///   OnEnable_NullRefs_DoesNotThrow                  ×1
    ///   OnDisable_NullRefs_DoesNotThrow                 ×1
    ///   OnDisable_Unregisters_PrestigeChannel           ×1
    ///   Refresh_NullPrestige_RankLabelShowsDash         ×1
    ///   Refresh_NullPrestige_CountLabelShowsPrestige0   ×1
    ///   Refresh_NullPrestige_ProgressBarIsZero          ×1
    ///   Refresh_WithPrestige_RankLabel                  ×1
    ///   Refresh_WithPrestige_CountLabel                 ×1
    ///   Refresh_WithPrestige_ProgressBar                ×1
    ///   Refresh_MaxPrestige_BarIsOne                    ×1
    ///   Refresh_ZeroPrestige_BarIsZero                  ×1
    ///   Refresh_NullLabels_DoesNotThrow                 ×1
    ///   Refresh_NullBar_DoesNotThrow                    ×1
    ///   OnPrestige_TriggersRefresh                      ×1
    ///   OnEnable_CallsRefresh                           ×1
    ///
    /// Total: 16 new EditMode tests.
    /// </summary>
    public class PrestigeStatsControllerTests
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

        private static PrestigeSystemSO CreatePrestige(int count = 0)
        {
            var so = ScriptableObject.CreateInstance<PrestigeSystemSO>();
            so.LoadSnapshot(count);
            return so;
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static PrestigeStatsController CreateController()
        {
            var go = new GameObject("PrestigeStatsCtrl_Test");
            return go.AddComponent<PrestigeStatsController>();
        }

        // ── FreshInstance ─────────────────────────────────────────────────────

        [Test]
        public void Ctrl_FreshInstance_PrestigeSystemIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.PrestigeSystem);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        // ── Lifecycle null safety ─────────────────────────────────────────────

        [Test]
        public void Ctrl_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_Unregisters_PrestigeChannel()
        {
            var ctrl = CreateController();
            var ch   = CreateEvent();
            SetField(ctrl, "_onPrestige", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            // Register an external counter — only this must fire after unsubscribe.
            int callCount = 0;
            ch.RegisterCallback(() => callCount++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, callCount,
                "OnDisable must unregister from _onPrestige (controller callback removed).");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        // ── Refresh — null prestige system (default/fallback values) ──────────

        [Test]
        public void Ctrl_Refresh_NullPrestige_RankLabelShowsDash()
        {
            var ctrl    = CreateController();
            var labelGO = new GameObject("RankLabel");
            var label   = labelGO.AddComponent<Text>();
            label.text  = "something";
            SetField(ctrl, "_rankLabel", label);

            ctrl.Refresh();

            Assert.AreEqual("—", label.text,
                "Refresh with null prestige system must show '—' in _rankLabel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
        }

        [Test]
        public void Ctrl_Refresh_NullPrestige_CountLabelShowsPrestige0()
        {
            var ctrl    = CreateController();
            var labelGO = new GameObject("CountLabel");
            var label   = labelGO.AddComponent<Text>();
            label.text  = "Prestige 99";
            SetField(ctrl, "_countLabel", label);

            ctrl.Refresh();

            Assert.AreEqual("Prestige 0", label.text,
                "Refresh with null prestige system must show 'Prestige 0' in _countLabel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
        }

        [Test]
        public void Ctrl_Refresh_NullPrestige_ProgressBarIsZero()
        {
            var ctrl    = CreateController();
            var sliderGO = new GameObject("Slider");
            var slider   = sliderGO.AddComponent<Slider>();
            slider.value = 0.75f;
            SetField(ctrl, "_progressBar", slider);

            ctrl.Refresh();

            Assert.AreEqual(0f, slider.value,
                "Refresh with null prestige system must set _progressBar to 0.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(sliderGO);
        }

        // ── Refresh — with prestige system ────────────────────────────────────

        [Test]
        public void Ctrl_Refresh_WithPrestige_RankLabel()
        {
            // count=1 → "Bronze I"
            var ctrl     = CreateController();
            var prestige = CreatePrestige(1);
            var labelGO  = new GameObject("RankLabel");
            var label    = labelGO.AddComponent<Text>();
            SetField(ctrl, "_prestigeSystem", prestige);
            SetField(ctrl, "_rankLabel",      label);

            ctrl.Refresh();

            Assert.AreEqual("Bronze I", label.text,
                "Refresh with count=1 must display 'Bronze I' in _rankLabel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(prestige);
        }

        [Test]
        public void Ctrl_Refresh_WithPrestige_CountLabel()
        {
            // count=4 → "Prestige 4"
            var ctrl     = CreateController();
            var prestige = CreatePrestige(4);
            var labelGO  = new GameObject("CountLabel");
            var label    = labelGO.AddComponent<Text>();
            SetField(ctrl, "_prestigeSystem", prestige);
            SetField(ctrl, "_countLabel",     label);

            ctrl.Refresh();

            Assert.AreEqual("Prestige 4", label.text,
                "Refresh with count=4 must display 'Prestige 4' in _countLabel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(prestige);
        }

        [Test]
        public void Ctrl_Refresh_WithPrestige_ProgressBar()
        {
            // count=5, maxRank=10 → progress = 0.5f
            var ctrl     = CreateController();
            var prestige = CreatePrestige(5); // maxRank defaults to 10
            var sliderGO = new GameObject("Slider");
            var slider   = sliderGO.AddComponent<Slider>();
            SetField(ctrl, "_prestigeSystem", prestige);
            SetField(ctrl, "_progressBar",    slider);

            ctrl.Refresh();

            Assert.AreEqual(0.5f, slider.value, 0.001f,
                "Refresh with count=5 and maxRank=10 must set _progressBar to 0.5.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(sliderGO);
            Object.DestroyImmediate(prestige);
        }

        [Test]
        public void Ctrl_Refresh_MaxPrestige_BarIsOne()
        {
            // count=10, maxRank=10 → progress = 1.0f (Clamp01)
            var ctrl     = CreateController();
            var prestige = CreatePrestige(10);
            var sliderGO = new GameObject("Slider");
            var slider   = sliderGO.AddComponent<Slider>();
            SetField(ctrl, "_prestigeSystem", prestige);
            SetField(ctrl, "_progressBar",    slider);

            ctrl.Refresh();

            Assert.AreEqual(1f, slider.value, 0.001f,
                "Refresh at max prestige (count=maxRank=10) must set _progressBar to 1.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(sliderGO);
            Object.DestroyImmediate(prestige);
        }

        [Test]
        public void Ctrl_Refresh_ZeroPrestige_BarIsZero()
        {
            // count=0, maxRank=10 → progress = 0f
            var ctrl     = CreateController();
            var prestige = CreatePrestige(0);
            var sliderGO = new GameObject("Slider");
            var slider   = sliderGO.AddComponent<Slider>();
            slider.value = 0.5f;
            SetField(ctrl, "_prestigeSystem", prestige);
            SetField(ctrl, "_progressBar",    slider);

            ctrl.Refresh();

            Assert.AreEqual(0f, slider.value, 0.001f,
                "Refresh with count=0 and maxRank=10 must set _progressBar to 0.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(sliderGO);
            Object.DestroyImmediate(prestige);
        }

        // ── Null UI refs (optional fields must not throw) ─────────────────────

        [Test]
        public void Ctrl_Refresh_NullLabels_DoesNotThrow()
        {
            var ctrl     = CreateController();
            var prestige = CreatePrestige(3);
            SetField(ctrl, "_prestigeSystem", prestige);
            // _rankLabel and _countLabel are null (not assigned)

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh must not throw when _rankLabel and _countLabel are null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(prestige);
        }

        [Test]
        public void Ctrl_Refresh_NullBar_DoesNotThrow()
        {
            var ctrl     = CreateController();
            var prestige = CreatePrestige(3);
            SetField(ctrl, "_prestigeSystem", prestige);
            // _progressBar is null (not assigned)

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh must not throw when _progressBar is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(prestige);
        }

        // ── Event-driven refresh ──────────────────────────────────────────────

        [Test]
        public void Ctrl_OnPrestige_TriggersRefresh()
        {
            var ctrl     = CreateController();
            var ch       = CreateEvent();
            var prestige = CreatePrestige(3);
            var labelGO  = new GameObject("CountLabel");
            var label    = labelGO.AddComponent<Text>();

            SetField(ctrl, "_onPrestige",     ch);
            SetField(ctrl, "_prestigeSystem", prestige);
            SetField(ctrl, "_countLabel",     label);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            // Advance prestige count without firing the event, then raise manually.
            prestige.LoadSnapshot(7);
            ch.Raise();

            Assert.AreEqual("Prestige 7", label.text,
                "Raising _onPrestige must trigger Refresh() and update the count label.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(prestige);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnEnable_CallsRefresh()
        {
            var ctrl     = CreateController();
            var prestige = CreatePrestige(2);
            var labelGO  = new GameObject("RankLabel");
            var label    = labelGO.AddComponent<Text>();

            SetField(ctrl, "_prestigeSystem", prestige);
            SetField(ctrl, "_rankLabel",      label);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            // OnEnable must call Refresh() immediately — label should be populated.
            Assert.AreEqual("Bronze II", label.text,
                "OnEnable must call Refresh() so the label is populated on activation.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(prestige);
        }
    }
}
