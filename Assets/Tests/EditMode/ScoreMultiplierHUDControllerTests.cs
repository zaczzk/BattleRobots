using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T191 — <see cref="ScoreMultiplierHUDController"/>.
    ///
    /// ScoreMultiplierHUDControllerTests (14):
    ///   FreshInstance_ScoreMultiplierIsNull                    ×1
    ///   FreshInstance_RewardCatalogIsNull                      ×1
    ///   FreshInstance_PrestigeSystemIsNull                     ×1
    ///   OnEnable_NullRefs_DoesNotThrow                         ×1
    ///   OnDisable_NullRefs_DoesNotThrow                        ×1
    ///   OnDisable_Unregisters_PrestigeChannel                  ×1
    ///   Refresh_NullScoreMultiplier_ShowsOneX                  ×1
    ///   Refresh_WithScoreMultiplier_ShowsMultiplier            ×1
    ///   Refresh_NullCatalog_RewardLabelShowsDash               ×1
    ///   Refresh_NullPrestigeSystem_RewardLabelShowsDash        ×1
    ///   Refresh_WithCatalogAndPrestige_ShowsRewardLabel        ×1
    ///   Refresh_NoRewardAtCurrentPrestige_ShowsDash            ×1
    ///   Refresh_NullLabels_DoesNotThrow                        ×1
    ///   OnEnable_CallsRefresh                                  ×1
    /// </summary>
    public class ScoreMultiplierHUDControllerTests
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

        private static ScoreMultiplierHUDController CreateCtrl()
        {
            var go = new GameObject("ScoreMultHUD_Test");
            return go.AddComponent<ScoreMultiplierHUDController>();
        }

        private static ScoreMultiplierSO CreateMult(float value)
        {
            var m = ScriptableObject.CreateInstance<ScoreMultiplierSO>();
            m.SetMultiplier(value);
            return m;
        }

        private static PrestigeSystemSO CreatePrestige(int count = 0)
        {
            var so = ScriptableObject.CreateInstance<PrestigeSystemSO>();
            so.LoadSnapshot(count);
            return so;
        }

        private static PrestigeRewardCatalogSO CreateCatalog(
            int rank = 1, string label = "Test Reward", float mult = 1.25f)
        {
            var so = ScriptableObject.CreateInstance<PrestigeRewardCatalogSO>();
            // Inject a single entry via reflection.
            var entry = new PrestigeRewardEntry { rank = rank, label = label, bonusMultiplier = mult };
            FieldInfo fi = typeof(PrestigeRewardCatalogSO)
                .GetField("_rewards", BindingFlags.Instance | BindingFlags.NonPublic);
            fi?.SetValue(so, new PrestigeRewardEntry[] { entry });
            return so;
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        // ── FreshInstance ─────────────────────────────────────────────────────

        [Test]
        public void Ctrl_FreshInstance_ScoreMultiplierIsNull()
        {
            var ctrl = CreateCtrl();
            Assert.IsNull(ctrl.ScoreMultiplier);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_FreshInstance_RewardCatalogIsNull()
        {
            var ctrl = CreateCtrl();
            Assert.IsNull(ctrl.RewardCatalog);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_FreshInstance_PrestigeSystemIsNull()
        {
            var ctrl = CreateCtrl();
            Assert.IsNull(ctrl.PrestigeSystem);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        // ── Lifecycle null safety ─────────────────────────────────────────────

        [Test]
        public void Ctrl_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateCtrl();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateCtrl();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_Unregisters_PrestigeChannel()
        {
            var ctrl = CreateCtrl();
            var ch   = CreateEvent();
            SetField(ctrl, "_onPrestige", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int callCount = 0;
            ch.RegisterCallback(() => callCount++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, callCount,
                "OnDisable must unregister from _onPrestige (only external counter must fire).");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        // ── Refresh — multiplier label ────────────────────────────────────────

        [Test]
        public void Ctrl_Refresh_NullScoreMultiplier_ShowsOneX()
        {
            var ctrl    = CreateCtrl();
            var labelGO = new GameObject("MultLabel");
            var label   = labelGO.AddComponent<Text>();
            label.text  = "x9.99";
            SetField(ctrl, "_multiplierLabel", label);

            ctrl.Refresh();

            Assert.AreEqual("x1.00", label.text,
                "Refresh with null ScoreMultiplierSO must display 'x1.00'.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
        }

        [Test]
        public void Ctrl_Refresh_WithScoreMultiplier_ShowsMultiplier()
        {
            var ctrl    = CreateCtrl();
            var mult    = CreateMult(2.5f);
            var labelGO = new GameObject("MultLabel");
            var label   = labelGO.AddComponent<Text>();
            SetField(ctrl, "_scoreMultiplier",  mult);
            SetField(ctrl, "_multiplierLabel",  label);

            ctrl.Refresh();

            Assert.AreEqual("x2.50", label.text,
                "Refresh must display the ScoreMultiplierSO.Multiplier formatted as 'xN.NN'.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(mult);
        }

        // ── Refresh — reward label ────────────────────────────────────────────

        [Test]
        public void Ctrl_Refresh_NullCatalog_RewardLabelShowsDash()
        {
            var ctrl    = CreateCtrl();
            var prestige = CreatePrestige(1);
            var labelGO = new GameObject("RewardLabel");
            var label   = labelGO.AddComponent<Text>();
            label.text  = "something";
            SetField(ctrl, "_prestigeSystem", prestige);
            SetField(ctrl, "_rewardLabel",    label);
            // _rewardCatalog stays null

            ctrl.Refresh();

            Assert.AreEqual("—", label.text,
                "Null _rewardCatalog must show '—' in the reward label.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(prestige);
        }

        [Test]
        public void Ctrl_Refresh_NullPrestigeSystem_RewardLabelShowsDash()
        {
            var ctrl    = CreateCtrl();
            var catalog = CreateCatalog(rank: 1, label: "Bronze Skin");
            var labelGO = new GameObject("RewardLabel");
            var label   = labelGO.AddComponent<Text>();
            label.text  = "something";
            SetField(ctrl, "_rewardCatalog", catalog);
            SetField(ctrl, "_rewardLabel",   label);
            // _prestigeSystem stays null

            ctrl.Refresh();

            Assert.AreEqual("—", label.text,
                "Null _prestigeSystem must show '—' in the reward label.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Ctrl_Refresh_WithCatalogAndPrestige_ShowsRewardLabel()
        {
            // Prestige count = 1; catalog has entry at rank 1 with label "Bronze Skin".
            var ctrl     = CreateCtrl();
            var prestige = CreatePrestige(1);
            var catalog  = CreateCatalog(rank: 1, label: "Bronze Skin");
            var labelGO  = new GameObject("RewardLabel");
            var label    = labelGO.AddComponent<Text>();
            SetField(ctrl, "_prestigeSystem", prestige);
            SetField(ctrl, "_rewardCatalog",  catalog);
            SetField(ctrl, "_rewardLabel",    label);

            ctrl.Refresh();

            Assert.AreEqual("Bronze Skin", label.text,
                "Refresh must display the reward label for the current prestige rank.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(prestige);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Ctrl_Refresh_NoRewardAtCurrentPrestige_ShowsDash()
        {
            // Prestige count = 3; catalog only has rank 1 → no exact match → "—".
            var ctrl     = CreateCtrl();
            var prestige = CreatePrestige(3);
            var catalog  = CreateCatalog(rank: 1, label: "Bronze Skin");
            var labelGO  = new GameObject("RewardLabel");
            var label    = labelGO.AddComponent<Text>();
            label.text   = "something";
            SetField(ctrl, "_prestigeSystem", prestige);
            SetField(ctrl, "_rewardCatalog",  catalog);
            SetField(ctrl, "_rewardLabel",    label);

            ctrl.Refresh();

            Assert.AreEqual("—", label.text,
                "No reward at the current prestige rank must show '—' in the reward label.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(prestige);
            Object.DestroyImmediate(catalog);
        }

        // ── Null UI ──────────────────────────────────────────────────────────

        [Test]
        public void Ctrl_Refresh_NullLabels_DoesNotThrow()
        {
            var ctrl     = CreateCtrl();
            var prestige = CreatePrestige(1);
            var catalog  = CreateCatalog(rank: 1, label: "Test");
            var mult     = CreateMult(2f);
            SetField(ctrl, "_scoreMultiplier", mult);
            SetField(ctrl, "_prestigeSystem",  prestige);
            SetField(ctrl, "_rewardCatalog",   catalog);
            // No UI labels assigned

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh with null UI labels must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(prestige);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(mult);
        }

        // ── Event-driven refresh ──────────────────────────────────────────────

        [Test]
        public void Ctrl_OnEnable_CallsRefresh()
        {
            var ctrl    = CreateCtrl();
            var mult    = CreateMult(3f);
            var labelGO = new GameObject("MultLabel");
            var label   = labelGO.AddComponent<Text>();
            SetField(ctrl, "_scoreMultiplier", mult);
            SetField(ctrl, "_multiplierLabel", label);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            Assert.AreEqual("x3.00", label.text,
                "OnEnable must call Refresh() immediately — label must be populated.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(mult);
        }
    }
}
