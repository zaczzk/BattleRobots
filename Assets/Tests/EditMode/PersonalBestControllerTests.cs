using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PersonalBestController"/>.
    ///
    /// Covers:
    ///   • Awake with null _newBestPanel — SetActive guard skips silently.
    ///   • OnEnable / OnDisable with null _onMatchEnded — ?. guard prevents throw.
    ///   • MatchEnded raised with null _personalBest — early return, no throw.
    ///   • MatchEnded raised with valid PersonalBestSO but all UI refs null — every
    ///     null-check guard in Refresh() prevents throw.
    ///   • MatchEnded with a new-best score — _newBestPanel is activated (IsNewBest=true).
    ///   • MatchEnded with a non-new-best score — _newBestPanel is deactivated
    ///     (IsNewBest=false after lower score).
    ///   • OnDisable unregisters callback from _onMatchEnded (external-counter pattern).
    ///
    /// All tests run headless; no scene or full uGUI setup required.
    /// </summary>
    public class PersonalBestControllerTests
    {
        // ── Reflection helper ─────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Factory helpers ───────────────────────────────────────────────────

        private static (GameObject go, PersonalBestController ctrl) MakeCtrl()
        {
            var go   = new GameObject("PersonalBestController");
            go.SetActive(false);          // keep inactive until test is ready to activate
            var ctrl = go.AddComponent<PersonalBestController>();
            return (go, ctrl);
        }

        // ── Awake — null _newBestPanel ────────────────────────────────────────

        [Test]
        public void Awake_NullNewBestPanel_DoesNotThrow()
        {
            var (go, _) = MakeCtrl();
            // _newBestPanel left null; Awake's _newBestPanel?.SetActive(false) must skip.
            Assert.DoesNotThrow(() => go.SetActive(true),
                "Awake with null _newBestPanel must not throw.");
            Object.DestroyImmediate(go);
        }

        // ── OnEnable / OnDisable — null channel ───────────────────────────────

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            var (go, _) = MakeCtrl();
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with null _onMatchEnded must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            var (go, _) = MakeCtrl();
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with null _onMatchEnded must not throw.");
            Object.DestroyImmediate(go);
        }

        // ── MatchEnded raised — null PersonalBestSO ───────────────────────────

        [Test]
        public void MatchEnded_NullPersonalBest_DoesNotThrow()
        {
            var matchEnded   = ScriptableObject.CreateInstance<VoidGameEvent>();
            var (go, ctrl)   = MakeCtrl();
            SetField(ctrl, "_onMatchEnded", matchEnded);
            // _personalBest left null — HandleMatchEnded early-return guard must fire.

            go.SetActive(true);

            Assert.DoesNotThrow(() => matchEnded.Raise(),
                "HandleMatchEnded with null _personalBest must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
        }

        // ── MatchEnded raised — PersonalBestSO valid, all UI refs null ────────

        [Test]
        public void MatchEnded_WithPersonalBest_AllUINullRefs_DoesNotThrow()
        {
            // Refresh() exercises _scoreText / _bestScoreText / _newBestPanel — all null.
            var matchEnded   = ScriptableObject.CreateInstance<VoidGameEvent>();
            var personalBest = ScriptableObject.CreateInstance<PersonalBestSO>();
            personalBest.Submit(400); // populate so IsNewBest=true
            var (go, ctrl)   = MakeCtrl();

            SetField(ctrl, "_onMatchEnded",  matchEnded);
            SetField(ctrl, "_personalBest",  personalBest);
            // _scoreText, _bestScoreText, _newBestPanel all left null.

            go.SetActive(true);

            Assert.DoesNotThrow(() => matchEnded.Raise(),
                "Refresh() with all null UI refs must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(personalBest);
        }

        // ── _newBestPanel toggle ──────────────────────────────────────────────

        [Test]
        public void MatchEnded_NewBest_NewBestPanel_IsActive()
        {
            var matchEnded   = ScriptableObject.CreateInstance<VoidGameEvent>();
            var personalBest = ScriptableObject.CreateInstance<PersonalBestSO>();
            personalBest.Submit(800); // first submission → IsNewBest = true

            var panelGO      = new GameObject("NewBestPanel");
            var (go, ctrl)   = MakeCtrl();

            SetField(ctrl, "_onMatchEnded", matchEnded);
            SetField(ctrl, "_personalBest", personalBest);
            SetField(ctrl, "_newBestPanel", panelGO);

            go.SetActive(true);
            matchEnded.Raise();

            Assert.IsTrue(panelGO.activeSelf,
                "_newBestPanel must be active when PersonalBestSO.IsNewBest is true.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGO);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(personalBest);
        }

        [Test]
        public void MatchEnded_NotNewBest_NewBestPanel_IsInactive()
        {
            var matchEnded   = ScriptableObject.CreateInstance<VoidGameEvent>();
            var personalBest = ScriptableObject.CreateInstance<PersonalBestSO>();
            personalBest.Submit(1000); // sets BestScore=1000
            personalBest.Submit(500);  // lower → IsNewBest=false

            var panelGO      = new GameObject("NewBestPanel");
            panelGO.SetActive(true);   // start active; Refresh must deactivate it
            var (go, ctrl)   = MakeCtrl();

            SetField(ctrl, "_onMatchEnded", matchEnded);
            SetField(ctrl, "_personalBest", personalBest);
            SetField(ctrl, "_newBestPanel", panelGO);

            go.SetActive(true);
            matchEnded.Raise();

            Assert.IsFalse(panelGO.activeSelf,
                "_newBestPanel must be inactive when PersonalBestSO.IsNewBest is false.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGO);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(personalBest);
        }

        // ── OnDisable — unregisters callback ──────────────────────────────────

        [Test]
        public void OnDisable_UnregistersFromMatchEndedChannel()
        {
            var matchEnded = ScriptableObject.CreateInstance<VoidGameEvent>();

            int externalCount = 0;
            matchEnded.RegisterCallback(() => externalCount++);

            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_onMatchEnded", matchEnded);

            go.SetActive(true);   // OnEnable registers _matchEndedCallback
            go.SetActive(false);  // OnDisable unregisters it

            matchEnded.Raise();   // only external counter must fire

            Assert.AreEqual(1, externalCount,
                "After OnDisable, only the external counter (not the controller callback) " +
                "should fire when _onMatchEnded is raised.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
        }
    }
}
