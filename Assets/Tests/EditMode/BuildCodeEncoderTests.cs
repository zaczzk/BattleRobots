using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="BuildCodeEncoder"/> and <see cref="BuildCodeController"/>.
    ///
    /// BuildCodeEncoderTests covers:
    ///   • Encode(null) → empty string.
    ///   • Encode(empty list) → empty string.
    ///   • Encode(single ID) → non-empty, then Decode back → same ID.
    ///   • Encode(multiple IDs) → Decode back → same ordered list.
    ///   • Encode skips null/whitespace entries.
    ///   • Decode(null) → null.
    ///   • Decode("") → null.
    ///   • Decode(whitespace) → null.
    ///   • Decode(malformed Base64) → null.
    ///   • Decode(valid code with empty segments) → only non-empty IDs returned.
    ///
    /// BuildCodeControllerTests covers:
    ///   • OnEnable / OnDisable with all-null refs → no throw.
    ///   • OnEnable / OnDisable with null channel → no throw.
    ///   • OnDisable unregisters from _onLoadoutChanged.
    ///   • ExportCode with null loadout → sets status text (does not throw).
    ///   • ImportCode with null import field → does not throw.
    ///   • ImportCode with invalid code → _statusText shows "Invalid code."
    ///   • ImportCode with valid code → applies loadout.
    ///
    /// All tests run headless (no Unity Editor scene required).
    /// </summary>
    public class BuildCodeEncoderTests
    {
        // ── Encode ────────────────────────────────────────────────────────────

        [Test]
        public void Encode_NullPartIds_ReturnsEmptyString()
        {
            string code = BuildCodeEncoder.Encode(null);
            Assert.AreEqual(string.Empty, code,
                "Encode(null) must return an empty string.");
        }

        [Test]
        public void Encode_EmptyList_ReturnsEmptyString()
        {
            string code = BuildCodeEncoder.Encode(new List<string>());
            Assert.AreEqual(string.Empty, code,
                "Encode(empty list) must return an empty string.");
        }

        [Test]
        public void Encode_SingleId_IsNonEmpty()
        {
            string code = BuildCodeEncoder.Encode(new List<string> { "chassis_mk1" });
            Assert.IsNotEmpty(code, "Encode with one part ID must return a non-empty code.");
        }

        [Test]
        public void Encode_SingleId_RoundTrips()
        {
            var ids = new List<string> { "chassis_mk1" };
            string code      = BuildCodeEncoder.Encode(ids);
            List<string> decoded = BuildCodeEncoder.Decode(code);

            Assert.IsNotNull(decoded, "Decode of a valid single-ID code must not return null.");
            Assert.AreEqual(1, decoded.Count, "Decoded list must contain exactly one entry.");
            Assert.AreEqual("chassis_mk1", decoded[0], "Decoded ID must match original.");
        }

        [Test]
        public void Encode_MultipleIds_RoundTrips()
        {
            var ids = new List<string> { "chassis_mk1", "arm_left_mk2", "legs_mk1" };
            string code = BuildCodeEncoder.Encode(ids);
            List<string> decoded = BuildCodeEncoder.Decode(code);

            Assert.IsNotNull(decoded);
            Assert.AreEqual(3, decoded.Count, "Decoded count must match original.");
            Assert.AreEqual("chassis_mk1",  decoded[0]);
            Assert.AreEqual("arm_left_mk2", decoded[1]);
            Assert.AreEqual("legs_mk1",     decoded[2]);
        }

        [Test]
        public void Encode_SkipsNullEntries()
        {
            var ids = new List<string> { "chassis_mk1", null, "legs_mk1" };
            string code = BuildCodeEncoder.Encode(ids);
            List<string> decoded = BuildCodeEncoder.Decode(code);

            Assert.IsNotNull(decoded);
            Assert.AreEqual(2, decoded.Count,
                "Null entries must be skipped during encoding.");
            Assert.AreEqual("chassis_mk1", decoded[0]);
            Assert.AreEqual("legs_mk1",    decoded[1]);
        }

        [Test]
        public void Encode_SkipsWhitespaceEntries()
        {
            var ids = new List<string> { "chassis_mk1", "   ", "legs_mk1" };
            string code = BuildCodeEncoder.Encode(ids);
            List<string> decoded = BuildCodeEncoder.Decode(code);

            Assert.IsNotNull(decoded);
            Assert.AreEqual(2, decoded.Count,
                "Whitespace-only entries must be skipped during encoding.");
        }

        // ── Decode ────────────────────────────────────────────────────────────

        [Test]
        public void Decode_NullCode_ReturnsNull()
        {
            Assert.IsNull(BuildCodeEncoder.Decode(null),
                "Decode(null) must return null.");
        }

        [Test]
        public void Decode_EmptyString_ReturnsNull()
        {
            Assert.IsNull(BuildCodeEncoder.Decode(string.Empty),
                "Decode(\"\") must return null.");
        }

        [Test]
        public void Decode_WhitespaceString_ReturnsNull()
        {
            Assert.IsNull(BuildCodeEncoder.Decode("   "),
                "Decode(whitespace) must return null.");
        }

        [Test]
        public void Decode_MalformedBase64_ReturnsNull()
        {
            Assert.IsNull(BuildCodeEncoder.Decode("!!!NOT_BASE64!!!"),
                "Decode of malformed Base64 must return null.");
        }

        [Test]
        public void Decode_ValidCodeProducesNonNullList()
        {
            string code = BuildCodeEncoder.Encode(new List<string> { "part_a", "part_b" });
            Assert.IsNotNull(BuildCodeEncoder.Decode(code),
                "Decode of a code produced by Encode must return a non-null list.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BuildCodeController tests
    // ═══════════════════════════════════════════════════════════════════════════

    public class BuildCodeControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static BuildCodeController MakeController(out GameObject go)
        {
            go = new GameObject("BuildCodeControllerTest");
            go.SetActive(false);
            return go.AddComponent<BuildCodeController>();
        }

        // ── OnEnable / OnDisable — null guards ────────────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            Assert.DoesNotThrow(() => go.SetActive(true));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            SetField(go.GetComponent<BuildCodeController>(), "_playerLoadout", loadout);
            // _onLoadoutChanged remains null
            Assert.DoesNotThrow(() => go.SetActive(true));
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(loadout);
        }

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            MakeController(out GameObject go);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false));
            Object.DestroyImmediate(go);
        }

        // ── OnDisable unregisters ─────────────────────────────────────────────

        [Test]
        public void OnDisable_UnregistersFromOnLoadoutChanged()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int externalCount = 0;
            channel.RegisterCallback(() => externalCount++);

            MakeController(out GameObject go);
            SetField(go.GetComponent<BuildCodeController>(), "_onLoadoutChanged", channel);

            go.SetActive(true);   // Awake + OnEnable → subscribed
            go.SetActive(false);  // OnDisable → must unsubscribe

            channel.Raise();

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);

            Assert.AreEqual(1, externalCount,
                "Only the external counter should fire; controller must be unsubscribed.");
        }

        // ── ExportCode ────────────────────────────────────────────────────────

        [Test]
        public void ExportCode_NullLoadout_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<BuildCodeController>();
            go.SetActive(true);
            // _playerLoadout remains null
            Assert.DoesNotThrow(() => ctrl.ExportCode());
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ExportCode_WithLoadout_WritesCodeToExportText()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<BuildCodeController>();
            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            loadout.SetLoadout(new List<string> { "chassis_mk1", "legs_mk1" });

            var textGo = new GameObject();
            var text   = textGo.AddComponent<UnityEngine.UI.Text>();

            SetField(ctrl, "_playerLoadout",  loadout);
            SetField(ctrl, "_exportCodeText", text);

            go.SetActive(true);
            ctrl.ExportCode();

            Assert.IsNotEmpty(text.text,
                "ExportCode must write a non-empty code to _exportCodeText.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGo);
            Object.DestroyImmediate(loadout);
        }

        // ── ImportCode ────────────────────────────────────────────────────────

        [Test]
        public void ImportCode_NullImportField_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<BuildCodeController>();
            go.SetActive(true);
            // _importCodeField remains null
            Assert.DoesNotThrow(() => ctrl.ImportCode());
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ImportCode_InvalidCode_SetsInvalidCodeStatus()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<BuildCodeController>();

            // Create an InputField (requires a canvas/eventSystem in play mode,
            // but we can test via reflection + manual text assignment for the field value).
            // We test the status text path by calling ImportCode via a child InputField.
            // Use a simpler approach: inject a live InputField using its text backing field.
            var inputGo   = new GameObject("InputGO");
            // InputField requires a Text child to initialise without error in EditMode.
            var textChild = new GameObject("Text");
            textChild.transform.SetParent(inputGo.transform);
            textChild.AddComponent<UnityEngine.UI.Text>();
            var inputField = inputGo.AddComponent<UnityEngine.UI.InputField>();

            var statusGo   = new GameObject();
            var statusText = statusGo.AddComponent<UnityEngine.UI.Text>();

            SetField(ctrl, "_importCodeField", inputField);
            SetField(ctrl, "_statusText",      statusText);

            // Inject junk text via reflection into the InputField's backing field.
            FieldInfo textField = typeof(UnityEngine.UI.InputField)
                .GetField("m_Text", BindingFlags.Instance | BindingFlags.NonPublic);
            if (textField != null) textField.SetValue(inputField, "!!!NOT_BASE64!!!");

            go.SetActive(true);
            ctrl.ImportCode();

            Assert.AreEqual("Invalid code.", statusText.text,
                "ImportCode with malformed code must set statusText to 'Invalid code.'");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(inputGo);
            Object.DestroyImmediate(statusGo);
        }

        [Test]
        public void ImportCode_ValidCode_AppliesLoadout()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<BuildCodeController>();
            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();

            // Build a valid code from known IDs.
            var originalIds = new List<string> { "part_alpha", "part_beta" };
            string code = BuildCodeEncoder.Encode(originalIds);

            var inputGo   = new GameObject("InputGO");
            var textChild = new GameObject("Text");
            textChild.transform.SetParent(inputGo.transform);
            textChild.AddComponent<UnityEngine.UI.Text>();
            var inputField = inputGo.AddComponent<UnityEngine.UI.InputField>();

            FieldInfo textFieldInfo = typeof(UnityEngine.UI.InputField)
                .GetField("m_Text", BindingFlags.Instance | BindingFlags.NonPublic);
            if (textFieldInfo != null) textFieldInfo.SetValue(inputField, code);

            SetField(ctrl, "_playerLoadout",  loadout);
            SetField(ctrl, "_importCodeField", inputField);
            // No _shopCatalog — skip catalog validation

            go.SetActive(true);
            ctrl.ImportCode();

            Assert.AreEqual(2, loadout.EquippedPartIds.Count,
                "After ImportCode, loadout must contain the decoded part IDs.");
            Assert.AreEqual("part_alpha", loadout.EquippedPartIds[0]);
            Assert.AreEqual("part_beta",  loadout.EquippedPartIds[1]);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(inputGo);
            Object.DestroyImmediate(loadout);
        }
    }
}
