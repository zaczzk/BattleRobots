using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MomentumHUDController"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all-null refs — no exception.
    ///   • Refresh with null SO — hides panel.
    ///   • Refresh with valid SO — shows panel and sets Slider value.
    ///   • OnDisable unregisters from the _onMomentumChanged channel.
    ///   • OnMomentumChanged raise triggers a Refresh call.
    /// </summary>
    public class MomentumHUDControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static MatchMomentumSO CreateMomentumSO(float current = 0f, float max = 100f)
        {
            var so = ScriptableObject.CreateInstance<MatchMomentumSO>();
            SetField(so, "_maxMomentum", max);
            SetField(so, "_decayRate",   0f);   // no decay in tests
            InvokePrivate(so, "OnEnable");       // zero momentum
            if (current > 0f)
                so.AddMomentum(current);
            return so;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            var go  = new GameObject();
            var hud = go.AddComponent<MomentumHUDController>();
            SetField(hud, "_momentumSO",         null);
            SetField(hud, "_onMomentumChanged",  null);
            SetField(hud, "_momentumBar",         null);
            SetField(hud, "_momentumLabel",       null);
            SetField(hud, "_momentumPanel",       null);

            InvokePrivate(hud, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(hud, "OnEnable"));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            var go  = new GameObject();
            var hud = go.AddComponent<MomentumHUDController>();
            SetField(hud, "_momentumSO",         null);
            SetField(hud, "_onMomentumChanged",  null);
            SetField(hud, "_momentumBar",         null);
            SetField(hud, "_momentumLabel",       null);
            SetField(hud, "_momentumPanel",       null);

            InvokePrivate(hud, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(hud, "OnDisable"));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Refresh_NullSO_HidesPanel()
        {
            var go     = new GameObject();
            var hud    = go.AddComponent<MomentumHUDController>();
            var panel  = new GameObject("Panel");

            SetField(hud, "_momentumSO",    null);
            SetField(hud, "_momentumPanel", panel);
            panel.SetActive(true);

            InvokePrivate(hud, "Awake");
            hud.Refresh();

            Assert.IsFalse(panel.activeSelf);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_WithSO_ShowsPanelAndSetsSlider()
        {
            var go      = new GameObject();
            var hud     = go.AddComponent<MomentumHUDController>();
            var panel   = new GameObject("Panel");
            var sliderGo = new GameObject("Slider");
            var slider  = sliderGo.AddComponent<Slider>();
            var so      = CreateMomentumSO(current: 50f, max: 100f);

            SetField(hud, "_momentumSO",    so);
            SetField(hud, "_momentumPanel", panel);
            SetField(hud, "_momentumBar",   slider);
            panel.SetActive(false);

            InvokePrivate(hud, "Awake");
            hud.Refresh();

            Assert.IsTrue(panel.activeSelf);
            Assert.AreEqual(0.5f, slider.value, 0.001f);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(sliderGo);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void OnDisable_UnregistersFromChannel()
        {
            var go      = new GameObject();
            var hud     = go.AddComponent<MomentumHUDController>();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();

            SetField(hud, "_momentumSO",        null);
            SetField(hud, "_onMomentumChanged", channel);

            InvokePrivate(hud, "Awake");
            InvokePrivate(hud, "OnEnable");
            InvokePrivate(hud, "OnDisable");

            // After unsubscribe, raising the channel must not throw.
            Assert.DoesNotThrow(() => channel.Raise());

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void OnMomentumChanged_Raise_TriggersRefresh_ShowsPanel()
        {
            var go      = new GameObject();
            var hud     = go.AddComponent<MomentumHUDController>();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            var panel   = new GameObject("Panel");
            var so      = CreateMomentumSO(current: 10f, max: 100f);

            SetField(hud, "_momentumSO",        so);
            SetField(hud, "_onMomentumChanged", channel);
            SetField(hud, "_momentumPanel",     panel);
            panel.SetActive(false);

            InvokePrivate(hud, "Awake");
            InvokePrivate(hud, "OnEnable");

            // OnEnable already calls Refresh once; reset panel to test event-driven path.
            panel.SetActive(false);
            channel.Raise();

            Assert.IsTrue(panel.activeSelf);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(so);
        }
    }
}
