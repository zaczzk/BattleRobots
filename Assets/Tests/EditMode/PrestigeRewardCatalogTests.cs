using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T181:
    ///   <see cref="PrestigeRewardCatalogSO"/> and
    ///   <see cref="PrestigeRewardController"/>.
    ///
    /// PrestigeRewardCatalogSOTests (10):
    ///   FreshInstance_CountIsZero ×1
    ///   HasRewardAtRank_EmptyCatalog_ReturnsFalse ×1
    ///   HasRewardAtRank_MatchingRank_ReturnsTrue ×1
    ///   HasRewardAtRank_NoMatchingRank_ReturnsFalse ×1
    ///   TryGetRewardForRank_Found_ReturnsTrueAndEntry ×1
    ///   TryGetRewardForRank_NotFound_ReturnsFalse ×1
    ///   TryGetNextReward_EmptyCatalog_ReturnsFalse ×1
    ///   TryGetNextReward_AllRewardsReached_ReturnsFalse ×1
    ///   TryGetNextReward_PicksLowestRankAboveCurrent ×1
    ///   TryGetNextReward_AtPrestigeZero_ReturnsFirstEntry ×1
    ///
    /// PrestigeRewardControllerTests (6):
    ///   FreshInstance_CatalogIsNull ×1
    ///   FreshInstance_PrestigeSystemIsNull ×1
    ///   OnEnable_AllNullRefs_DoesNotThrow ×1
    ///   OnDisable_AllNullRefs_DoesNotThrow ×1
    ///   Refresh_NoNextReward_ShowsNoMorePanel ×1
    ///   Refresh_HasNextReward_PopulatesLabels ×1
    ///
    /// Total: 16 new EditMode tests.
    /// </summary>
    public class PrestigeRewardCatalogTests
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

        private static PrestigeRewardCatalogSO CreateCatalog(PrestigeRewardEntry[] entries)
        {
            var so = ScriptableObject.CreateInstance<PrestigeRewardCatalogSO>();
            SetField(so, "_rewards", entries);
            return so;
        }

        private static PrestigeSystemSO CreatePrestige(int count = 0)
        {
            var so = ScriptableObject.CreateInstance<PrestigeSystemSO>();
            so.LoadSnapshot(count);
            return so;
        }

        private static PrestigeRewardController CreateController()
        {
            var go = new GameObject("PrestigeRewardCtrl_Test");
            return go.AddComponent<PrestigeRewardController>();
        }

        // ══════════════════════════════════════════════════════════════════════
        // PrestigeRewardCatalogSO Tests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Catalog_FreshInstance_CountIsZero()
        {
            var so = ScriptableObject.CreateInstance<PrestigeRewardCatalogSO>();
            Assert.AreEqual(0, so.Count);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_HasRewardAtRank_EmptyCatalog_ReturnsFalse()
        {
            var so = CreateCatalog(new PrestigeRewardEntry[0]);
            Assert.IsFalse(so.HasRewardAtRank(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_HasRewardAtRank_MatchingRank_ReturnsTrue()
        {
            var so = CreateCatalog(new[]
            {
                new PrestigeRewardEntry { rank = 2, label = "Silver Skin", bonusMultiplier = 1.1f },
            });
            Assert.IsTrue(so.HasRewardAtRank(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_HasRewardAtRank_NoMatchingRank_ReturnsFalse()
        {
            var so = CreateCatalog(new[]
            {
                new PrestigeRewardEntry { rank = 2, label = "Silver Skin", bonusMultiplier = 1.1f },
            });
            Assert.IsFalse(so.HasRewardAtRank(5));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_TryGetRewardForRank_Found_ReturnsTrueAndEntry()
        {
            var entry = new PrestigeRewardEntry { rank = 3, label = "Gold Badge", bonusMultiplier = 1.25f };
            var so    = CreateCatalog(new[] { entry });

            bool found = so.TryGetRewardForRank(3, out PrestigeRewardEntry result);

            Assert.IsTrue(found);
            Assert.AreEqual(3,          result.rank);
            Assert.AreEqual("Gold Badge", result.label);
            Assert.AreEqual(1.25f,      result.bonusMultiplier, 0.001f);

            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_TryGetRewardForRank_NotFound_ReturnsFalse()
        {
            var so = CreateCatalog(new[]
            {
                new PrestigeRewardEntry { rank = 1, label = "Bronze", bonusMultiplier = 1.05f },
            });
            bool found = so.TryGetRewardForRank(9, out _);
            Assert.IsFalse(found);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_TryGetNextReward_EmptyCatalog_ReturnsFalse()
        {
            var so    = CreateCatalog(new PrestigeRewardEntry[0]);
            bool found = so.TryGetNextReward(0, out _);
            Assert.IsFalse(found);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_TryGetNextReward_AllRewardsReached_ReturnsFalse()
        {
            var so = CreateCatalog(new[]
            {
                new PrestigeRewardEntry { rank = 1, label = "A", bonusMultiplier = 1.1f },
                new PrestigeRewardEntry { rank = 3, label = "B", bonusMultiplier = 1.2f },
            });
            // Current prestige (5) is already above all entries.
            bool found = so.TryGetNextReward(5, out _);
            Assert.IsFalse(found);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_TryGetNextReward_PicksLowestRankAboveCurrent()
        {
            var so = CreateCatalog(new[]
            {
                new PrestigeRewardEntry { rank = 5, label = "High", bonusMultiplier = 1.5f },
                new PrestigeRewardEntry { rank = 2, label = "Low",  bonusMultiplier = 1.1f },
                new PrestigeRewardEntry { rank = 3, label = "Mid",  bonusMultiplier = 1.2f },
            });
            bool found = so.TryGetNextReward(1, out PrestigeRewardEntry result);
            Assert.IsTrue(found);
            Assert.AreEqual(2, result.rank, "Should pick the lowest rank above current prestige (2).");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_TryGetNextReward_AtPrestigeZero_ReturnsFirstEntry()
        {
            var so = CreateCatalog(new[]
            {
                new PrestigeRewardEntry { rank = 1, label = "Bronze", bonusMultiplier = 1.05f },
            });
            bool found = so.TryGetNextReward(0, out PrestigeRewardEntry result);
            Assert.IsTrue(found);
            Assert.AreEqual(1, result.rank);
            Object.DestroyImmediate(so);
        }

        // ══════════════════════════════════════════════════════════════════════
        // PrestigeRewardController Tests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void RewardCtrl_FreshInstance_CatalogIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Catalog);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void RewardCtrl_FreshInstance_PrestigeSystemIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.PrestigeSystem);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void RewardCtrl_OnEnable_AllNullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void RewardCtrl_OnDisable_AllNullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void RewardCtrl_Refresh_NoNextReward_ActivatesNoMorePanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("NoMoreRewards");
            panel.SetActive(false);

            // Empty catalog → no next reward.
            var catalog = CreateCatalog(new PrestigeRewardEntry[0]);
            SetField(ctrl, "_catalog",            catalog);
            SetField(ctrl, "_noMoreRewardsPanel", panel);

            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "_noMoreRewardsPanel must be activated when catalog has no rewards.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void RewardCtrl_Refresh_HasNextReward_PopulatesLabels()
        {
            var ctrl = CreateController();

            var catalog  = CreateCatalog(new[]
            {
                new PrestigeRewardEntry { rank = 2, label = "Silver Skin", bonusMultiplier = 1.25f },
            });
            var prestige = CreatePrestige(0); // at rank 0, next is rank 2

            var rewardLabelGO     = new GameObject("RewardLabel");
            var multiplierLabelGO = new GameObject("MultiplierLabel");
            var rankLabelGO       = new GameObject("RankLabel");

            var rewardText     = rewardLabelGO.AddComponent<UnityEngine.UI.Text>();
            var multiplierText = multiplierLabelGO.AddComponent<UnityEngine.UI.Text>();
            var rankText       = rankLabelGO.AddComponent<UnityEngine.UI.Text>();

            SetField(ctrl, "_catalog",          catalog);
            SetField(ctrl, "_prestigeSystem",   prestige);
            SetField(ctrl, "_rewardLabel",      rewardText);
            SetField(ctrl, "_multiplierLabel",  multiplierText);
            SetField(ctrl, "_rankLabel",        rankText);

            ctrl.Refresh();

            Assert.AreEqual("Silver Skin", rewardText.text,
                "_rewardLabel must show the next reward label.");
            Assert.AreEqual("x1.25", multiplierText.text,
                "_multiplierLabel must show formatted multiplier.");
            Assert.AreEqual("At Prestige 2", rankText.text,
                "_rankLabel must show 'At Prestige N'.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(rewardLabelGO);
            Object.DestroyImmediate(multiplierLabelGO);
            Object.DestroyImmediate(rankLabelGO);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(prestige);
        }
    }
}
