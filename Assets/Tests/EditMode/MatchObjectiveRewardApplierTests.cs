using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T235: <see cref="MatchObjectiveRewardApplier"/>.
    ///
    /// MatchObjectiveRewardApplierTests (16):
    ///   FreshInstance_BonusObjectiveNull                    ×1
    ///   FreshInstance_PersistenceNull                       ×1
    ///   FreshInstance_WalletNull                            ×1
    ///   OnEnable_NullRefs_DoesNotThrow                      ×1
    ///   OnDisable_NullRefs_DoesNotThrow                     ×1
    ///   OnDisable_Unregisters                               ×1
    ///   ApplyReward_NullBonusObjective_NoThrow              ×1
    ///   ApplyReward_NullWallet_NoThrow                      ×1
    ///   ApplyReward_NullPersistence_NoThrow                 ×1
    ///   ApplyReward_AddsCorrectAmountToWallet               ×1
    ///   ApplyReward_RecordsPersistenceCompleted             ×1
    ///   ApplyReward_RecordsPersistenceTitle                 ×1
    ///   ApplyReward_FiresOnRewardApplied                    ×1
    ///   OnObjectiveCompleted_Channel_TriggersApplyReward    ×1
    ///   ApplyReward_ZeroReward_SkipsWalletCall              ×1
    ///   ApplyReward_MultipleCompletions_AccumulatesInWallet ×1
    ///
    /// Total: 16 new EditMode tests.
    /// </summary>
    public class MatchObjectiveRewardApplierTests
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

        private static MatchObjectiveRewardApplier CreateApplier() =>
            new GameObject("ObjRewardApplier_Test").AddComponent<MatchObjectiveRewardApplier>();

        private static VoidGameEvent CreateVoidEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static MatchBonusObjectiveSO CreateBonusSO(int reward = 100)
        {
            var so = ScriptableObject.CreateInstance<MatchBonusObjectiveSO>();
            SetField(so, "_bonusReward", reward);
            InvokePrivate(so, "OnEnable");
            return so;
        }

        private static MatchObjectivePersistenceSO CreatePersistenceSO()
        {
            var so = ScriptableObject.CreateInstance<MatchObjectivePersistenceSO>();
            InvokePrivate(so, "OnEnable"); // calls Reset()
            return so;
        }

        private static PlayerWallet CreateWallet(int startBalance = 500)
        {
            var so = ScriptableObject.CreateInstance<PlayerWallet>();
            SetField(so, "_startingBalance", startBalance);
            so.Reset();
            return so;
        }

        // ── Fresh-instance property tests ─────────────────────────────────────

        [Test]
        public void FreshInstance_BonusObjectiveNull()
        {
            var applier = CreateApplier();
            Assert.IsNull(applier.BonusObjective,
                "BonusObjective must be null on a fresh instance.");
            Object.DestroyImmediate(applier.gameObject);
        }

        [Test]
        public void FreshInstance_PersistenceNull()
        {
            var applier = CreateApplier();
            Assert.IsNull(applier.Persistence,
                "Persistence must be null on a fresh instance.");
            Object.DestroyImmediate(applier.gameObject);
        }

        [Test]
        public void FreshInstance_WalletNull()
        {
            var applier = CreateApplier();
            Assert.IsNull(applier.Wallet,
                "Wallet must be null on a fresh instance.");
            Object.DestroyImmediate(applier.gameObject);
        }

        // ── Lifecycle null-safety ─────────────────────────────────────────────

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var applier = CreateApplier();
            InvokePrivate(applier, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(applier, "OnEnable"),
                "OnEnable with all-null refs must not throw.");
            Object.DestroyImmediate(applier.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var applier = CreateApplier();
            InvokePrivate(applier, "Awake");
            InvokePrivate(applier, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(applier, "OnDisable"),
                "OnDisable with all-null refs must not throw.");
            Object.DestroyImmediate(applier.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters()
        {
            var applier = CreateApplier();
            var ch      = CreateVoidEvent();
            SetField(applier, "_onObjectiveCompleted", ch);
            InvokePrivate(applier, "Awake");
            InvokePrivate(applier, "OnEnable");
            InvokePrivate(applier, "OnDisable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            ch.Raise();

            Assert.AreEqual(1, count,
                "After OnDisable only the manually registered callback must fire.");
            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(ch);
        }

        // ── ApplyReward null-safety ───────────────────────────────────────────

        [Test]
        public void ApplyReward_NullBonusObjective_NoThrow()
        {
            var applier = CreateApplier();
            // _bonusObjective remains null
            Assert.DoesNotThrow(() => applier.ApplyReward(),
                "ApplyReward with null BonusObjective must not throw.");
            Object.DestroyImmediate(applier.gameObject);
        }

        [Test]
        public void ApplyReward_NullWallet_NoThrow()
        {
            var applier = CreateApplier();
            var bonus   = CreateBonusSO(50);
            SetField(applier, "_bonusObjective", bonus);
            // _wallet remains null
            Assert.DoesNotThrow(() => applier.ApplyReward(),
                "ApplyReward with null Wallet must not throw.");
            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(bonus);
        }

        [Test]
        public void ApplyReward_NullPersistence_NoThrow()
        {
            var applier = CreateApplier();
            var bonus   = CreateBonusSO(50);
            SetField(applier, "_bonusObjective", bonus);
            // _persistence remains null
            Assert.DoesNotThrow(() => applier.ApplyReward(),
                "ApplyReward with null Persistence must not throw.");
            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(bonus);
        }

        // ── ApplyReward functional tests ──────────────────────────────────────

        [Test]
        public void ApplyReward_AddsCorrectAmountToWallet()
        {
            var applier     = CreateApplier();
            var bonus       = CreateBonusSO(reward: 100);
            var wallet      = CreateWallet(startBalance: 500);
            SetField(applier, "_bonusObjective", bonus);
            SetField(applier, "_wallet",         wallet);

            applier.ApplyReward();

            Assert.AreEqual(600, wallet.Balance,
                "Wallet balance must increase by BonusReward after ApplyReward.");
            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(bonus);
            Object.DestroyImmediate(wallet);
        }

        [Test]
        public void ApplyReward_RecordsPersistenceCompleted()
        {
            var applier     = CreateApplier();
            var bonus       = CreateBonusSO(reward: 50);
            var persistence = CreatePersistenceSO();
            SetField(applier, "_bonusObjective", bonus);
            SetField(applier, "_persistence",   persistence);

            applier.ApplyReward();

            Assert.AreEqual(1, persistence.Count,
                "Persistence must have exactly one entry after ApplyReward.");
            Assert.IsTrue(persistence.Entries[0].completed,
                "Recorded entry must have completed = true.");
            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(bonus);
            Object.DestroyImmediate(persistence);
        }

        [Test]
        public void ApplyReward_RecordsPersistenceTitle()
        {
            var applier     = CreateApplier();
            var bonus       = CreateBonusSO(reward: 50);
            var persistence = CreatePersistenceSO();
            SetField(applier, "_bonusObjective", bonus);
            SetField(applier, "_persistence",   persistence);

            applier.ApplyReward();

            string recorded = persistence.Entries[0].title;
            Assert.IsFalse(string.IsNullOrEmpty(recorded),
                "Recorded title must not be null or empty after ApplyReward.");
            Assert.AreEqual(bonus.BonusTitle, recorded,
                "Recorded title must match BonusObjective.BonusTitle.");
            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(bonus);
            Object.DestroyImmediate(persistence);
        }

        [Test]
        public void ApplyReward_FiresOnRewardApplied()
        {
            var applier = CreateApplier();
            var bonus   = CreateBonusSO(reward: 25);
            var ch      = CreateVoidEvent();
            SetField(applier, "_bonusObjective", bonus);
            SetField(applier, "_onRewardApplied", ch);

            int count = 0;
            ch.RegisterCallback(() => count++);

            applier.ApplyReward();

            Assert.AreEqual(1, count,
                "_onRewardApplied must fire exactly once per ApplyReward call.");
            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(bonus);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void OnObjectiveCompleted_Channel_TriggersApplyReward()
        {
            var applier = CreateApplier();
            var bonus   = CreateBonusSO(reward: 75);
            var wallet  = CreateWallet(startBalance: 0);
            var ch      = CreateVoidEvent();

            SetField(applier, "_bonusObjective",       bonus);
            SetField(applier, "_wallet",               wallet);
            SetField(applier, "_onObjectiveCompleted", ch);
            InvokePrivate(applier, "Awake");
            InvokePrivate(applier, "OnEnable");

            ch.Raise();

            Assert.AreEqual(75, wallet.Balance,
                "Raising _onObjectiveCompleted must trigger ApplyReward and credit the wallet.");
            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(bonus);
            Object.DestroyImmediate(wallet);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void ApplyReward_ZeroReward_SkipsWalletCall()
        {
            var applier = CreateApplier();
            var bonus   = CreateBonusSO(reward: 0);
            var wallet  = CreateWallet(startBalance: 500);
            SetField(applier, "_bonusObjective", bonus);
            SetField(applier, "_wallet",         wallet);

            applier.ApplyReward();

            Assert.AreEqual(500, wallet.Balance,
                "When BonusReward is 0 the wallet balance must remain unchanged.");
            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(bonus);
            Object.DestroyImmediate(wallet);
        }

        [Test]
        public void ApplyReward_MultipleCompletions_AccumulatesInWallet()
        {
            var applier = CreateApplier();
            var bonus   = CreateBonusSO(reward: 100);
            var wallet  = CreateWallet(startBalance: 500);
            SetField(applier, "_bonusObjective", bonus);
            SetField(applier, "_wallet",         wallet);

            applier.ApplyReward();
            applier.ApplyReward();
            applier.ApplyReward();

            Assert.AreEqual(800, wallet.Balance,
                "Three ApplyReward calls must add 300 total to the wallet (3 × 100).");
            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(bonus);
            Object.DestroyImmediate(wallet);
        }
    }
}
