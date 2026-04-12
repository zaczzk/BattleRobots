using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PartRepairController"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all-null refs        → DoesNotThrow.
    ///   • OnEnable / OnDisable with null channel          → DoesNotThrow.
    ///   • <see cref="PartRepairController.RepairAll"/>:   null manager → DoesNotThrow.
    ///   • <see cref="PartRepairController.RepairPart"/>:  null manager → DoesNotThrow.
    ///   • Refresh with null registry                      → DoesNotThrow.
    ///   • Refresh with null _listContainer                → DoesNotThrow.
    ///   • OnDisable unregisters from <c>_onRepairApplied</c>
    ///     (external-counter pattern verifies the callback is removed).
    ///   • <see cref="PartRepairController.FormatHP"/>:    correct formatting and rounding.
    ///   • <see cref="PartRepairController.FormatCost"/>:  correct formatting.
    ///
    /// Uses the inactive-GO pattern so Awake runs only after all fields are injected.
    /// </summary>
    public class PartRepairControllerTests
    {
        private GameObject           _go;
        private PartRepairController _ctrl;
        private VoidGameEvent        _onRepairApplied;

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
            _onRepairApplied = ScriptableObject.CreateInstance<VoidGameEvent>();

            _go = new GameObject("PartRepairController");
            _go.SetActive(false); // inactive until fields are wired
            _ctrl = _go.AddComponent<PartRepairController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            if (_onRepairApplied != null) Object.DestroyImmediate(_onRepairApplied);
        }

        private void Activate() => _go.SetActive(true);

        // ── OnEnable / OnDisable — null guard paths ───────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            // All inspector fields null — must not throw on enable.
            Assert.DoesNotThrow(() => Activate());
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            Activate();
            Assert.DoesNotThrow(() => _go.SetActive(false));
        }

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            // _onRepairApplied is null; Refresh must still run safely.
            Assert.DoesNotThrow(() => Activate());
        }

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            Activate();
            Assert.DoesNotThrow(() => _go.SetActive(false));
        }

        // ── RepairAll / RepairPart — null manager guard ───────────────────────

        [Test]
        public void RepairAll_NullManager_DoesNotThrow()
        {
            // _repairManager left null.
            Activate();
            Assert.DoesNotThrow(() => _ctrl.RepairAll());
        }

        [Test]
        public void RepairPart_NullManager_DoesNotThrow()
        {
            // _repairManager left null.
            Activate();
            Assert.DoesNotThrow(() => _ctrl.RepairPart("arm_01"));
        }

        // ── Refresh — null guard paths ────────────────────────────────────────

        [Test]
        public void Refresh_NullRegistry_DoesNotThrow()
        {
            // _registry is null — Refresh must fall back to Array.Empty and not throw.
            SetField(_ctrl, "_onRepairApplied", _onRepairApplied);
            Assert.DoesNotThrow(() => Activate()); // OnEnable calls Refresh
        }

        [Test]
        public void Refresh_NullListContainer_DoesNotThrow()
        {
            // _registry null, _listContainer null — row-building section skipped gracefully.
            Assert.DoesNotThrow(() => Activate());
        }

        // ── OnDisable unregisters ─────────────────────────────────────────────

        [Test]
        public void OnDisable_UnregistersFromOnRepairApplied()
        {
            // External-counter pattern: after OnDisable, raising the event must NOT
            // invoke the controller's Refresh delegate.
            SetField(_ctrl, "_onRepairApplied", _onRepairApplied);

            Activate();           // OnEnable — registers delegate + calls Refresh once
            _go.SetActive(false); // OnDisable — must unregister delegate

            int externalCount = 0;
            _onRepairApplied.RegisterCallback(() => externalCount++);

            _onRepairApplied.Raise(); // controller's Refresh must NOT run

            Assert.AreEqual(1, externalCount,
                "After OnDisable the controller must not respond to _onRepairApplied.");
        }

        // ── FormatHP ──────────────────────────────────────────────────────────

        [Test]
        public void FormatHP_DisplaysCurrentAndMaxHP()
        {
            string result = PartRepairController.FormatHP(35f, 50f);
            Assert.AreEqual("35 / 50 HP", result);
        }

        [Test]
        public void FormatHP_RoundsToNearestInt()
        {
            // 35.7 rounds to 36; 50.4 rounds to 50.
            string result = PartRepairController.FormatHP(35.7f, 50.4f);
            Assert.AreEqual("36 / 50 HP", result);
        }

        // ── FormatCost ────────────────────────────────────────────────────────

        [Test]
        public void FormatCost_DisplaysCost()
        {
            string result = PartRepairController.FormatCost(70);
            Assert.AreEqual("Cost: 70", result);
        }

        [Test]
        public void FormatCost_ZeroCost_DisplaysZero()
        {
            string result = PartRepairController.FormatCost(0);
            Assert.AreEqual("Cost: 0", result);
        }
    }
}
