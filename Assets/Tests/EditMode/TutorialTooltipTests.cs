using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // TutorialTooltipConfig tests
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// EditMode tests for <see cref="TutorialTooltipConfig"/>.
    ///
    /// Covers:
    ///   • FreshInstance: Entries is non-null and empty.
    ///   • Entries returns IReadOnlyList.
    ///   • GetTooltipForTag: null / whitespace → string.Empty.
    ///   • GetTooltipForTag: no matching entry → string.Empty.
    ///   • GetTooltipForTag: single matching entry → correct tooltip text.
    ///   • GetTooltipForTag: multiple entries → correct entry returned.
    ///   • GetTooltipForTag: null entry in list → silently skipped, no throw.
    ///   • GetTooltipForTag: entry with null tooltipText → returns string.Empty.
    ///   • GetTooltipForTag: empty tooltipText → returns string.Empty (not null).
    ///
    /// All tests run headless (no Unity Editor scene required).
    /// </summary>
    public class TutorialTooltipConfigTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── SetUp / TearDown ──────────────────────────────────────────────────

        private TutorialTooltipConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<TutorialTooltipConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_Entries_NotNull()
        {
            Assert.IsNotNull(_config.Entries,
                "Fresh TutorialTooltipConfig.Entries must not be null.");
        }

        [Test]
        public void FreshInstance_Entries_IsEmpty()
        {
            Assert.AreEqual(0, _config.Entries.Count,
                "Fresh TutorialTooltipConfig.Entries must be empty.");
        }

        [Test]
        public void Entries_ReturnsIReadOnlyList()
        {
            Assert.IsInstanceOf<IReadOnlyList<TutorialTooltipEntry>>(_config.Entries,
                "Entries must implement IReadOnlyList<TutorialTooltipEntry>.");
        }

        // ── GetTooltipForTag — null / whitespace guards ───────────────────────

        [Test]
        public void GetTooltipForTag_Null_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, _config.GetTooltipForTag(null),
                "GetTooltipForTag(null) must return string.Empty.");
        }

        [Test]
        public void GetTooltipForTag_Whitespace_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, _config.GetTooltipForTag("   "),
                "GetTooltipForTag with whitespace-only input must return string.Empty.");
        }

        // ── GetTooltipForTag — no match ───────────────────────────────────────

        [Test]
        public void GetTooltipForTag_NoMatch_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, _config.GetTooltipForTag("UnknownPanel"),
                "GetTooltipForTag with no matching entry must return string.Empty.");
        }

        // ── GetTooltipForTag — happy path ─────────────────────────────────────

        [Test]
        public void GetTooltipForTag_SingleEntry_ReturnsTooltipText()
        {
            var entry = new TutorialTooltipEntry
                { panelTag = "ShopPanel", tooltipText = "Visit the shop to buy parts!" };
            SetField(_config, "_entries", new List<TutorialTooltipEntry> { entry });

            Assert.AreEqual("Visit the shop to buy parts!", _config.GetTooltipForTag("ShopPanel"),
                "GetTooltipForTag must return the configured tooltip text for a matching entry.");
        }

        [Test]
        public void GetTooltipForTag_MultipleEntries_ReturnsCorrectTooltip()
        {
            var e1 = new TutorialTooltipEntry { panelTag = "ShopPanel",  tooltipText = "Shop hint" };
            var e2 = new TutorialTooltipEntry { panelTag = "ArenaPanel", tooltipText = "Arena hint" };
            SetField(_config, "_entries", new List<TutorialTooltipEntry> { e1, e2 });

            Assert.AreEqual("Arena hint", _config.GetTooltipForTag("ArenaPanel"),
                "GetTooltipForTag must return the correct tooltip when multiple entries are present.");
        }

        // ── GetTooltipForTag — edge cases ─────────────────────────────────────

        [Test]
        public void GetTooltipForTag_NullEntryInList_DoesNotThrow()
        {
            var entries = new List<TutorialTooltipEntry> { null };
            SetField(_config, "_entries", entries);

            Assert.DoesNotThrow(() => _config.GetTooltipForTag("ShopPanel"),
                "GetTooltipForTag must silently skip null list entries without throwing.");
        }

        [Test]
        public void GetTooltipForTag_EntryWithEmptyTooltipText_ReturnsEmpty()
        {
            var entry = new TutorialTooltipEntry { panelTag = "ShopPanel", tooltipText = "" };
            SetField(_config, "_entries", new List<TutorialTooltipEntry> { entry });

            Assert.AreEqual(string.Empty, _config.GetTooltipForTag("ShopPanel"),
                "GetTooltipForTag must return string.Empty when the matched entry's tooltipText is empty.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TutorialTooltipController tests
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// EditMode tests for <see cref="TutorialTooltipController"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all-null refs → no throw.
    ///   • OnEnable with empty _panelTag → hides tooltip panel.
    ///   • OnEnable with unseen tag → shows tooltip panel (first visit path).
    ///   • OnEnable with unseen tag + config → populates tooltip text.
    ///   • OnEnable with unseen tag + null config → shows panel (no throw, no text set).
    ///   • OnEnable with unseen tag + null _tooltipText → does not throw.
    ///   • Dismiss with empty _panelTag → no-op, does not throw.
    ///   • Dismiss with valid tag → hides tooltip panel.
    ///   • Dismiss with valid tag → persists tag (re-enabling hides panel).
    ///   • Hide → sets tooltip panel inactive.
    ///   • Awake wires _dismissButton onClick listener.
    ///
    /// SetUp/TearDown call SaveSystem.Delete() to prevent test pollution.
    /// All tests run headless (no Unity Editor scene required).
    /// </summary>
    public class TutorialTooltipControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static TutorialTooltipController MakeController(out GameObject go)
        {
            go = new GameObject("TutorialTooltipControllerTest");
            go.SetActive(false); // prevent OnEnable before fields are set
            return go.AddComponent<TutorialTooltipController>();
        }

        // ── SetUp / TearDown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            SaveSystem.Delete();
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();
        }

        // ── OnEnable / OnDisable — null guards ────────────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        // ── OnEnable — empty tag hides tooltip ────────────────────────────────

        [Test]
        public void OnEnable_EmptyPanelTag_HidesTooltipPanel()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<TutorialTooltipController>();
            var panelGo = new GameObject("TooltipPanel");
            panelGo.SetActive(true);

            SetField(ctrl, "_tooltipPanel", panelGo);
            // _panelTag remains "" (default) → treated as empty

            go.SetActive(true);

            Assert.IsFalse(panelGo.activeSelf,
                "Tooltip panel must be hidden when _panelTag is empty.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
        }

        // ── OnEnable — first visit shows tooltip ──────────────────────────────

        [Test]
        public void OnEnable_UnseenTag_ShowsTooltipPanel()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<TutorialTooltipController>();
            var panelGo = new GameObject("TooltipPanel");
            panelGo.SetActive(false);

            SetField(ctrl, "_panelTag",    "ShopPanel");
            SetField(ctrl, "_tooltipPanel", panelGo);
            // No save file → seenTooltipPanelTags is empty → first visit

            go.SetActive(true);

            Assert.IsTrue(panelGo.activeSelf,
                "Tooltip panel must be shown on the first visit (tag not in seen-set).");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
        }

        [Test]
        public void OnEnable_UnseenTag_WithConfig_PopulatesText()
        {
            var config = ScriptableObject.CreateInstance<TutorialTooltipConfig>();
            var entry  = new TutorialTooltipEntry
                { panelTag = "ShopPanel", tooltipText = "Welcome to the shop!" };
            SetField(config, "_entries", new List<TutorialTooltipEntry> { entry });

            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<TutorialTooltipController>();
            var textGo = new GameObject("TextGo");
            var label  = textGo.AddComponent<Text>();

            SetField(ctrl, "_panelTag",   "ShopPanel");
            SetField(ctrl, "_config",     config);
            SetField(ctrl, "_tooltipText", label);

            go.SetActive(true);

            Assert.AreEqual("Welcome to the shop!", label.text,
                "OnEnable must populate _tooltipText from the config on first visit.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGo);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void OnEnable_UnseenTag_NullConfig_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<TutorialTooltipController>();
            var panelGo = new GameObject("TooltipPanel");

            SetField(ctrl, "_panelTag",    "ArenaPanel");
            SetField(ctrl, "_tooltipPanel", panelGo);
            // _config remains null → should not throw, panel shown with no text

            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with null _config must not throw on first visit.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
        }

        [Test]
        public void OnEnable_UnseenTag_NullTooltipText_DoesNotThrow()
        {
            var config = ScriptableObject.CreateInstance<TutorialTooltipConfig>();
            var entry  = new TutorialTooltipEntry
                { panelTag = "LoadoutPanel", tooltipText = "Equip parts here." };
            SetField(config, "_entries", new List<TutorialTooltipEntry> { entry });

            MakeController(out GameObject go);
            var ctrl = go.GetComponent<TutorialTooltipController>();
            SetField(ctrl, "_panelTag", "LoadoutPanel");
            SetField(ctrl, "_config",   config);
            // _tooltipText remains null → no assignment, but must not throw

            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with null _tooltipText must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(config);
        }

        // ── Dismiss — guards ──────────────────────────────────────────────────

        [Test]
        public void Dismiss_EmptyPanelTag_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<TutorialTooltipController>();
            go.SetActive(true);
            // _panelTag is "" → Dismiss must be a no-op

            Assert.DoesNotThrow(() => ctrl.Dismiss(),
                "Dismiss with empty _panelTag must not throw.");

            Object.DestroyImmediate(go);
        }

        // ── Dismiss — happy path ──────────────────────────────────────────────

        [Test]
        public void Dismiss_ValidTag_HidesTooltipPanel()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<TutorialTooltipController>();
            var panelGo = new GameObject("TooltipPanel");
            panelGo.SetActive(true);

            SetField(ctrl, "_panelTag",    "CareerPanel");
            SetField(ctrl, "_tooltipPanel", panelGo);

            go.SetActive(true); // shows tooltip (first visit)
            Assert.IsTrue(panelGo.activeSelf); // confirm it's shown

            ctrl.Dismiss();

            Assert.IsFalse(panelGo.activeSelf,
                "Dismiss must hide the tooltip panel.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
        }

        [Test]
        public void Dismiss_ValidTag_PersistedTag_ReenableHidesPanel()
        {
            // First controller: show then dismiss → persists tag in save file.
            MakeController(out GameObject go1);
            var ctrl1    = go1.GetComponent<TutorialTooltipController>();
            var panelGo1 = new GameObject("TooltipPanel1");

            SetField(ctrl1, "_panelTag",    "SettingsPanel");
            SetField(ctrl1, "_tooltipPanel", panelGo1);

            go1.SetActive(true);  // first visit → shown
            ctrl1.Dismiss();      // marks seen + saves

            Object.DestroyImmediate(go1);
            Object.DestroyImmediate(panelGo1);

            // Second controller with the same tag and a new panel → must be hidden.
            MakeController(out GameObject go2);
            var ctrl2    = go2.GetComponent<TutorialTooltipController>();
            var panelGo2 = new GameObject("TooltipPanel2");
            panelGo2.SetActive(true); // start visible

            SetField(ctrl2, "_panelTag",    "SettingsPanel");
            SetField(ctrl2, "_tooltipPanel", panelGo2);

            go2.SetActive(true); // tag is now seen → must hide

            Assert.IsFalse(panelGo2.activeSelf,
                "Re-enabling a controller for an already-dismissed panel must hide the tooltip.");

            Object.DestroyImmediate(go2);
            Object.DestroyImmediate(panelGo2);
        }

        // ── Hide ──────────────────────────────────────────────────────────────

        [Test]
        public void Hide_SetsTooltipPanelInactive()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<TutorialTooltipController>();
            var panelGo = new GameObject("TooltipPanel");
            panelGo.SetActive(true);

            SetField(ctrl, "_tooltipPanel", panelGo);
            go.SetActive(true);

            ctrl.Hide();

            Assert.IsFalse(panelGo.activeSelf,
                "Hide() must set the tooltip panel inactive.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
        }

        // ── Awake wiring ──────────────────────────────────────────────────────

        [Test]
        public void Awake_WiresDismissButton_InvokingHidesTooltipPanel()
        {
            MakeController(out GameObject go);
            var ctrl  = go.GetComponent<TutorialTooltipController>();

            var btnGo = new GameObject("DismissBtn");
            btnGo.AddComponent<Image>(); // Button requires a Graphic
            var btn   = btnGo.AddComponent<Button>();

            var panelGo = new GameObject("TooltipPanel");
            panelGo.SetActive(true);

            SetField(ctrl, "_panelTag",    "TestWiringPanel");
            SetField(ctrl, "_dismissButton", btn);
            SetField(ctrl, "_tooltipPanel",  panelGo);

            go.SetActive(true); // Awake fires → wires btn.onClick → Dismiss

            // Invoking the button must call Dismiss → Hide → panel goes inactive.
            btn.onClick.Invoke();

            Assert.IsFalse(panelGo.activeSelf,
                "Invoking _dismissButton.onClick must call Dismiss() and hide the tooltip panel.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(btnGo);
            Object.DestroyImmediate(panelGo);
        }
    }
}
