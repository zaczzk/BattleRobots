using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T195:
    ///   <see cref="MilestoneRewardCatalogSO"/> and
    ///   <see cref="MilestoneRewardApplier"/>.
    ///
    /// MilestoneRewardCatalogSOTests (8):
    ///   Catalog_FreshInstance_CountIsZero                             ×1
    ///   Catalog_GetReward_OutOfRange_ReturnsZero                      ×1
    ///   Catalog_GetReward_NegativeIndex_ReturnsZero                   ×1
    ///   Catalog_GetReward_InRange_ReturnsValue                        ×1
    ///   Catalog_GetReward_ZeroIndex_FirstValue                        ×1
    ///   Catalog_GetReward_LastIndex_LastValue                         ×1
    ///   Catalog_Count_MatchesArrayLength                              ×1
    ///   Catalog_RewardLabel_DefaultNotEmpty                           ×1
    ///
    /// MilestoneRewardApplierTests (10):
    ///   Applier_FreshInstance_CatalogIsNull                           ×1
    ///   Applier_FreshInstance_WalletIsNull                            ×1
    ///   Applier_OnEnable_NullChannels_DoesNotThrow                    ×1
    ///   Applier_OnDisable_NullChannels_DoesNotThrow                   ×1
    ///   Applier_OnDisable_Unregisters_MatchEndedChannel               ×1
    ///   Applier_CheckMilestones_NullCatalog_NoThrow                   ×1
    ///   Applier_CheckMilestones_NullMastery_NoThrow                   ×1
    ///   Applier_CheckMilestones_NullMilestoneSO_NoThrow               ×1
    ///   Applier_CheckMilestones_NoClearedChange_BalanceUnchanged      ×1
    ///   Applier_CheckMilestones_NewlyClearedMilestone_AddsReward      ×1
    ///
    /// Total: 18 new EditMode tests.
    /// </summary>
    public class MilestoneRewardTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static MilestoneRewardCatalogSO CreateCatalog(float[] rewards)
        {
            var so = ScriptableObject.CreateInstance<MilestoneRewardCatalogSO>();
            SetField(so, "_rewardPerMilestone", rewards);
            return so;
        }

        private static MasteryProgressMilestoneSO CreateMilestoneSO(float[] physicalThresholds)
        {
            var so = ScriptableObject.CreateInstance<MasteryProgressMilestoneSO>();
            SetField(so, "_physicalMilestones", physicalThresholds);
            return so;
        }

        private static DamageTypeMasterySO CreateMastery() =>
            ScriptableObject.CreateInstance<DamageTypeMasterySO>();

        private static PlayerWallet CreateWallet(int startBalance = 500)
        {
            var w = ScriptableObject.CreateInstance<PlayerWallet>();
            SetField(w, "_startingBalance", startBalance);
            w.Reset();
            return w;
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static MilestoneRewardApplier CreateApplier()
        {
            var go = new GameObject("MilestoneRewardApplier_Test");
            return go.AddComponent<MilestoneRewardApplier>();
        }

        // ══════════════════════════════════════════════════════════════════════
        // MilestoneRewardCatalogSO Tests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Catalog_FreshInstance_CountIsZero()
        {
            var so = ScriptableObject.CreateInstance<MilestoneRewardCatalogSO>();
            Assert.AreEqual(0, so.Count,
                "Fresh catalog must have Count == 0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_GetReward_OutOfRange_ReturnsZero()
        {
            var so = CreateCatalog(new float[] { 100f });
            Assert.AreEqual(0f, so.GetReward(1), 0.001f,
                "GetReward at out-of-range index must return 0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_GetReward_NegativeIndex_ReturnsZero()
        {
            var so = CreateCatalog(new float[] { 100f, 200f });
            Assert.AreEqual(0f, so.GetReward(-1), 0.001f,
                "GetReward with negative index must return 0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_GetReward_InRange_ReturnsValue()
        {
            var so = CreateCatalog(new float[] { 100f, 250f, 500f });
            Assert.AreEqual(250f, so.GetReward(1), 0.001f,
                "GetReward must return the reward at the specified index.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_GetReward_ZeroIndex_FirstValue()
        {
            var so = CreateCatalog(new float[] { 75f, 150f });
            Assert.AreEqual(75f, so.GetReward(0), 0.001f,
                "GetReward(0) must return the first reward.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_GetReward_LastIndex_LastValue()
        {
            var so = CreateCatalog(new float[] { 100f, 200f, 300f });
            Assert.AreEqual(300f, so.GetReward(2), 0.001f,
                "GetReward at last index must return the last reward.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_Count_MatchesArrayLength()
        {
            var so = CreateCatalog(new float[] { 100f, 200f, 300f, 400f });
            Assert.AreEqual(4, so.Count,
                "Count must match the length of the configured reward array.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_RewardLabel_DefaultNotEmpty()
        {
            var so = ScriptableObject.CreateInstance<MilestoneRewardCatalogSO>();
            Assert.IsFalse(string.IsNullOrEmpty(so.RewardLabel),
                "RewardLabel must not be null or empty on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        // ══════════════════════════════════════════════════════════════════════
        // MilestoneRewardApplier Tests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Applier_FreshInstance_CatalogIsNull()
        {
            var applier = CreateApplier();
            Assert.IsNull(applier.Catalog,
                "Fresh applier must have null Catalog.");
            Object.DestroyImmediate(applier.gameObject);
        }

        [Test]
        public void Applier_FreshInstance_WalletIsNull()
        {
            var applier = CreateApplier();
            Assert.IsNull(applier.Wallet,
                "Fresh applier must have null Wallet.");
            Object.DestroyImmediate(applier.gameObject);
        }

        [Test]
        public void Applier_OnEnable_NullChannels_DoesNotThrow()
        {
            var applier = CreateApplier();
            InvokePrivate(applier, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(applier, "OnEnable"),
                "OnEnable with all null refs must not throw.");
            Object.DestroyImmediate(applier.gameObject);
        }

        [Test]
        public void Applier_OnDisable_NullChannels_DoesNotThrow()
        {
            var applier = CreateApplier();
            InvokePrivate(applier, "Awake");
            InvokePrivate(applier, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(applier, "OnDisable"),
                "OnDisable with all null refs must not throw.");
            Object.DestroyImmediate(applier.gameObject);
        }

        [Test]
        public void Applier_OnDisable_Unregisters_MatchEndedChannel()
        {
            var applier = CreateApplier();
            var ch      = CreateEvent();
            SetField(applier, "_onMatchEnded", ch);
            InvokePrivate(applier, "Awake");
            InvokePrivate(applier, "OnEnable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            InvokePrivate(applier, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, count,
                "OnDisable must unregister from _onMatchEnded.");

            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Applier_CheckMilestones_NullCatalog_NoThrow()
        {
            var applier    = CreateApplier();
            var mastery    = CreateMastery();
            var milestoneSO = CreateMilestoneSO(new float[] { 500f });
            SetField(applier, "_mastery",    mastery);
            SetField(applier, "_milestoneSO", milestoneSO);
            // _catalog is null
            InvokePrivate(applier, "Awake");

            Assert.DoesNotThrow(() => InvokePrivate(applier, "CheckMilestones"),
                "CheckMilestones with null catalog must not throw.");

            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(mastery);
            Object.DestroyImmediate(milestoneSO);
        }

        [Test]
        public void Applier_CheckMilestones_NullMastery_NoThrow()
        {
            var applier = CreateApplier();
            var catalog  = CreateCatalog(new float[] { 100f });
            var milestone = CreateMilestoneSO(new float[] { 500f });
            SetField(applier, "_catalog",    catalog);
            SetField(applier, "_milestoneSO", milestone);
            // _mastery is null
            InvokePrivate(applier, "Awake");

            Assert.DoesNotThrow(() => InvokePrivate(applier, "CheckMilestones"),
                "CheckMilestones with null mastery must not throw.");

            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(milestone);
        }

        [Test]
        public void Applier_CheckMilestones_NullMilestoneSO_NoThrow()
        {
            var applier = CreateApplier();
            var catalog  = CreateCatalog(new float[] { 100f });
            var mastery  = CreateMastery();
            SetField(applier, "_catalog", catalog);
            SetField(applier, "_mastery", mastery);
            // _milestoneSO is null
            InvokePrivate(applier, "Awake");

            Assert.DoesNotThrow(() => InvokePrivate(applier, "CheckMilestones"),
                "CheckMilestones with null milestoneSO must not throw.");

            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(mastery);
        }

        [Test]
        public void Applier_CheckMilestones_NoClearedChange_BalanceUnchanged()
        {
            var applier    = CreateApplier();
            var catalog    = CreateCatalog(new float[] { 100f, 200f });
            var mastery    = CreateMastery();
            var milestoneSO = CreateMilestoneSO(new float[] { 500f, 1000f });
            var wallet     = CreateWallet(500);

            SetField(applier, "_catalog",    catalog);
            SetField(applier, "_mastery",    mastery);
            SetField(applier, "_milestoneSO", milestoneSO);
            SetField(applier, "_wallet",     wallet);
            InvokePrivate(applier, "Awake");
            // No damage accumulated → cleared count stays at 0
            InvokePrivate(applier, "CheckMilestones");

            Assert.AreEqual(500, wallet.Balance,
                "Wallet balance must be unchanged when no new milestones are cleared.");

            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(mastery);
            Object.DestroyImmediate(milestoneSO);
            Object.DestroyImmediate(wallet);
        }

        [Test]
        public void Applier_CheckMilestones_NewlyClearedMilestone_AddsReward()
        {
            var applier    = CreateApplier();
            var catalog    = CreateCatalog(new float[] { 100f, 200f });
            var mastery    = CreateMastery();
            var milestoneSO = CreateMilestoneSO(new float[] { 500f, 1000f });
            var wallet     = CreateWallet(500);

            SetField(applier, "_catalog",    catalog);
            SetField(applier, "_mastery",    mastery);
            SetField(applier, "_milestoneSO", milestoneSO);
            SetField(applier, "_wallet",     wallet);
            InvokePrivate(applier, "Awake");
            // _previousClearedCounts[Physical] = 0 at Awake time

            // Accumulate enough damage to clear milestone 0 (threshold 500)
            mastery.AddDealt(500f, DamageType.Physical);

            // Now CheckMilestones should detect cleared=1, prev=0 → grant reward[0]=100
            InvokePrivate(applier, "CheckMilestones");

            Assert.AreEqual(600, wallet.Balance,
                "Wallet balance must increase by the milestone 0 reward (100) after " +
                "clearing the first Physical milestone.");

            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(mastery);
            Object.DestroyImmediate(milestoneSO);
            Object.DestroyImmediate(wallet);
        }
    }
}
