using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T033 — key binding system.
    ///
    /// Coverage:
    ///   SettingsSO:
    ///     - LoadKeyBindings(null)  → defaults applied for all actions
    ///     - LoadKeyBindings(empty) → defaults applied for all actions
    ///     - LoadKeyBindings(partial) → saved keys used; missing ones default
    ///     - LoadKeyBindings(full)  → all saved keys used
    ///     - GetBinding known action  → returns KeyCode
    ///     - GetBinding unknown action → returns KeyCode.None
    ///     - SetBinding → overrides value; subsequent GetBinding reflects change
    ///     - SetBinding empty actionName → no-op (no exception)
    ///     - BuildKeyBindings round-trip → all actions preserved
    ///     - Multiple SetBinding calls accumulate correctly
    ///
    ///   SaveData:
    ///     - keyBindings field default not null
    ///     - Full SaveSystem round-trip: SetBinding → BuildKeyBindings → Save → Load → LoadKeyBindings → GetBinding
    /// </summary>
    [TestFixture]
    public sealed class KeyBindingsTests
    {
        private SettingsSO _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<SettingsSO>();
            SaveSystem.Delete();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_settings);
            SaveSystem.Delete();
        }

        // ── LoadKeyBindings ───────────────────────────────────────────────────

        [Test]
        public void LoadKeyBindings_Null_AppliesDefaults()
        {
            _settings.LoadKeyBindings(null);

            // Default forward = W
            Assert.AreEqual(KeyCode.W, _settings.GetBinding("Forward"));
        }

        [Test]
        public void LoadKeyBindings_EmptyData_AppliesDefaults()
        {
            var empty = new KeyBindingsData(); // entries list is empty
            _settings.LoadKeyBindings(empty);

            Assert.AreEqual(KeyCode.S,     _settings.GetBinding("Back"));
            Assert.AreEqual(KeyCode.A,     _settings.GetBinding("Left"));
            Assert.AreEqual(KeyCode.D,     _settings.GetBinding("Right"));
            Assert.AreEqual(KeyCode.Space, _settings.GetBinding("Fire"));
        }

        [Test]
        public void LoadKeyBindings_FullDefaults_AllFiveActionsPresent()
        {
            _settings.LoadKeyBindings(null);

            Assert.AreEqual(KeyCode.W,     _settings.GetBinding("Forward"), "Forward");
            Assert.AreEqual(KeyCode.S,     _settings.GetBinding("Back"),    "Back");
            Assert.AreEqual(KeyCode.A,     _settings.GetBinding("Left"),    "Left");
            Assert.AreEqual(KeyCode.D,     _settings.GetBinding("Right"),   "Right");
            Assert.AreEqual(KeyCode.Space, _settings.GetBinding("Fire"),    "Fire");
        }

        [Test]
        public void LoadKeyBindings_PartialData_MissingActionsGetDefaults()
        {
            var data = new KeyBindingsData();
            data.entries.Add(new KeyBindingEntry { actionName = "Forward", keyCode = (int)KeyCode.UpArrow });
            // Back, Left, Right, Fire are missing → should fall back to defaults

            _settings.LoadKeyBindings(data);

            Assert.AreEqual(KeyCode.UpArrow, _settings.GetBinding("Forward"), "custom Forward");
            Assert.AreEqual(KeyCode.S,       _settings.GetBinding("Back"),    "default Back");
        }

        [Test]
        public void LoadKeyBindings_FullData_AllCustomKeysUsed()
        {
            var data = new KeyBindingsData();
            data.entries.Add(new KeyBindingEntry { actionName = "Forward", keyCode = (int)KeyCode.UpArrow    });
            data.entries.Add(new KeyBindingEntry { actionName = "Back",    keyCode = (int)KeyCode.DownArrow  });
            data.entries.Add(new KeyBindingEntry { actionName = "Left",    keyCode = (int)KeyCode.LeftArrow  });
            data.entries.Add(new KeyBindingEntry { actionName = "Right",   keyCode = (int)KeyCode.RightArrow });
            data.entries.Add(new KeyBindingEntry { actionName = "Fire",    keyCode = (int)KeyCode.Return     });

            _settings.LoadKeyBindings(data);

            Assert.AreEqual(KeyCode.UpArrow,    _settings.GetBinding("Forward"));
            Assert.AreEqual(KeyCode.DownArrow,  _settings.GetBinding("Back"));
            Assert.AreEqual(KeyCode.LeftArrow,  _settings.GetBinding("Left"));
            Assert.AreEqual(KeyCode.RightArrow, _settings.GetBinding("Right"));
            Assert.AreEqual(KeyCode.Return,     _settings.GetBinding("Fire"));
        }

        // ── GetBinding ────────────────────────────────────────────────────────

        [Test]
        public void GetBinding_UnknownAction_ReturnsNone()
        {
            _settings.LoadKeyBindings(null);

            Assert.AreEqual(KeyCode.None, _settings.GetBinding("NonExistentAction"));
        }

        [Test]
        public void GetBinding_BeforeLoad_ReturnsDefault()
        {
            // Default is returned even without an explicit LoadKeyBindings call
            // because s_DefaultBindings is consulted as a fallback.
            KeyCode forward = _settings.GetBinding("Forward");
            Assert.AreEqual(KeyCode.W, forward);
        }

        // ── SetBinding ────────────────────────────────────────────────────────

        [Test]
        public void SetBinding_ValidAction_UpdatesGetBinding()
        {
            _settings.LoadKeyBindings(null);
            _settings.SetBinding("Forward", KeyCode.UpArrow);

            Assert.AreEqual(KeyCode.UpArrow, _settings.GetBinding("Forward"));
        }

        [Test]
        public void SetBinding_NewAction_GetBindingReturnsIt()
        {
            _settings.LoadKeyBindings(null);
            _settings.SetBinding("Boost", KeyCode.LeftShift);

            Assert.AreEqual(KeyCode.LeftShift, _settings.GetBinding("Boost"));
        }

        [Test]
        public void SetBinding_EmptyActionName_DoesNotThrow()
        {
            _settings.LoadKeyBindings(null);

            Assert.DoesNotThrow(() => _settings.SetBinding(string.Empty, KeyCode.W));
            Assert.DoesNotThrow(() => _settings.SetBinding(null,         KeyCode.W));
        }

        [Test]
        public void SetBinding_MultipleCallsAccumulate()
        {
            _settings.LoadKeyBindings(null);
            _settings.SetBinding("Forward", KeyCode.I);
            _settings.SetBinding("Back",    KeyCode.K);

            Assert.AreEqual(KeyCode.I, _settings.GetBinding("Forward"));
            Assert.AreEqual(KeyCode.K, _settings.GetBinding("Back"));
            // Unmodified action still has its default.
            Assert.AreEqual(KeyCode.Space, _settings.GetBinding("Fire"));
        }

        // ── BuildKeyBindings ──────────────────────────────────────────────────

        [Test]
        public void BuildKeyBindings_RoundTrip_AllActionsPreserved()
        {
            _settings.LoadKeyBindings(null);
            _settings.SetBinding("Forward", KeyCode.UpArrow);

            KeyBindingsData snapshot = _settings.BuildKeyBindings();

            // Load into a fresh instance and verify.
            var other = ScriptableObject.CreateInstance<SettingsSO>();
            other.LoadKeyBindings(snapshot);

            Assert.AreEqual(KeyCode.UpArrow, other.GetBinding("Forward"),  "Forward");
            Assert.AreEqual(KeyCode.S,       other.GetBinding("Back"),     "Back default preserved");
            Assert.AreEqual(KeyCode.Space,   other.GetBinding("Fire"),     "Fire default preserved");

            Object.DestroyImmediate(other);
        }

        [Test]
        public void BuildKeyBindings_NotNull()
        {
            _settings.LoadKeyBindings(null);
            Assert.IsNotNull(_settings.BuildKeyBindings());
            Assert.IsNotNull(_settings.BuildKeyBindings().entries);
        }

        // ── SaveData default ──────────────────────────────────────────────────

        [Test]
        public void SaveData_DefaultKeyBindings_NotNull()
        {
            var sd = new SaveData();
            Assert.IsNotNull(sd.keyBindings);
            Assert.IsNotNull(sd.keyBindings.entries);
        }

        // ── SaveSystem round-trip ─────────────────────────────────────────────

        [Test]
        public void SaveSystem_RoundTrip_KeyBindingsPersistedAndRestored()
        {
            _settings.LoadKeyBindings(null);
            _settings.SetBinding("Forward", KeyCode.UpArrow);
            _settings.SetBinding("Fire",    KeyCode.Return);

            SaveData outgoing = SaveSystem.Load();
            outgoing.keyBindings = _settings.BuildKeyBindings();
            SaveSystem.Save(outgoing);

            // Restore into a fresh SettingsSO.
            var fresh = ScriptableObject.CreateInstance<SettingsSO>();
            SaveData incoming = SaveSystem.Load();
            fresh.LoadKeyBindings(incoming.keyBindings);

            Assert.AreEqual(KeyCode.UpArrow, fresh.GetBinding("Forward"), "Forward persisted");
            Assert.AreEqual(KeyCode.Return,  fresh.GetBinding("Fire"),    "Fire persisted");
            Assert.AreEqual(KeyCode.S,       fresh.GetBinding("Back"),    "Back default persisted");

            Object.DestroyImmediate(fresh);
        }
    }
}
