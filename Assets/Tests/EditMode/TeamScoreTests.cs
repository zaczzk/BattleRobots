using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T225:
    ///   <see cref="TeamScoreSO"/> and <see cref="TeamScoreHUDController"/>.
    ///
    /// TeamScoreSOTests (8):
    ///   FreshInstance_TeamAScore_IsZero                    ×1
    ///   FreshInstance_TeamBScore_IsZero                    ×1
    ///   AddTeamAScore_NegativeDelta_NoChange               ×1
    ///   AddTeamAScore_PositiveDelta_IncreasesScore         ×1
    ///   AddTeamBScore_PositiveDelta_IncreasesScore         ×1
    ///   AddTeamAScore_FiresOnScoreChanged                  ×1
    ///   ResetScores_ClearsScores                           ×1
    ///   LeadingTeam_AllThreeCases                          ×1
    ///
    /// TeamScoreHUDControllerTests (6):
    ///   FreshInstance_TeamScoreNull                        ×1
    ///   OnEnable_NullRefs_DoesNotThrow                     ×1
    ///   OnDisable_NullRefs_DoesNotThrow                    ×1
    ///   OnDisable_Unregisters                              ×1
    ///   Refresh_NullScore_HidesPanel                       ×1
    ///   Refresh_WithScore_SetsLabels                       ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class TeamScoreTests
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

        private static TeamScoreSO CreateTeamScoreSO()
        {
            var so = ScriptableObject.CreateInstance<TeamScoreSO>();
            InvokePrivate(so, "OnEnable");
            return so;
        }

        private static VoidGameEvent CreateVoidEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static TeamScoreHUDController CreateController() =>
            new GameObject("TeamScoreHUDCtrl_Test").AddComponent<TeamScoreHUDController>();

        private static Text AddText(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            return child.AddComponent<Text>();
        }

        // ── TeamScoreSOTests ──────────────────────────────────────────────────

        [Test]
        public void FreshInstance_TeamAScore_IsZero()
        {
            var so = CreateTeamScoreSO();
            Assert.AreEqual(0, so.TeamAScore);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_TeamBScore_IsZero()
        {
            var so = CreateTeamScoreSO();
            Assert.AreEqual(0, so.TeamBScore);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AddTeamAScore_NegativeDelta_NoChange()
        {
            var so = CreateTeamScoreSO();
            so.AddTeamAScore(-5);
            Assert.AreEqual(0, so.TeamAScore,
                "Negative delta should not change TeamAScore.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AddTeamAScore_PositiveDelta_IncreasesScore()
        {
            var so = CreateTeamScoreSO();
            so.AddTeamAScore(10);
            Assert.AreEqual(10, so.TeamAScore);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AddTeamBScore_PositiveDelta_IncreasesScore()
        {
            var so = CreateTeamScoreSO();
            so.AddTeamBScore(7);
            Assert.AreEqual(7, so.TeamBScore);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AddTeamAScore_FiresOnScoreChanged()
        {
            var so  = CreateTeamScoreSO();
            var evt = CreateVoidEvent();
            SetField(so, "_onScoreChanged", evt);

            int count = 0;
            evt.RegisterCallback(() => count++);
            so.AddTeamAScore(5);

            Assert.AreEqual(1, count,
                "_onScoreChanged should fire once when AddTeamAScore succeeds.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void ResetScores_ClearsScores()
        {
            var so = CreateTeamScoreSO();
            so.AddTeamAScore(10);
            so.AddTeamBScore(6);
            so.ResetScores();

            Assert.AreEqual(0, so.TeamAScore, "ResetScores should zero TeamAScore.");
            Assert.AreEqual(0, so.TeamBScore, "ResetScores should zero TeamBScore.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void LeadingTeam_AllThreeCases()
        {
            var so = CreateTeamScoreSO();

            // Tie initially
            Assert.AreEqual("Tie", so.LeadingTeam);

            // Team A leads
            so.AddTeamAScore(5);
            Assert.AreEqual("A", so.LeadingTeam);

            // Team B overtakes
            so.AddTeamBScore(10);
            Assert.AreEqual("B", so.LeadingTeam);

            Object.DestroyImmediate(so);
        }

        // ── TeamScoreHUDControllerTests ───────────────────────────────────────

        [Test]
        public void FreshInstance_TeamScoreNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.TeamScore);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateVoidEvent();
            SetField(ctrl, "_onScoreChanged", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, count,
                "After OnDisable only the manually registered callback should fire.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Refresh_NullScore_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(true);
            SetField(ctrl, "_panel", panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh(); // _teamScore is null

            Assert.IsFalse(panel.activeSelf,
                "Panel should be hidden when no TeamScoreSO is assigned.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_WithScore_SetsLabels()
        {
            var ctrl       = CreateController();
            var teamALabel = AddText(ctrl.gameObject, "teamALabel");
            var teamBLabel = AddText(ctrl.gameObject, "teamBLabel");
            var leadLabel  = AddText(ctrl.gameObject, "leadLabel");
            var so         = CreateTeamScoreSO();
            so.AddTeamAScore(3);
            so.AddTeamBScore(1);

            SetField(ctrl, "_teamScore",  so);
            SetField(ctrl, "_teamALabel", teamALabel);
            SetField(ctrl, "_teamBLabel", teamBLabel);
            SetField(ctrl, "_leadLabel",  leadLabel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            StringAssert.Contains("3", teamALabel.text,
                "Team A label should contain the current Team A score.");
            StringAssert.Contains("1", teamBLabel.text,
                "Team B label should contain the current Team B score.");
            Assert.AreEqual("Team A Leads", leadLabel.text,
                "Lead label should show 'Team A Leads' when A has a higher score.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }
    }
}
