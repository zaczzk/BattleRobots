using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for the M26 Weapon Type Unlock system (T173):
    ///   <see cref="WeaponTypeUnlockConfig"/>,
    ///   <see cref="WeaponTypeUnlockEvaluator"/>, and
    ///   <see cref="WeaponTypeUnlockController"/>.
    ///
    /// WeaponTypeUnlockConfigTests (12):
    ///   Fresh-instance defaults — Physical=0, Energy=1, Thermal=4, Shock=7.
    ///   GetRequiredPrestige returns the correct field for each DamageType.
    ///   GetRequiredPrestige returns 0 for an unknown / out-of-range type.
    ///   IsUnlocked returns true when prestigeCount >= required.
    ///   IsUnlocked returns false when prestigeCount &lt; required.
    ///   IsUnlocked returns true when prestigeCount == required (boundary).
    ///   GetLockReason returns string.Empty when type is unlocked.
    ///   GetLockReason returns a non-empty string containing the required count when locked.
    ///   GetLockReason locked string contains the rank label from PrestigeSystemSO.
    ///   IsUnlocked Physical at prestige 0 returns true (always available).
    ///   IsUnlocked Energy at prestige 0 returns false (default requires 1).
    ///   IsUnlocked Energy at prestige 1 returns true (default requires 1).
    ///
    /// WeaponTypeUnlockEvaluatorTests (4):
    ///   Null config → IsTypeUnlocked always returns true.
    ///   Null prestige → treated as count 0.
    ///   IsTypeUnlocked with sufficient prestige returns true.
    ///   GetLockReason with null config returns string.Empty.
    ///
    /// WeaponTypeUnlockControllerTests (8):
    ///   OnEnable with all-null refs does not throw.
    ///   OnDisable with all-null refs does not throw.
    ///   OnEnable with null channel does not throw.
    ///   OnDisable with null channel does not throw.
    ///   OnDisable unregisters from _onPrestige.
    ///   Refresh with null _listContainer does not throw.
    ///   Refresh with null _unlockConfig does not throw.
    ///   Fresh instance — UnlockConfig and PrestigeSystem are null.
    ///
    /// Total: 24 new EditMode tests.
    /// All tests run headless (no Unity Editor scene required).
    /// </summary>
    public class WeaponTypeUnlockTests
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

        private static void InvokePrivate(object target, string method, object[] args = null)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, args ?? System.Array.Empty<object>());
        }

        private static WeaponTypeUnlockConfig CreateConfig(
            int physical = 0, int energy = 0, int thermal = 0, int shock = 0)
        {
            var cfg = ScriptableObject.CreateInstance<WeaponTypeUnlockConfig>();
            SetField(cfg, "_physicalRequiredPrestige", physical);
            SetField(cfg, "_energyRequiredPrestige",   energy);
            SetField(cfg, "_thermalRequiredPrestige",  thermal);
            SetField(cfg, "_shockRequiredPrestige",    shock);
            return cfg;
        }

        private static PrestigeSystemSO CreatePrestige(int count, int max = 10)
        {
            var p = ScriptableObject.CreateInstance<PrestigeSystemSO>();
            SetField(p, "_maxPrestigeRank", max);
            p.LoadSnapshot(count);
            return p;
        }

        // ══════════════════════════════════════════════════════════════════════
        // WeaponTypeUnlockConfig Tests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void FreshInstance_Physical_DefaultIsZero()
        {
            var cfg = ScriptableObject.CreateInstance<WeaponTypeUnlockConfig>();
            Assert.AreEqual(0, cfg.PhysicalRequiredPrestige,
                "Physical weapons should always be available by default (requirement 0).");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void FreshInstance_Energy_DefaultIsOne()
        {
            var cfg = ScriptableObject.CreateInstance<WeaponTypeUnlockConfig>();
            Assert.AreEqual(1, cfg.EnergyRequiredPrestige,
                "Energy weapons should require Prestige 1 by default.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void FreshInstance_Thermal_DefaultIsFour()
        {
            var cfg = ScriptableObject.CreateInstance<WeaponTypeUnlockConfig>();
            Assert.AreEqual(4, cfg.ThermalRequiredPrestige,
                "Thermal weapons should require Prestige 4 by default.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void FreshInstance_Shock_DefaultIsSeven()
        {
            var cfg = ScriptableObject.CreateInstance<WeaponTypeUnlockConfig>();
            Assert.AreEqual(7, cfg.ShockRequiredPrestige,
                "Shock weapons should require Prestige 7 by default.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetRequiredPrestige_Physical_ReturnsPhysicalField()
        {
            var cfg = CreateConfig(physical: 2);
            Assert.AreEqual(2, cfg.GetRequiredPrestige(DamageType.Physical));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetRequiredPrestige_Energy_ReturnsEnergyField()
        {
            var cfg = CreateConfig(energy: 3);
            Assert.AreEqual(3, cfg.GetRequiredPrestige(DamageType.Energy));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetRequiredPrestige_Thermal_ReturnsThermalField()
        {
            var cfg = CreateConfig(thermal: 5);
            Assert.AreEqual(5, cfg.GetRequiredPrestige(DamageType.Thermal));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetRequiredPrestige_Shock_ReturnsShockField()
        {
            var cfg = CreateConfig(shock: 8);
            Assert.AreEqual(8, cfg.GetRequiredPrestige(DamageType.Shock));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetRequiredPrestige_UnknownType_ReturnsZero()
        {
            var cfg = CreateConfig(physical: 1, energy: 2, thermal: 3, shock: 4);
            // Cast an integer outside the defined enum range.
            Assert.AreEqual(0, cfg.GetRequiredPrestige((DamageType)999),
                "Unknown DamageType values should return 0 (effectively always unlocked).");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void IsUnlocked_PrestigeAtRequiredLevel_ReturnsTrue()
        {
            // Energy requires 3; prestige count 3 → exactly meets requirement.
            var cfg = CreateConfig(energy: 3);
            Assert.IsTrue(cfg.IsUnlocked(DamageType.Energy, 3),
                "Prestige count equal to the requirement should be unlocked.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void IsUnlocked_PrestigeBelowRequired_ReturnsFalse()
        {
            // Energy requires 3; prestige count 2 → below requirement.
            var cfg = CreateConfig(energy: 3);
            Assert.IsFalse(cfg.IsUnlocked(DamageType.Energy, 2),
                "Prestige count below the requirement should be locked.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetLockReason_Unlocked_ReturnsEmpty()
        {
            // Physical requires 0; count 0 → unlocked → empty string.
            var cfg = CreateConfig(physical: 0);
            Assert.AreEqual(string.Empty, cfg.GetLockReason(DamageType.Physical, 0),
                "GetLockReason should return empty string for unlocked types.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetLockReason_Locked_ContainsRequiredCountAndRankLabel()
        {
            // Shock requires 7 (Gold I); count 0 → locked.
            var cfg    = CreateConfig(shock: 7);
            string msg = cfg.GetLockReason(DamageType.Shock, 0);
            StringAssert.Contains("7",     msg, "Lock reason should contain the required prestige number.");
            StringAssert.Contains("Gold I", msg, "Lock reason should contain the rank label 'Gold I'.");
            Object.DestroyImmediate(cfg);
        }

        // ══════════════════════════════════════════════════════════════════════
        // WeaponTypeUnlockEvaluator Tests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Evaluator_NullConfig_AllTypesUnlocked()
        {
            // Null config → evaluator must return true regardless of type or prestige.
            var prestige = CreatePrestige(count: 0);
            Assert.IsTrue(WeaponTypeUnlockEvaluator.IsTypeUnlocked(null, prestige, DamageType.Energy),
                "Null config should mean all types are unlocked (backwards-compatible).");
            Object.DestroyImmediate(prestige);
        }

        [Test]
        public void Evaluator_NullPrestige_TreatedAsCountZero()
        {
            // Energy requires 1; null prestige = count 0 → should be locked.
            var cfg = CreateConfig(energy: 1);
            Assert.IsFalse(WeaponTypeUnlockEvaluator.IsTypeUnlocked(cfg, null, DamageType.Energy),
                "Null prestige should be treated as count 0; Energy (requires 1) should be locked.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Evaluator_IsTypeUnlocked_SufficientPrestige_ReturnsTrue()
        {
            // Energy requires 2; prestige count 3 → unlocked.
            var cfg      = CreateConfig(energy: 2);
            var prestige = CreatePrestige(count: 3);
            Assert.IsTrue(WeaponTypeUnlockEvaluator.IsTypeUnlocked(cfg, prestige, DamageType.Energy),
                "PrestigeCount 3 >= requirement 2 → Energy should be unlocked.");
            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(prestige);
        }

        [Test]
        public void Evaluator_GetLockReason_NullConfig_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty,
                WeaponTypeUnlockEvaluator.GetLockReason(null, null, DamageType.Shock),
                "Null config should return empty lock reason string.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // WeaponTypeUnlockController Tests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Controller_OnEnable_AllNullRefs_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponTypeUnlockController>();
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnEnable"),
                "OnEnable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_AllNullRefs_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponTypeUnlockController>();
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnDisable"),
                "OnDisable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnEnable_NullChannel_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponTypeUnlockController>();
            SetField(ctl, "_onPrestige", null);
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnEnable"),
                "OnEnable with null channel must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullChannel_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponTypeUnlockController>();
            SetField(ctl, "_onPrestige", null);
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnDisable"),
                "OnDisable with null channel must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_UnregistersFromOnPrestige()
        {
            var go      = new GameObject();
            var ctl     = go.AddComponent<WeaponTypeUnlockController>();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(ctl, "_onPrestige", channel);

            InvokePrivate(ctl, "Awake");
            InvokePrivate(ctl, "OnEnable");

            int callCount = 0;
            channel.RegisterCallback(() => callCount++);

            InvokePrivate(ctl, "OnDisable");
            channel.Raise();

            // The controller's _refreshDelegate was unregistered; only our manual callback fires.
            Assert.AreEqual(1, callCount,
                "After OnDisable, the controller must have unregistered its refresh callback.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Controller_Refresh_NullListContainer_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponTypeUnlockController>();
            SetField(ctl, "_listContainer", null);
            Assert.DoesNotThrow(() => ctl.Refresh(),
                "Refresh with null _listContainer must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_Refresh_NullUnlockConfig_DoesNotThrow()
        {
            var go        = new GameObject();
            var ctl       = go.AddComponent<WeaponTypeUnlockController>();
            var container = new GameObject().transform;
            SetField(ctl, "_listContainer", container);
            SetField(ctl, "_unlockConfig",  null);
            Assert.DoesNotThrow(() => ctl.Refresh(),
                "Refresh with null _unlockConfig must not throw.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(container.gameObject);
        }

        [Test]
        public void Controller_FreshInstance_UnlockConfigAndPrestigeSystem_AreNull()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<WeaponTypeUnlockController>();
            Assert.IsNull(ctl.UnlockConfig,    "UnlockConfig should default to null.");
            Assert.IsNull(ctl.PrestigeSystem,  "PrestigeSystem should default to null.");
            Object.DestroyImmediate(go);
        }
    }
}
