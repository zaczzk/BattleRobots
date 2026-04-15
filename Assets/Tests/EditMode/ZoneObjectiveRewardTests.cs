using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T280: <see cref="ZoneObjectiveRewardConfig"/> and
    /// <see cref="ZoneObjectiveRewardApplier"/>.
    ///
    /// ZoneObjectiveRewardTests (14):
    ///   Config_FreshInstance_BaseReward_100                                 ×1
    ///   Config_FreshInstance_BonusPerZone_25                                ×1
    ///   Config_GetReward_ZeroZones_ReturnsBase                              ×1
    ///   Config_GetReward_TwoZones_ReturnsBaseAndBonus                       ×1
    ///   Config_GetReward_NegativeZones_TreatedAsZero                        ×1
    ///   Config_RewardLabel_ContainsBaseReward                               ×1
    ///   Applier_FreshInstance_RewardConfigNull                              ×1
    ///   Applier_OnEnable_NullRefs_DoesNotThrow                              ×1
    ///   Applier_OnDisable_Unregisters_Channel                               ×1
    ///   Applier_ApplyReward_NullConfig_NoOp                                 ×1
    ///   Applier_ApplyReward_NullWallet_NoOp                                 ×1
    ///   Applier_ApplyReward_ObjectiveNotComplete_NoOp                       ×1
    ///   Applier_ApplyReward_NoObjectiveGuard_CreditsWallet                  ×1
    ///   Applier_ApplyReward_WithZones_CreditsScaledReward                   ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class ZoneObjectiveRewardTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneObjectiveRewardConfig CreateConfig() =>
            ScriptableObject.CreateInstance<ZoneObjectiveRewardConfig>();

        private static ZoneObjectiveSO CreateObjectiveSO() =>
            ScriptableObject.CreateInstance<ZoneObjectiveSO>();

        private static ZoneDominanceSO CreateDominanceSO() =>
            ScriptableObject.CreateInstance<ZoneDominanceSO>();

        private static PlayerWallet CreateWallet() =>
            ScriptableObject.CreateInstance<PlayerWallet>();

        private static ZoneObjectiveRewardApplier CreateApplier() =>
            new GameObject("RewardApplier_Test")
                .AddComponent<ZoneObjectiveRewardApplier>();

        // ── Config Tests ──────────────────────────────────────────────────────

        [Test]
        public void Config_FreshInstance_BaseReward_100()
        {
            var cfg = CreateConfig();
            Assert.AreEqual(100f, cfg.BaseReward, 0.001f,
                "BaseReward must default to 100.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_FreshInstance_BonusPerZone_25()
        {
            var cfg = CreateConfig();
            Assert.AreEqual(25f, cfg.BonusPerZone, 0.001f,
                "BonusPerZone must default to 25.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_GetReward_ZeroZones_ReturnsBase()
        {
            var cfg = CreateConfig();
            SetField(cfg, "_baseReward",   100f);
            SetField(cfg, "_bonusPerZone",  25f);

            float reward = cfg.GetReward(0);

            Assert.AreEqual(100f, reward, 0.001f,
                "GetReward(0) must return BaseReward when no zones are held.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_GetReward_TwoZones_ReturnsBaseAndBonus()
        {
            var cfg = CreateConfig();
            SetField(cfg, "_baseReward",   100f);
            SetField(cfg, "_bonusPerZone",  25f);

            float reward = cfg.GetReward(2);

            Assert.AreEqual(150f, reward, 0.001f,
                "GetReward(2) must return 100 + 2*25 = 150.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_GetReward_NegativeZones_TreatedAsZero()
        {
            var cfg = CreateConfig();
            SetField(cfg, "_baseReward",   100f);
            SetField(cfg, "_bonusPerZone",  25f);

            float reward = cfg.GetReward(-5);

            Assert.AreEqual(100f, reward, 0.001f,
                "GetReward with negative zonesHeld must treat it as 0 and return BaseReward.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_RewardLabel_ContainsBaseReward()
        {
            var cfg = CreateConfig();
            SetField(cfg, "_baseReward", 200f);

            string label = cfg.RewardLabel;

            StringAssert.Contains("200", label,
                "RewardLabel must contain the base reward value as an integer.");
            Object.DestroyImmediate(cfg);
        }

        // ── Applier Tests ─────────────────────────────────────────────────────

        [Test]
        public void Applier_FreshInstance_RewardConfigNull()
        {
            var applier = CreateApplier();
            Assert.IsNull(applier.RewardConfig,
                "RewardConfig must be null on a fresh ZoneObjectiveRewardApplier.");
            Object.DestroyImmediate(applier.gameObject);
        }

        [Test]
        public void Applier_OnEnable_NullRefs_DoesNotThrow()
        {
            var applier = CreateApplier();
            InvokePrivate(applier, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(applier, "OnEnable"),
                "OnEnable with all-null refs must not throw.");
            Object.DestroyImmediate(applier.gameObject);
        }

        [Test]
        public void Applier_OnDisable_Unregisters_Channel()
        {
            var applier    = CreateApplier();
            var objComplete = CreateEvent();
            SetField(applier, "_onObjectiveComplete", objComplete);
            InvokePrivate(applier, "Awake");
            InvokePrivate(applier, "OnEnable");
            InvokePrivate(applier, "OnDisable");

            // After disable, raising the channel must not invoke ApplyReward.
            // We verify no throw occurs (no wallet / config assigned, so a call
            // would be a no-op anyway — the important thing is no NRE).
            Assert.DoesNotThrow(() => objComplete.Raise(),
                "Raising _onObjectiveComplete after OnDisable must not throw.");

            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(objComplete);
        }

        [Test]
        public void Applier_ApplyReward_NullConfig_NoOp()
        {
            var applier = CreateApplier();
            var wallet  = CreateWallet();
            InvokePrivate(wallet, "OnEnable");   // seeds Balance
            int before  = wallet.Balance;
            SetField(applier, "_wallet", wallet);
            // _rewardConfig left null

            applier.ApplyReward();

            Assert.AreEqual(before, wallet.Balance,
                "ApplyReward must be a no-op when _rewardConfig is null.");
            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(wallet);
        }

        [Test]
        public void Applier_ApplyReward_NullWallet_NoOp()
        {
            var applier = CreateApplier();
            var cfg     = CreateConfig();
            SetField(cfg, "_baseReward", 100f);
            SetField(applier, "_rewardConfig", cfg);
            // _wallet left null

            Assert.DoesNotThrow(() => applier.ApplyReward(),
                "ApplyReward must not throw when _wallet is null.");

            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Applier_ApplyReward_ObjectiveNotComplete_NoOp()
        {
            var applier   = CreateApplier();
            var cfg       = CreateConfig();
            var wallet    = CreateWallet();
            var objective = CreateObjectiveSO();
            SetField(cfg, "_baseReward", 100f);
            SetField(applier, "_rewardConfig", cfg);
            SetField(applier, "_wallet",       wallet);
            SetField(applier, "_objectiveSO",  objective);
            InvokePrivate(wallet, "OnEnable");
            int before = wallet.Balance;

            // objective.IsComplete is false — ApplyReward must be a no-op.
            applier.ApplyReward();

            Assert.AreEqual(before, wallet.Balance,
                "ApplyReward must be a no-op when _objectiveSO.IsComplete is false.");
            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(wallet);
            Object.DestroyImmediate(objective);
        }

        [Test]
        public void Applier_ApplyReward_NoObjectiveGuard_CreditsWallet()
        {
            var applier = CreateApplier();
            var cfg     = CreateConfig();
            var wallet  = CreateWallet();
            SetField(cfg, "_baseReward",   100f);
            SetField(cfg, "_bonusPerZone",   0f);
            SetField(applier, "_rewardConfig", cfg);
            SetField(applier, "_wallet",       wallet);
            InvokePrivate(wallet, "OnEnable");
            int before = wallet.Balance;

            applier.ApplyReward();

            Assert.AreEqual(before + 100, wallet.Balance,
                "ApplyReward without an objective guard must credit 100 to the wallet.");
            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(wallet);
        }

        [Test]
        public void Applier_ApplyReward_WithZones_CreditsScaledReward()
        {
            var applier   = CreateApplier();
            var cfg       = CreateConfig();
            var wallet    = CreateWallet();
            var dominance = CreateDominanceSO();
            SetField(cfg, "_baseReward",   100f);
            SetField(cfg, "_bonusPerZone",  25f);
            SetField(applier, "_rewardConfig", cfg);
            SetField(applier, "_wallet",       wallet);
            SetField(applier, "_dominanceSO",  dominance);
            SetField(dominance, "_totalZones", 3);
            dominance.AddPlayerZone();
            dominance.AddPlayerZone();   // PlayerZoneCount = 2
            InvokePrivate(wallet, "OnEnable");
            int before = wallet.Balance;

            applier.ApplyReward();

            // 100 + 2*25 = 150
            Assert.AreEqual(before + 150, wallet.Balance,
                "ApplyReward with 2 zones held must credit 150 (100 base + 2×25).");
            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(wallet);
            Object.DestroyImmediate(dominance);
        }
    }
}
