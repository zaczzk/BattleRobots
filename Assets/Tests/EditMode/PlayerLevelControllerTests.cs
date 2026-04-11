using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PlayerLevelController"/>.
    ///
    /// Covers:
    ///   • <see cref="PlayerLevelController.Refresh"/> null-safety:
    ///       null <c>_progression</c> → early return, no throw;
    ///       valid SO at non-max level / max level with all UI refs null → no throw;
    ///       called twice in succession → no throw.
    ///   • OnEnable with all null event channels → no throw.
    ///   • OnDisable with all null event channels → no throw.
    ///   • OnDisable unregisters the void refresh delegate from <c>_onLevelUp</c>
    ///       (inactive-GO double enable/disable cycle; verified via external counter).
    ///   • OnDisable unregisters the int refresh delegate from <c>_onXPGained</c>
    ///       (same pattern, IntGameEvent variant).
    ///
    /// All tests run headless (no scene, no uGUI references required).
    /// </summary>
    public class PlayerLevelControllerTests
    {
        // ── Scene / MB objects ────────────────────────────────────────────────
        private GameObject             _go;
        private PlayerLevelController  _ctrl;

        // ── ScriptableObjects ─────────────────────────────────────────────────
        private PlayerProgressionSO _progression;
        private VoidGameEvent       _onLevelUp;
        private IntGameEvent        _onXPGained;

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
        public void SetUp()
        {
            _go          = new GameObject("PlayerLevelController");
            _go.SetActive(false); // inactive so Awake/OnEnable don't fire during setup
            _ctrl        = _go.AddComponent<PlayerLevelController>();
            _progression = ScriptableObject.CreateInstance<PlayerProgressionSO>();
            _onLevelUp   = ScriptableObject.CreateInstance<VoidGameEvent>();
            _onXPGained  = ScriptableObject.CreateInstance<IntGameEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_progression);
            Object.DestroyImmediate(_onLevelUp);
            Object.DestroyImmediate(_onXPGained);
        }

        // ── Refresh — null progression ────────────────────────────────────────

        [Test]
        public void Refresh_NullProgression_DoesNotThrow()
        {
            // _progression not wired; Refresh() must early-out without throwing.
            _go.SetActive(true);
            Assert.DoesNotThrow(() => _ctrl.Refresh(),
                "Refresh() with null _progression must return early without throwing.");
        }

        // ── Refresh — valid progression, no UI refs ───────────────────────────

        [Test]
        public void Refresh_WithProgression_NotMaxLevel_AllUINull_DoesNotThrow()
        {
            SetField(_ctrl, "_progression", _progression);
            _go.SetActive(true);

            // Level 1, 0 XP — not max level branch.
            Assert.DoesNotThrow(() => _ctrl.Refresh(),
                "Refresh() at non-max level with all UI refs null must not throw.");
        }

        [Test]
        public void Refresh_WithProgression_AtMaxLevel_AllUINull_DoesNotThrow()
        {
            SetField(_ctrl, "_progression", _progression);
            _go.SetActive(true);

            // Force max level via a large XP injection (triangular formula: 50×N×(N-1)).
            // Default _maxLevel = 10 → TotalXPForLevel(10) = 50×10×9 = 4500.
            _progression.AddXP(9999);

            Assert.DoesNotThrow(() => _ctrl.Refresh(),
                "Refresh() at max level with all UI refs null must not throw.");
        }

        [Test]
        public void Refresh_CalledTwice_DoesNotThrow()
        {
            SetField(_ctrl, "_progression", _progression);
            _go.SetActive(true);

            Assert.DoesNotThrow(() =>
            {
                _ctrl.Refresh();
                _ctrl.Refresh();
            }, "Calling Refresh() twice in succession must not throw.");
        }

        // ── OnEnable / OnDisable — null event channels ────────────────────────

        [Test]
        public void OnEnable_AllEventChannelsNull_DoesNotThrow()
        {
            // No event SOs assigned; the ?. operators must handle nulls silently.
            Assert.DoesNotThrow(() => _go.SetActive(true),
                "Activating PlayerLevelController with all null event channels must not throw.");
        }

        [Test]
        public void OnDisable_AllEventChannelsNull_DoesNotThrow()
        {
            _go.SetActive(true);
            Assert.DoesNotThrow(() => _go.SetActive(false),
                "Deactivating PlayerLevelController with all null event channels must not throw.");
        }

        // ── OnDisable — unregisters void delegate from _onLevelUp ─────────────

        [Test]
        public void OnDisable_UnregistersFromLevelUpEvent_VoidCallback()
        {
            SetField(_ctrl, "_progression", _progression);
            SetField(_ctrl, "_onLevelUp",   _onLevelUp);

            // An external counter verifies the event still works after disable.
            int externalCount = 0;
            _onLevelUp.RegisterCallback(() => externalCount++);

            _go.SetActive(true);   // Awake caches + OnEnable registers _refreshVoid.
            _go.SetActive(false);  // OnDisable must unregister _refreshVoid.

            // Raise the event — only the external counter should fire.
            _onLevelUp.Raise();

            Assert.AreEqual(1, externalCount,
                "After OnDisable, only the external counter (not _refreshVoid) should fire.");
        }

        // ── OnDisable — unregisters int delegate from _onXPGained ─────────────

        [Test]
        public void OnDisable_UnregistersFromXPGainedEvent_IntCallback()
        {
            SetField(_ctrl, "_progression", _progression);
            SetField(_ctrl, "_onXPGained",  _onXPGained);

            int externalCount = 0;
            _onXPGained.RegisterCallback((int _) => externalCount++);

            _go.SetActive(true);
            _go.SetActive(false);

            _onXPGained.Raise(100);

            Assert.AreEqual(1, externalCount,
                "After OnDisable, only the external counter (not _refreshInt) should fire.");
        }
    }
}
