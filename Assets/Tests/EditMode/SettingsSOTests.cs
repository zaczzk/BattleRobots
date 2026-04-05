using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode integration tests for <see cref="SettingsSO"/> and its persistence
    /// round-trip through <see cref="SaveSystem"/>.
    ///
    /// Coverage:
    ///   - ResetToDefaults populates all three fields.
    ///   - LoadFromData maps POCO values into runtime properties.
    ///   - BuildData snapshots runtime state back to a POCO.
    ///   - SetMasterVolume / SetSfxVolume clamp to [0, 1].
    ///   - SetInvertControls toggles correctly.
    ///   - Full round-trip: mutate → BuildData → SaveSystem → Load → LoadFromData → verify.
    ///   - LoadFromData(null) falls back to defaults.
    ///   - Clamping: values outside [0,1] are clamped.
    /// </summary>
    [TestFixture]
    public sealed class SettingsSOTests
    {
        private SettingsSO _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<SettingsSO>();
            // Remove any leftover save file that might pollute round-trip tests.
            SaveSystem.Delete();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_settings);
            SaveSystem.Delete();
        }

        // ── ResetToDefaults ───────────────────────────────────────────────────

        [Test]
        public void ResetToDefaults_SetsExpectedValues()
        {
            _settings.ResetToDefaults();

            // Defaults are defined in the inspector; ScriptableObject.CreateInstance
            // uses Unity's serialised defaults (1f, 1f, false) from the field initialisers.
            Assert.AreEqual(1f,    _settings.MasterVolume,   1e-5f, "Default MasterVolume should be 1.");
            Assert.AreEqual(1f,    _settings.SfxVolume,      1e-5f, "Default SfxVolume should be 1.");
            Assert.IsFalse(_settings.InvertControls,                  "Default InvertControls should be false.");
        }

        // ── LoadFromData / BuildData ──────────────────────────────────────────

        [Test]
        public void LoadFromData_MapsAllFields()
        {
            var data = new SettingsData
            {
                masterVolume   = 0.6f,
                sfxVolume      = 0.3f,
                invertControls = true,
            };

            _settings.LoadFromData(data);

            Assert.AreEqual(0.6f, _settings.MasterVolume,  1e-5f);
            Assert.AreEqual(0.3f, _settings.SfxVolume,     1e-5f);
            Assert.IsTrue(_settings.InvertControls);
        }

        [Test]
        public void LoadFromData_Null_FallsBackToDefaults()
        {
            _settings.LoadFromData(null);

            Assert.AreEqual(1f, _settings.MasterVolume, 1e-5f, "Null data should fall back to default master volume.");
            Assert.AreEqual(1f, _settings.SfxVolume,    1e-5f, "Null data should fall back to default sfx volume.");
        }

        [Test]
        public void BuildData_SnapshotsCurrentState()
        {
            _settings.LoadFromData(new SettingsData
            {
                masterVolume   = 0.75f,
                sfxVolume      = 0.5f,
                invertControls = true,
            });

            SettingsData snapshot = _settings.BuildData();

            Assert.AreEqual(0.75f, snapshot.masterVolume,   1e-5f);
            Assert.AreEqual(0.5f,  snapshot.sfxVolume,      1e-5f);
            Assert.IsTrue(snapshot.invertControls);
        }

        // ── Mutators ──────────────────────────────────────────────────────────

        [Test]
        public void SetMasterVolume_UpdatesProperty()
        {
            _settings.SetMasterVolume(0.4f);
            Assert.AreEqual(0.4f, _settings.MasterVolume, 1e-5f);
        }

        [Test]
        public void SetSfxVolume_UpdatesProperty()
        {
            _settings.SetSfxVolume(0.2f);
            Assert.AreEqual(0.2f, _settings.SfxVolume, 1e-5f);
        }

        [Test]
        public void SetInvertControls_TogglesProperty()
        {
            _settings.SetInvertControls(true);
            Assert.IsTrue(_settings.InvertControls);

            _settings.SetInvertControls(false);
            Assert.IsFalse(_settings.InvertControls);
        }

        [Test]
        public void SetMasterVolume_ClampsAboveOne()
        {
            _settings.SetMasterVolume(5f);
            Assert.AreEqual(1f, _settings.MasterVolume, 1e-5f, "Value >1 should clamp to 1.");
        }

        [Test]
        public void SetMasterVolume_ClampsBelowZero()
        {
            _settings.SetMasterVolume(-0.5f);
            Assert.AreEqual(0f, _settings.MasterVolume, 1e-5f, "Value <0 should clamp to 0.");
        }

        [Test]
        public void SetSfxVolume_ClampsAboveOne()
        {
            _settings.SetSfxVolume(99f);
            Assert.AreEqual(1f, _settings.SfxVolume, 1e-5f);
        }

        // ── Full persistence round-trip ───────────────────────────────────────

        [Test]
        public void RoundTrip_SettingsViaSaveSystem_PreservesAllValues()
        {
            // Arrange: set non-default values.
            _settings.SetMasterVolume(0.55f);
            _settings.SetSfxVolume(0.33f);
            _settings.SetInvertControls(true);

            // Act: persist to disk.
            var saveData = new SaveData
            {
                walletBalance = 999,
                settings      = _settings.BuildData(),
            };
            SaveSystem.Save(saveData);

            // Act: load from disk into a fresh SettingsSO instance.
            var loaded    = ScriptableObject.CreateInstance<SettingsSO>();
            SaveData read = SaveSystem.Load();
            loaded.LoadFromData(read.settings);

            // Assert.
            Assert.AreEqual(0.55f, loaded.MasterVolume,  1e-4f, "MasterVolume must survive round-trip.");
            Assert.AreEqual(0.33f, loaded.SfxVolume,     1e-4f, "SfxVolume must survive round-trip.");
            Assert.IsTrue(loaded.InvertControls,                  "InvertControls must survive round-trip.");

            Object.DestroyImmediate(loaded);
        }

        [Test]
        public void LoadFromData_ClampsOutOfRangeValues()
        {
            var badData = new SettingsData { masterVolume = 2f, sfxVolume = -1f };
            _settings.LoadFromData(badData);

            Assert.AreEqual(1f, _settings.MasterVolume, 1e-5f, "Out-of-range master volume should clamp to 1.");
            Assert.AreEqual(0f, _settings.SfxVolume,    1e-5f, "Negative sfx volume should clamp to 0.");
        }
    }
}
