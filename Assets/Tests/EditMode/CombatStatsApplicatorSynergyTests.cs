using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for the build-synergy integration inside
    /// <see cref="CombatStatsApplicator.ApplyStats"/>.
    ///
    /// Covers:
    ///   • Null _synergyConfig / _playerLoadout / _shopCatalog → no throw, stats unchanged.
    ///   • All three assigned, one synergy condition satisfied → health bonus applied
    ///     and reflected in HealthSO after ApplyStats.
    ///   • Speed bonus from active synergy pushed to RobotLocomotionController.
    ///   • No active synergies (condition not met) → base stats unchanged.
    /// </summary>
    public class CombatStatsApplicatorSynergyTests
    {
        // ── Scene objects ─────────────────────────────────────────────────────
        private GameObject            _go;
        private CombatStatsApplicator _applicator;

        // ── Mandatory SOs ─────────────────────────────────────────────────────
        private RobotDefinition  _robotDef;
        private HealthSO         _health;
        private VoidGameEvent    _matchStarted;

        // ── Synergy SOs ───────────────────────────────────────────────────────
        private PartSynergyConfig _synergyConfig;
        private PlayerLoadout     _playerLoadout;
        private ShopCatalog       _shopCatalog;
        private PartDefinition    _part;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void SetFieldOnType(Type type, object target, string name, object value)
        {
            FieldInfo fi = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {type.Name}.");
            fi.SetValue(target, value);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go         = new GameObject("TestApplicator_Synergy");
            _applicator = _go.AddComponent<CombatStatsApplicator>();

            _robotDef     = ScriptableObject.CreateInstance<RobotDefinition>();
            _health       = ScriptableObject.CreateInstance<HealthSO>();
            _matchStarted = ScriptableObject.CreateInstance<VoidGameEvent>();

            // Default robot: HP=100, speed=5 (matching RobotDefinition field defaults)
            _health.Reset();

            // Mandatory fields on applicator
            SetField(_applicator, "_robotDefinition",   _robotDef);
            SetField(_applicator, "_health",            _health);
            SetField(_applicator, "_matchStartedEvent", _matchStarted);

            // ── Synergy objects ───────────────────────────────────────────────
            _part          = ScriptableObject.CreateInstance<PartDefinition>();
            _shopCatalog   = ScriptableObject.CreateInstance<ShopCatalog>();
            _playerLoadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            _synergyConfig = ScriptableObject.CreateInstance<PartSynergyConfig>();

            // Part: id="weapon_rare", category=Weapon, rarity=Rare
            SetFieldOnType(typeof(PartDefinition), _part, "_partId",   "weapon_rare");
            SetFieldOnType(typeof(PartDefinition), _part, "_category", PartCategory.Weapon);
            SetFieldOnType(typeof(PartDefinition), _part, "_rarity",   PartRarity.Rare);
            // Neutral combat stats so base-stat tests remain clean
            SetFieldOnType(typeof(PartDefinition), _part, "_stats", PartStats.Default);

            // Catalog contains the part
            SetFieldOnType(typeof(ShopCatalog), _shopCatalog, "_parts",
                new List<PartDefinition> { _part });

            // Player has the part equipped
            _playerLoadout.LoadSnapshot(new List<string> { "weapon_rare" });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_robotDef);
            Object.DestroyImmediate(_health);
            Object.DestroyImmediate(_matchStarted);
            Object.DestroyImmediate(_part);
            Object.DestroyImmediate(_shopCatalog);
            Object.DestroyImmediate(_playerLoadout);
            Object.DestroyImmediate(_synergyConfig);
        }

        // ── Null-safety ───────────────────────────────────────────────────────

        [Test]
        public void ApplyStats_NullSynergyConfig_DoesNotThrow()
        {
            // _synergyConfig, _playerLoadout, _shopCatalog all left as null (default)
            Assert.DoesNotThrow(() => _applicator.ApplyStats());
        }

        [Test]
        public void ApplyStats_NullPlayerLoadout_DoesNotThrow()
        {
            SetField(_applicator, "_synergyConfig", _synergyConfig);
            // _playerLoadout and _shopCatalog left null
            Assert.DoesNotThrow(() => _applicator.ApplyStats());
        }

        [Test]
        public void ApplyStats_NullShopCatalog_DoesNotThrow()
        {
            SetField(_applicator, "_synergyConfig",  _synergyConfig);
            SetField(_applicator, "_playerLoadout",  _playerLoadout);
            // _shopCatalog left null
            Assert.DoesNotThrow(() => _applicator.ApplyStats());
        }

        // ── Health bonus via active synergy ───────────────────────────────────

        [Test]
        public void ApplyStats_WithActiveSynergy_AppliesHealthBonus()
        {
            // Configure a synergy: 1× Weapon at Rare+ → +50 HP
            var entry = new PartSynergyEntry
            {
                displayName           = "Blade Master",
                bonusDescription      = "+50 HP",
                requirements          = new List<PartSynergyRequirement>
                {
                    new PartSynergyRequirement
                    {
                        requiredCategory = PartCategory.Weapon,
                        minimumRarity    = PartRarity.Rare,
                        requiredCount    = 1,
                    }
                },
                healthBonus           = 50,
                speedMultiplierBonus  = 0f,
                damageMultiplierBonus = 0f,
                armorBonus            = 0,
            };
            SetFieldOnType(typeof(PartSynergyConfig), _synergyConfig, "_entries",
                new List<PartSynergyEntry> { entry });

            // Wire synergy system to applicator
            SetField(_applicator, "_synergyConfig", _synergyConfig);
            SetField(_applicator, "_playerLoadout",  _playerLoadout);
            SetField(_applicator, "_shopCatalog",    _shopCatalog);

            _applicator.ApplyStats();

            // RobotDefinition default HP=100, synergy adds 50 → expect 150
            Assert.AreEqual(150f, _health.MaxHealth,     0.001f);
            Assert.AreEqual(150f, _health.CurrentHealth, 0.001f);
        }

        // ── Speed bonus via active synergy ────────────────────────────────────

        [Test]
        public void ApplyStats_WithActiveSynergy_AppliesSpeedBonus()
        {
            // Configure a synergy: 1× Weapon at Rare+ → +20% speed
            var entry = new PartSynergyEntry
            {
                displayName           = "Speed Demon",
                bonusDescription      = "+20% Speed",
                requirements          = new List<PartSynergyRequirement>
                {
                    new PartSynergyRequirement
                    {
                        requiredCategory = PartCategory.Weapon,
                        minimumRarity    = PartRarity.Rare,
                        requiredCount    = 1,
                    }
                },
                healthBonus           = 0,
                speedMultiplierBonus  = 0.20f,
                damageMultiplierBonus = 0f,
                armorBonus            = 0,
            };
            SetFieldOnType(typeof(PartSynergyConfig), _synergyConfig, "_entries",
                new List<PartSynergyEntry> { entry });

            SetField(_applicator, "_synergyConfig", _synergyConfig);
            SetField(_applicator, "_playerLoadout",  _playerLoadout);
            SetField(_applicator, "_shopCatalog",    _shopCatalog);

            // Wire locomotion so we can verify speed
            var locoGo = new GameObject("Loco");
            var loco   = locoGo.AddComponent<RobotLocomotionController>();
            SetField(_applicator, "_locomotion", loco);

            _applicator.ApplyStats();

            // RobotDefinition default speed=5, synergy × 1.20 → 6.0
            Assert.AreEqual(5f * 1.20f, loco.BaseSpeed, 0.001f);

            Object.DestroyImmediate(locoGo);
        }

        // ── No active synergy when condition not met ──────────────────────────

        [Test]
        public void ApplyStats_NoActiveSynergies_BaseStatsUnchanged()
        {
            // Synergy requires Epic rarity, but part is only Rare → condition not met
            var entry = new PartSynergyEntry
            {
                displayName           = "Titan Build",
                bonusDescription      = "+100 HP",
                requirements          = new List<PartSynergyRequirement>
                {
                    new PartSynergyRequirement
                    {
                        requiredCategory = PartCategory.Weapon,
                        minimumRarity    = PartRarity.Epic,  // higher than part's Rare
                        requiredCount    = 1,
                    }
                },
                healthBonus           = 100,
                speedMultiplierBonus  = 0f,
                damageMultiplierBonus = 0f,
                armorBonus            = 0,
            };
            SetFieldOnType(typeof(PartSynergyConfig), _synergyConfig, "_entries",
                new List<PartSynergyEntry> { entry });

            SetField(_applicator, "_synergyConfig", _synergyConfig);
            SetField(_applicator, "_playerLoadout",  _playerLoadout);
            SetField(_applicator, "_shopCatalog",    _shopCatalog);

            _applicator.ApplyStats();

            // Synergy should NOT activate; HP must remain at robot base (100)
            Assert.AreEqual(100f, _health.MaxHealth,     0.001f);
            Assert.AreEqual(100f, _health.CurrentHealth, 0.001f);
        }
    }
}
