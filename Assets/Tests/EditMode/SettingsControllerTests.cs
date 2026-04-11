using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="SettingsController"/>.
    ///
    /// Covers:
    ///   • OnEnable with null _settings — early-return path, no throw.
    ///   • OnDisable with null _settings — PersistSettings early-return, no throw.
    ///   • OnEnable with a valid GameSettingsSO but all sliders null — the
    ///     SetValueWithoutNotify and AddListener calls are null-guarded via ?.
    ///   • OnDisable with valid settings but null sliders — RemoveListener calls
    ///     are null-guarded; PersistSettings writes to disk.
    ///   • OnDisable persists the correct volume snapshot to disk
    ///     (SaveSystem.Load().settingsSnapshot.masterVolume == expected value).
    ///   • Second OnDisable (double-disable) — idempotent, no throw.
    ///
    /// SaveSystem.Delete() is called in SetUp and TearDown to prevent cross-test
    /// file pollution.
    /// All tests run headless; no Slider components are required.
    /// </summary>
    public class SettingsControllerTests
    {
        // ── Reflection helper ─────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()   => SaveSystem.Delete();

        [TearDown]
        public void TearDown() => SaveSystem.Delete();

        // ── Factory helper ────────────────────────────────────────────────────

        private static (GameObject go, SettingsController ctrl) MakeCtrl()
        {
            var go   = new GameObject("SettingsController");
            go.SetActive(false); // inactive so OnEnable doesn't fire during field setup
            var ctrl = go.AddComponent<SettingsController>();
            return (go, ctrl);
        }

        // ── OnEnable — null settings ──────────────────────────────────────────

        [Test]
        public void OnEnable_NullSettings_DoesNotThrow()
        {
            // _settings is null; OnEnable's early-return guard must prevent any throw.
            var (go, _) = MakeCtrl();
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with null _settings must not throw.");
            Object.DestroyImmediate(go);
        }

        // ── OnDisable — null settings ─────────────────────────────────────────

        [Test]
        public void OnDisable_NullSettings_DoesNotThrow()
        {
            var (go, _) = MakeCtrl();
            go.SetActive(true);
            // PersistSettings() returns early when _settings is null.
            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with null _settings must not throw.");
            Object.DestroyImmediate(go);
        }

        // ── OnEnable — valid settings, null sliders ───────────────────────────

        [Test]
        public void OnEnable_WithSettings_NullSliders_DoesNotThrow()
        {
            var settings = ScriptableObject.CreateInstance<GameSettingsSO>();
            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_settings", settings);
            // All three slider fields remain null; SetValueWithoutNotify + AddListener
            // are null-guarded via ?. and must not throw.
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with valid settings but null sliders must not throw.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(settings);
        }

        // ── OnDisable — valid settings, null sliders — persists snapshot ──────

        [Test]
        public void OnDisable_WithSettings_NullSliders_PersistsSnapshotToDisk()
        {
            // Set a distinctive master volume, disable → PersistSettings must write it.
            var settings = ScriptableObject.CreateInstance<GameSettingsSO>();
            settings.SetMasterVolume(0.65f);

            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_settings", settings);
            go.SetActive(true);   // Awake caches delegates; OnEnable no-ops (null sliders)
            go.SetActive(false);  // OnDisable → PersistSettings() → SaveSystem.Save()

            SaveData saved = SaveSystem.Load();
            Assert.AreEqual(0.65f, saved.settingsSnapshot.masterVolume, 0.001f,
                "OnDisable must persist the current MasterVolume to disk via PersistSettings.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(settings);
        }

        // ── OnDisable idempotency ─────────────────────────────────────────────

        [Test]
        public void OnDisable_CalledTwice_DoesNotThrow()
        {
            var settings = ScriptableObject.CreateInstance<GameSettingsSO>();
            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_settings", settings);
            go.SetActive(true);

            Assert.DoesNotThrow(() =>
            {
                go.SetActive(false); // first disable
                go.SetActive(false); // second disable (already inactive — Unity no-ops OnDisable)
            }, "Disabling SettingsController twice must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(settings);
        }
    }
}
