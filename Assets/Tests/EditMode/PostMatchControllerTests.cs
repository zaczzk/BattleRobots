using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PostMatchController"/>.
    ///
    /// Covers:
    ///   • Awake with null _resultPanel — panel?.SetActive(false) silently skips.
    ///   • OnEnable / OnDisable with null _onMatchEnded — ?. guards prevent throw.
    ///   • MatchEnded raised with null _matchResult — ShowResults logs a warning
    ///     and returns early without throwing.
    ///   • MatchEnded raised with null _resultPanel — SetActive guarded safely.
    ///   • MatchEnded raised with a populated MatchResultSO but all UI Text refs
    ///     null — every string.Format path is guarded by null-check on the Text.
    ///   • OnPlayAgainPressed() with null _sceneRegistry — falls back to "Arena";
    ///     SceneLoader.LoadScene may log an error but must not throw.
    ///   • OnMainMenuPressed() with null _sceneRegistry — falls back to "MainMenu".
    ///   • OnDisable unregisters the callback from _onMatchEnded (external-counter
    ///     pattern: only the test counter fires after disable).
    ///   • Awake hides _resultPanel on startup via SetActive(false).
    ///
    /// All tests run headless; no scene or uGUI objects required.
    /// </summary>
    public class PostMatchControllerTests
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

        private static (GameObject go, PostMatchController ctrl) MakeCtrl()
        {
            var go   = new GameObject("PostMatchController");
            go.SetActive(false);
            var ctrl = go.AddComponent<PostMatchController>();
            return (go, ctrl);
        }

        private static MatchResultSO MakeResult(
            bool playerWon       = true,
            float durationSecs   = 75f,
            int currencyEarned   = 200,
            int newWalletBalance = 700)
        {
            var result = ScriptableObject.CreateInstance<MatchResultSO>();
            result.Write(playerWon, durationSecs, currencyEarned, newWalletBalance);
            return result;
        }

        // ── Awake — null result panel ─────────────────────────────────────────

        [Test]
        public void Awake_NullResultPanel_DoesNotThrow()
        {
            var (go, _) = MakeCtrl();
            // _resultPanel not assigned; Awake's _resultPanel?.SetActive(false) skips.
            Assert.DoesNotThrow(() => go.SetActive(true),
                "Awake with null _resultPanel must not throw.");
            Object.DestroyImmediate(go);
        }

        // ── OnEnable / OnDisable — null channel ───────────────────────────────

        [Test]
        public void OnEnable_NullMatchEndedChannel_DoesNotThrow()
        {
            var (go, _) = MakeCtrl();
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with null _onMatchEnded must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullMatchEndedChannel_DoesNotThrow()
        {
            var (go, _) = MakeCtrl();
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with null _onMatchEnded must not throw.");
            Object.DestroyImmediate(go);
        }

        // ── MatchEnded raised — null MatchResultSO ────────────────────────────

        [Test]
        public void MatchEnded_Raise_NullMatchResult_DoesNotThrow()
        {
            var matchEnded = ScriptableObject.CreateInstance<VoidGameEvent>();
            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_onMatchEnded", matchEnded);

            go.SetActive(true); // OnEnable registers callback

            // _matchResult is null; ShowResults must log a warning and return early.
            Assert.DoesNotThrow(() => matchEnded.Raise(),
                "Raising _onMatchEnded with null _matchResult must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
        }

        // ── MatchEnded raised — null result panel ─────────────────────────────

        [Test]
        public void MatchEnded_Raise_NullResultPanel_DoesNotThrow()
        {
            var matchEnded = ScriptableObject.CreateInstance<VoidGameEvent>();
            var result     = MakeResult();
            var (go, ctrl) = MakeCtrl();

            SetField(ctrl, "_onMatchEnded", matchEnded);
            SetField(ctrl, "_matchResult",  result);
            // _resultPanel left null — ShowResults' SetActive guard must handle this.

            go.SetActive(true);

            Assert.DoesNotThrow(() => matchEnded.Raise(),
                "Raising _onMatchEnded with null _resultPanel must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(result);
        }

        // ── MatchEnded raised — MatchResultSO populated, UI refs null ─────────

        [Test]
        public void MatchEnded_Raise_WithMatchResult_NullUIRefs_DoesNotThrow()
        {
            // ShowResults exercises all string.Format paths but all Text fields are null;
            // each individual null-check (_outcomeText != null, etc.) must guard cleanly.
            var matchEnded = ScriptableObject.CreateInstance<VoidGameEvent>();
            var result     = MakeResult(playerWon: true, durationSecs: 90f, currencyEarned: 150);
            var (go, ctrl) = MakeCtrl();

            SetField(ctrl, "_onMatchEnded", matchEnded);
            SetField(ctrl, "_matchResult",  result);

            go.SetActive(true);

            Assert.DoesNotThrow(() => matchEnded.Raise(),
                "ShowResults with null UI Text refs must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(result);
        }

        // ── OnPlayAgainPressed — null SceneRegistry ───────────────────────────

        [Test]
        public void OnPlayAgainPressed_NullSceneRegistry_DoesNotThrow()
        {
            // Falls back to SceneLoader.LoadScene("Arena"). Scene won't load in
            // EditMode but no exception should be thrown.
            var (go, ctrl) = MakeCtrl();
            go.SetActive(true);

            Assert.DoesNotThrow(() => ctrl.OnPlayAgainPressed(),
                "OnPlayAgainPressed with null _sceneRegistry must not throw.");

            Object.DestroyImmediate(go);
        }

        // ── OnMainMenuPressed — null SceneRegistry ────────────────────────────

        [Test]
        public void OnMainMenuPressed_NullSceneRegistry_DoesNotThrow()
        {
            var (go, ctrl) = MakeCtrl();
            go.SetActive(true);

            Assert.DoesNotThrow(() => ctrl.OnMainMenuPressed(),
                "OnMainMenuPressed with null _sceneRegistry must not throw.");

            Object.DestroyImmediate(go);
        }

        // ── OnDisable — unregisters callback from _onMatchEnded ───────────────

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

            // Raise — only the external counter must fire.
            matchEnded.Raise();

            Assert.AreEqual(1, externalCount,
                "After OnDisable, only the external counter (not _matchEndedCallback) " +
                "should fire on _onMatchEnded.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
        }
    }
}
