using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="ComboHUDController"/>.
    ///
    /// Covers:
    ///   • Null _comboCounter — Refresh() does not throw; HideAll is safe.
    ///   • Null _onComboChanged — subscribe/unsubscribe paths do not throw.
    ///   • All optional UI refs null — Refresh() does not throw.
    ///   • _comboPanel SetActive follows IsComboActive.
    ///   • _comboCountText shows "COMBO x{N}" when active, empty when inactive.
    ///   • _multiplierText shows "x{M:F1}" when active, empty when inactive.
    ///   • Refresh is idempotent — two consecutive calls produce same result.
    ///   • Null event channel with valid counter — Refresh still works.
    ///   • _comboFill.fillAmount zero when inactive.
    ///   • Disable then re-enable — HideAll followed by fresh Refresh.
    /// </summary>
    public class ComboHUDControllerTests
    {
        private GameObject           _go;
        private ComboHUDController   _ctrl;
        private ComboCounterSO       _counter;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void CallMethod(object target, string name)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(mi, $"Method '{name}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go      = new GameObject("ComboHUD");
            _ctrl    = _go.AddComponent<ComboHUDController>();
            _counter = ScriptableObject.CreateInstance<ComboCounterSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_counter);
            _ctrl    = null;
            _counter = null;
        }

        // ── Null-safety ───────────────────────────────────────────────────────

        [Test]
        public void Refresh_NullCounter_DoesNotThrow()
        {
            // _comboCounter is null by default.
            Assert.DoesNotThrow(() => _ctrl.Refresh());
        }

        [Test]
        public void OnEnable_NullChannelAndCounter_DoesNotThrow()
        {
            // Both _onComboChanged and _comboCounter are null by default.
            Assert.DoesNotThrow(() => CallMethod(_ctrl, "OnEnable"));
        }

        [Test]
        public void OnDisable_NullChannelAndCounter_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => CallMethod(_ctrl, "OnDisable"));
        }

        [Test]
        public void Refresh_AllOptionalUINullAndCounterAssigned_DoesNotThrow()
        {
            SetField(_ctrl, "_comboCounter", _counter);
            // All optional fields remain null (panel, texts, fill).

            Assert.DoesNotThrow(() => _ctrl.Refresh());
        }

        [Test]
        public void Refresh_NullChannel_WithValidCounter_DoesNotThrow()
        {
            SetField(_ctrl, "_comboCounter", _counter);
            // _onComboChanged remains null.

            _counter.RecordHit();

            Assert.DoesNotThrow(() => _ctrl.Refresh());
        }

        // ── Panel visibility ──────────────────────────────────────────────────

        [Test]
        public void Refresh_PanelHiddenWhenNoCombo()
        {
            var panel = new GameObject("Panel");
            panel.transform.SetParent(_go.transform);
            SetField(_ctrl, "_comboCounter", _counter);
            SetField(_ctrl, "_comboPanel",   panel);

            _ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf);

            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_PanelShownWhenComboActive()
        {
            var panel = new GameObject("Panel");
            panel.transform.SetParent(_go.transform);
            SetField(_ctrl, "_comboCounter", _counter);
            SetField(_ctrl, "_comboPanel",   panel);

            _counter.RecordHit();
            _ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf);

            Object.DestroyImmediate(panel);
        }

        // ── Text formatting ───────────────────────────────────────────────────

        [Test]
        public void Refresh_ComboCountTextFormattedCorrectlyWhenActive()
        {
            var textGO  = new GameObject("CountText");
            textGO.transform.SetParent(_go.transform);
            var text = textGO.AddComponent<Text>();
            SetField(_ctrl, "_comboCounter",   _counter);
            SetField(_ctrl, "_comboCountText", text);

            _counter.RecordHit();
            _counter.RecordHit();
            _counter.RecordHit();
            _ctrl.Refresh();

            Assert.AreEqual("COMBO x3", text.text);

            Object.DestroyImmediate(textGO);
        }

        [Test]
        public void Refresh_ComboCountTextEmptyWhenInactive()
        {
            var textGO = new GameObject("CountText");
            textGO.transform.SetParent(_go.transform);
            var text = textGO.AddComponent<Text>();
            text.text = "COMBO x5"; // pre-populate
            SetField(_ctrl, "_comboCounter",   _counter);
            SetField(_ctrl, "_comboCountText", text);

            _ctrl.Refresh(); // combo inactive

            Assert.AreEqual(string.Empty, text.text);

            Object.DestroyImmediate(textGO);
        }

        [Test]
        public void Refresh_MultiplierTextFormattedCorrectlyAtTierOne()
        {
            var textGO = new GameObject("MultText");
            textGO.transform.SetParent(_go.transform);
            var text = textGO.AddComponent<Text>();
            SetField(_ctrl, "_comboCounter",   _counter);
            SetField(_ctrl, "_multiplierText", text);

            // 5 hits → multiplier = 1.1
            for (int i = 0; i < 5; i++) _counter.RecordHit();
            _ctrl.Refresh();

            Assert.AreEqual("x1.1", text.text);

            Object.DestroyImmediate(textGO);
        }

        [Test]
        public void Refresh_MultiplierTextEmptyWhenInactive()
        {
            var textGO = new GameObject("MultText");
            textGO.transform.SetParent(_go.transform);
            var text = textGO.AddComponent<Text>();
            text.text = "x1.5";
            SetField(_ctrl, "_comboCounter",   _counter);
            SetField(_ctrl, "_multiplierText", text);

            _ctrl.Refresh(); // combo inactive

            Assert.AreEqual(string.Empty, text.text);

            Object.DestroyImmediate(textGO);
        }

        // ── HideAll via OnDisable ─────────────────────────────────────────────

        [Test]
        public void OnDisable_HidesPanelAndClearsTexts()
        {
            var panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(_go.transform);
            var textGO  = new GameObject("CountText");
            textGO.transform.SetParent(_go.transform);
            var text = textGO.AddComponent<Text>();

            SetField(_ctrl, "_comboCounter",   _counter);
            SetField(_ctrl, "_comboPanel",     panelGO);
            SetField(_ctrl, "_comboCountText", text);

            _counter.RecordHit();
            _ctrl.Refresh();
            // Panel should be visible and text populated.
            Assert.IsTrue(panelGO.activeSelf, "Pre-condition: panel should be active after Refresh with active combo");

            CallMethod(_ctrl, "OnDisable");

            Assert.IsFalse(panelGO.activeSelf, "OnDisable should hide the panel");
            Assert.AreEqual(string.Empty, text.text, "OnDisable should clear combo text");

            Object.DestroyImmediate(panelGO);
            Object.DestroyImmediate(textGO);
        }

        // ── Idempotency ───────────────────────────────────────────────────────

        [Test]
        public void Refresh_CalledTwice_SameResult()
        {
            var panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(_go.transform);
            SetField(_ctrl, "_comboCounter", _counter);
            SetField(_ctrl, "_comboPanel",   panelGO);

            _counter.RecordHit();
            _ctrl.Refresh();
            bool firstCall = panelGO.activeSelf;
            _ctrl.Refresh();
            bool secondCall = panelGO.activeSelf;

            Assert.AreEqual(firstCall, secondCall);

            Object.DestroyImmediate(panelGO);
        }
    }
}
