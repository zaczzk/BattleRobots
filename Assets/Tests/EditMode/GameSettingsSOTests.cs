using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="GameSettingsSO"/> and <see cref="SettingsSnapshot"/>.
    ///
    /// Strategy:
    ///   • <c>ScriptableObject.CreateInstance</c> allocates the SO on the Unity heap;
    ///     <c>Object.DestroyImmediate</c> cleans up in TearDown.
    ///   • The private <c>_onSettingsChanged</c> field is injected via reflection for
    ///     event-channel tests, mirroring the pattern used in VoidGameEventTests and
    ///     RobotDefinitionTests.
    ///   • LoadSnapshot tests verify volumes are NOT fired (bootstrapper contract).
    /// </summary>
    public class GameSettingsSOTests
    {
        private GameSettingsSO _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<GameSettingsSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_settings);
            _settings = null;
        }

        // ── Default values ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_MasterVolume_IsOne()
            => Assert.AreEqual(1f, _settings.MasterVolume);

        [Test]
        public void FreshInstance_SfxVolume_IsOne()
            => Assert.AreEqual(1f, _settings.SfxVolume);

        [Test]
        public void FreshInstance_MusicVolume_IsOne()
            => Assert.AreEqual(1f, _settings.MusicVolume);

        [Test]
        public void FreshInstance_EffectiveSfxVolume_IsOne()
            => Assert.AreEqual(1f, _settings.EffectiveSfxVolume);

        [Test]
        public void FreshInstance_EffectiveMusicVolume_IsOne()
            => Assert.AreEqual(1f, _settings.EffectiveMusicVolume);

        // ── SetMasterVolume ───────────────────────────────────────────────────

        [Test]
        public void SetMasterVolume_ValidValue_Stores()
        {
            _settings.SetMasterVolume(0.5f);
            Assert.AreEqual(0.5f, _settings.MasterVolume);
        }

        [Test]
        public void SetMasterVolume_BelowZero_ClampsToZero()
        {
            _settings.SetMasterVolume(-0.5f);
            Assert.AreEqual(0f, _settings.MasterVolume);
        }

        [Test]
        public void SetMasterVolume_AboveOne_ClampsToOne()
        {
            _settings.SetMasterVolume(2f);
            Assert.AreEqual(1f, _settings.MasterVolume);
        }

        // ── SetSfxVolume ──────────────────────────────────────────────────────

        [Test]
        public void SetSfxVolume_ValidValue_Stores()
        {
            _settings.SetSfxVolume(0.7f);
            Assert.AreEqual(0.7f, _settings.SfxVolume);
        }

        [Test]
        public void SetSfxVolume_BelowZero_ClampsToZero()
        {
            _settings.SetSfxVolume(-1f);
            Assert.AreEqual(0f, _settings.SfxVolume);
        }

        [Test]
        public void SetSfxVolume_AboveOne_ClampsToOne()
        {
            _settings.SetSfxVolume(5f);
            Assert.AreEqual(1f, _settings.SfxVolume);
        }

        // ── SetMusicVolume ────────────────────────────────────────────────────

        [Test]
        public void SetMusicVolume_ValidValue_Stores()
        {
            _settings.SetMusicVolume(0.3f);
            Assert.AreEqual(0.3f, _settings.MusicVolume);
        }

        // ── Effective volumes ─────────────────────────────────────────────────

        [Test]
        public void EffectiveSfxVolume_IsMasterTimesSfx()
        {
            _settings.SetMasterVolume(0.8f);
            _settings.SetSfxVolume(0.5f);
            Assert.AreEqual(0.4f, _settings.EffectiveSfxVolume, 0.0001f);
        }

        [Test]
        public void EffectiveMusicVolume_IsMasterTimesMusic()
        {
            _settings.SetMasterVolume(0.5f);
            _settings.SetMusicVolume(0.6f);
            Assert.AreEqual(0.3f, _settings.EffectiveMusicVolume, 0.0001f);
        }

        [Test]
        public void EffectiveSfxVolume_WhenMasterZero_IsZero()
        {
            _settings.SetMasterVolume(0f);
            _settings.SetSfxVolume(1f);
            Assert.AreEqual(0f, _settings.EffectiveSfxVolume);
        }

        // ── TakeSnapshot ──────────────────────────────────────────────────────

        [Test]
        public void TakeSnapshot_CapturesAllThreeVolumes()
        {
            _settings.SetMasterVolume(0.9f);
            _settings.SetSfxVolume(0.7f);
            _settings.SetMusicVolume(0.5f);
            SettingsSnapshot snap = _settings.TakeSnapshot();
            Assert.AreEqual(0.9f, snap.masterVolume, 0.0001f);
            Assert.AreEqual(0.7f, snap.sfxVolume,    0.0001f);
            Assert.AreEqual(0.5f, snap.musicVolume,  0.0001f);
        }

        [Test]
        public void TakeSnapshot_FreshInstance_AllVolumesOne()
        {
            SettingsSnapshot snap = _settings.TakeSnapshot();
            Assert.AreEqual(1f, snap.masterVolume);
            Assert.AreEqual(1f, snap.sfxVolume);
            Assert.AreEqual(1f, snap.musicVolume);
        }

        // ── LoadSnapshot ──────────────────────────────────────────────────────

        [Test]
        public void LoadSnapshot_RestoresAllVolumes()
        {
            var snap = new SettingsSnapshot { masterVolume = 0.4f, sfxVolume = 0.6f, musicVolume = 0.8f };
            _settings.LoadSnapshot(snap);
            Assert.AreEqual(0.4f, _settings.MasterVolume, 0.0001f);
            Assert.AreEqual(0.6f, _settings.SfxVolume,    0.0001f);
            Assert.AreEqual(0.8f, _settings.MusicVolume,  0.0001f);
        }

        [Test]
        public void LoadSnapshot_NullInput_LeavesValuesUnchanged()
        {
            _settings.SetMasterVolume(0.5f);
            _settings.LoadSnapshot(null);
            Assert.AreEqual(0.5f, _settings.MasterVolume);
        }

        [Test]
        public void LoadSnapshot_OutOfRangeValues_Clamps()
        {
            var snap = new SettingsSnapshot { masterVolume = 2f, sfxVolume = -1f, musicVolume = 0.5f };
            _settings.LoadSnapshot(snap);
            Assert.AreEqual(1f,   _settings.MasterVolume);
            Assert.AreEqual(0f,   _settings.SfxVolume);
            Assert.AreEqual(0.5f, _settings.MusicVolume, 0.0001f);
        }

        [Test]
        public void LoadSnapshot_DoesNotRaiseSettingsChangedEvent()
        {
            // Contract: LoadSnapshot is silent so GameBootstrapper can call it
            // before AudioManager is listening.
            var evt = ScriptableObject.CreateInstance<VoidGameEvent>();
            InjectEventField(evt);
            bool raised = false;
            evt.RegisterCallback(() => raised = true);

            _settings.LoadSnapshot(new SettingsSnapshot { masterVolume = 0.5f, sfxVolume = 0.5f, musicVolume = 0.5f });

            Assert.IsFalse(raised, "LoadSnapshot must not raise _onSettingsChanged.");
            Object.DestroyImmediate(evt);
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_SetsAllVolumesToOne()
        {
            _settings.SetMasterVolume(0.1f);
            _settings.SetSfxVolume(0.2f);
            _settings.SetMusicVolume(0.3f);
            _settings.Reset();
            Assert.AreEqual(1f, _settings.MasterVolume);
            Assert.AreEqual(1f, _settings.SfxVolume);
            Assert.AreEqual(1f, _settings.MusicVolume);
        }

        // ── Event channel ─────────────────────────────────────────────────────

        [Test]
        public void SetMasterVolume_RaisesSettingsChangedEvent()
        {
            var evt = ScriptableObject.CreateInstance<VoidGameEvent>();
            InjectEventField(evt);
            bool raised = false;
            evt.RegisterCallback(() => raised = true);

            _settings.SetMasterVolume(0.5f);

            Assert.IsTrue(raised);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SetSfxVolume_RaisesSettingsChangedEvent()
        {
            var evt = ScriptableObject.CreateInstance<VoidGameEvent>();
            InjectEventField(evt);
            bool raised = false;
            evt.RegisterCallback(() => raised = true);

            _settings.SetSfxVolume(0.5f);

            Assert.IsTrue(raised);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Reset_RaisesSettingsChangedEvent()
        {
            var evt = ScriptableObject.CreateInstance<VoidGameEvent>();
            InjectEventField(evt);
            bool raised = false;
            evt.RegisterCallback(() => raised = true);

            _settings.Reset();

            Assert.IsTrue(raised);
            Object.DestroyImmediate(evt);
        }

        // ── SettingsSnapshot defaults ─────────────────────────────────────────

        [Test]
        public void SettingsSnapshot_FreshInstance_DefaultVolumesAreOne()
        {
            var snap = new SettingsSnapshot();
            Assert.AreEqual(1f, snap.masterVolume);
            Assert.AreEqual(1f, snap.sfxVolume);
            Assert.AreEqual(1f, snap.musicVolume);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void InjectEventField(VoidGameEvent evt)
        {
            var field = typeof(GameSettingsSO).GetField(
                "_onSettingsChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(_settings, evt);
        }
    }
}
