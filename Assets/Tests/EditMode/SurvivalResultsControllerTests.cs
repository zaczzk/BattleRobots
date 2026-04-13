using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="SurvivalResultsController"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all-null refs — no throw.
    ///   • OnEnable / OnDisable with null event channels — no throw.
    ///   • OnEnable hides the results panel immediately.
    ///   • ShowResults with null manager — shows panel (no throw).
    ///   • ShowResults with active manager — sets _wavesCompletedText.
    ///   • ShowResults with active manager — sets _botsDefeatedText.
    ///   • ShowResults with wallet — credits earned equals balance delta since snapshot.
    ///   • ShowResults with null wallet — no throw.
    ///   • ShowResults new best wave — _newBestBadge shown.
    ///   • ShowResults not new best wave — _newBestBadge hidden.
    ///   • ShowResults with null _resultsPanel — no throw.
    ///   • _onSurvivalEnded raised → ShowResults shows the panel.
    ///   • _onWaveStarted wave 1 → snapshots _bestWaveAtStart (used for badge logic).
    ///   • OnDisable unregisters both event channels.
    ///
    /// <c>SurvivalResultsController</c> is a <c>MonoBehaviour</c>; a headless
    /// <c>GameObject</c> is created for each test. Private serialised fields are
    /// injected via reflection — the same pattern used throughout this test suite.
    /// </summary>
    public class SurvivalResultsControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static SurvivalResultsController MakeController(out GameObject go)
        {
            go = new GameObject("SurvivalResultsControllerTest");
            go.SetActive(false); // prevent OnEnable before wiring
            return go.AddComponent<SurvivalResultsController>();
        }

        /// <summary>
        /// Creates a <see cref="WaveManagerSO"/> with an active survival run on wave 1
        /// and returns the config used so the caller can clean it up.
        /// </summary>
        private static WaveManagerSO MakeActiveManager(out WaveConfigSO config)
        {
            config = ScriptableObject.CreateInstance<WaveConfigSO>();
            var mgr = ScriptableObject.CreateInstance<WaveManagerSO>();
            mgr.StartSurvival(config);
            return mgr;
        }

        // ── OnEnable / OnDisable — null guards ────────────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnable_NullChannels_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<SurvivalResultsController>();
            var mgr  = ScriptableObject.CreateInstance<WaveManagerSO>();
            SetField(ctrl, "_waveManager", mgr);
            // event channels remain null

            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with null event channels must not throw.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(mgr);
        }

        [Test]
        public void OnDisable_NullChannels_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<SurvivalResultsController>();
            var mgr  = ScriptableObject.CreateInstance<WaveManagerSO>();
            SetField(ctrl, "_waveManager", mgr);
            go.SetActive(true);

            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with null event channels must not throw.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(mgr);
        }

        // ── OnEnable hides panel ──────────────────────────────────────────────

        [Test]
        public void OnEnable_HidesResultsPanel()
        {
            MakeController(out GameObject go);
            var ctrl  = go.GetComponent<SurvivalResultsController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true); // start visible
            SetField(ctrl, "_resultsPanel", panel);

            go.SetActive(true); // OnEnable → Hide()

            Assert.IsFalse(panel.activeSelf,
                "Results panel must be hidden immediately on OnEnable.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        // ── ShowResults — label population ────────────────────────────────────

        [Test]
        public void ShowResults_NullManager_ShowsPanel()
        {
            MakeController(out GameObject go);
            var ctrl  = go.GetComponent<SurvivalResultsController>();
            var panel = new GameObject("Panel");
            panel.SetActive(false);
            SetField(ctrl, "_resultsPanel", panel);
            go.SetActive(true); // Hide() is called

            ctrl.ShowResults();

            Assert.IsTrue(panel.activeSelf,
                "ShowResults must show the results panel even when _waveManager is null.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void ShowResults_WithManager_SetsWavesCompletedLabel()
        {
            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<SurvivalResultsController>();
            var labelGo = new GameObject("Label");
            var label  = labelGo.AddComponent<Text>();
            var mgr    = MakeActiveManager(out WaveConfigSO config); // CurrentWave = 1

            SetField(ctrl, "_waveManager",       mgr);
            SetField(ctrl, "_wavesCompletedText", label);
            go.SetActive(true);

            ctrl.ShowResults();

            Assert.AreEqual("Wave 1", label.text,
                "_wavesCompletedText must show 'Wave N' matching WaveManagerSO.CurrentWave.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(labelGo);
            Object.DestroyImmediate(mgr);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void ShowResults_WithManager_SetsBotsDefeatedLabel()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<SurvivalResultsController>();
            var labelGo = new GameObject("Label");
            var label   = labelGo.AddComponent<Text>();
            var mgr     = MakeActiveManager(out WaveConfigSO config);
            mgr.RecordBotDefeated(); // TotalBotsDefeated = 1

            SetField(ctrl, "_waveManager",    mgr);
            SetField(ctrl, "_botsDefeatedText", label);
            go.SetActive(true);

            ctrl.ShowResults();

            Assert.AreEqual("1 bots defeated", label.text,
                "_botsDefeatedText must show 'N bots defeated' matching TotalBotsDefeated.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(labelGo);
            Object.DestroyImmediate(mgr);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void ShowResults_WithWallet_SetsCreditsEarned()
        {
            // Wire the wave-started channel to both the manager and the controller
            // so the snapshot fires when StartSurvival is called.
            var waveStartedCh = ScriptableObject.CreateInstance<VoidGameEvent>();

            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<SurvivalResultsController>();
            var labelGo = new GameObject("Label");
            var label   = labelGo.AddComponent<Text>();

            var wallet = ScriptableObject.CreateInstance<PlayerWallet>();
            wallet.Reset(); // Balance = 500 (default _startingBalance)

            var config = ScriptableObject.CreateInstance<WaveConfigSO>();
            var mgr    = ScriptableObject.CreateInstance<WaveManagerSO>();
            SetField(mgr, "_onWaveStarted", waveStartedCh); // mgr fires the channel on StartSurvival

            SetField(ctrl, "_waveManager",      mgr);
            SetField(ctrl, "_playerWallet",     wallet);
            SetField(ctrl, "_creditsEarnedText", label);
            SetField(ctrl, "_onWaveStarted",    waveStartedCh); // ctrl subscribes to the same channel
            go.SetActive(true); // OnEnable

            mgr.StartSurvival(config); // CurrentWave = 1 → fires waveStartedCh → snapshot at 500

            wallet.AddFunds(100); // simulates per-wave credit reward → Balance = 600

            ctrl.ShowResults();

            Assert.AreEqual("+100 credits", label.text,
                "_creditsEarnedText must show credits earned since wave 1 started.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(labelGo);
            Object.DestroyImmediate(wallet);
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(mgr);
            Object.DestroyImmediate(waveStartedCh);
        }

        [Test]
        public void ShowResults_NullWallet_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<SurvivalResultsController>();
            var labelGo = new GameObject("Label");
            var label   = labelGo.AddComponent<Text>();
            SetField(ctrl, "_creditsEarnedText", label);
            // _playerWallet remains null
            go.SetActive(true);

            Assert.DoesNotThrow(() => ctrl.ShowResults(),
                "ShowResults with null _playerWallet must not throw.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(labelGo);
        }

        // ── ShowResults — new best badge ──────────────────────────────────────

        [Test]
        public void ShowResults_NewBest_ShowsBadge()
        {
            var waveStartedCh = ScriptableObject.CreateInstance<VoidGameEvent>();

            MakeController(out GameObject go);
            var ctrl  = go.GetComponent<SurvivalResultsController>();
            var badge = new GameObject("Badge");
            badge.SetActive(false);

            var config = ScriptableObject.CreateInstance<WaveConfigSO>();
            var mgr    = ScriptableObject.CreateInstance<WaveManagerSO>();
            // BestWave starts at 0 (never played before)
            SetField(mgr, "_onWaveStarted", waveStartedCh);

            SetField(ctrl, "_waveManager",   mgr);
            SetField(ctrl, "_newBestBadge",  badge);
            SetField(ctrl, "_onWaveStarted", waveStartedCh);
            go.SetActive(true);

            mgr.StartSurvival(config); // wave 1 → snapshot _bestWaveAtStart = 0
            // Advance to wave 3 without defeating bots (tests internal state directly)
            mgr.StartNextWave(config); // wave 2 (fires channel, CurrentWave ≠ 1, no re-snapshot)
            mgr.StartNextWave(config); // wave 3

            ctrl.ShowResults(); // CurrentWave(3) > _bestWaveAtStart(0) → badge shown

            Assert.IsTrue(badge.activeSelf,
                "_newBestBadge must be shown when the player surpasses their previous best wave.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(badge);
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(mgr);
            Object.DestroyImmediate(waveStartedCh);
        }

        [Test]
        public void ShowResults_NotNewBest_HidesBadge()
        {
            var waveStartedCh = ScriptableObject.CreateInstance<VoidGameEvent>();

            MakeController(out GameObject go);
            var ctrl  = go.GetComponent<SurvivalResultsController>();
            var badge = new GameObject("Badge");
            badge.SetActive(true); // start visible

            var config = ScriptableObject.CreateInstance<WaveConfigSO>();
            var mgr    = ScriptableObject.CreateInstance<WaveManagerSO>();
            mgr.LoadSnapshot(5); // BestWave = 5 from a previous session
            SetField(mgr, "_onWaveStarted", waveStartedCh);

            SetField(ctrl, "_waveManager",   mgr);
            SetField(ctrl, "_newBestBadge",  badge);
            SetField(ctrl, "_onWaveStarted", waveStartedCh);
            go.SetActive(true);

            mgr.StartSurvival(config); // wave 1 → snapshot _bestWaveAtStart = 5
            // Player dies on wave 1: CurrentWave = 1, _bestWaveAtStart = 5

            ctrl.ShowResults(); // 1 > 5 is false → badge hidden

            Assert.IsFalse(badge.activeSelf,
                "_newBestBadge must be hidden when the player did not reach a new best wave.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(badge);
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(mgr);
            Object.DestroyImmediate(waveStartedCh);
        }

        [Test]
        public void ShowResults_NullResultsPanel_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<SurvivalResultsController>();
            // _resultsPanel remains null
            go.SetActive(true);

            Assert.DoesNotThrow(() => ctrl.ShowResults(),
                "ShowResults with null _resultsPanel must not throw.");
            Object.DestroyImmediate(go);
        }

        // ── Event-driven paths ────────────────────────────────────────────────

        [Test]
        public void OnSurvivalEnded_Raised_ShowsPanel()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();

            MakeController(out GameObject go);
            var ctrl  = go.GetComponent<SurvivalResultsController>();
            var panel = new GameObject("Panel");
            panel.SetActive(false);

            SetField(ctrl, "_resultsPanel",   panel);
            SetField(ctrl, "_onSurvivalEnded", channel);
            go.SetActive(true); // OnEnable → Hide()

            channel.Raise(); // → ShowResults → panel shown

            Assert.IsTrue(panel.activeSelf,
                "Results panel must be shown when _onSurvivalEnded is raised.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void OnWaveStarted_Wave1_SnapshotsBestWave_ForBadgeLogic()
        {
            // Verify that the wave-1 snapshot captures BestWave correctly and
            // that the badge reflects the snapshotted value rather than 0.
            var waveStartedCh = ScriptableObject.CreateInstance<VoidGameEvent>();

            MakeController(out GameObject go);
            var ctrl  = go.GetComponent<SurvivalResultsController>();
            var badge = new GameObject("Badge");
            badge.SetActive(true);

            var config = ScriptableObject.CreateInstance<WaveConfigSO>();
            var mgr    = ScriptableObject.CreateInstance<WaveManagerSO>();
            mgr.LoadSnapshot(10); // BestWave = 10 from a previous session
            SetField(mgr, "_onWaveStarted", waveStartedCh);

            SetField(ctrl, "_waveManager",   mgr);
            SetField(ctrl, "_newBestBadge",  badge);
            SetField(ctrl, "_onWaveStarted", waveStartedCh);
            go.SetActive(true);

            mgr.StartSurvival(config); // wave 1 fires → snapshot _bestWaveAtStart = 10
            // CurrentWave = 1; player dies immediately.

            ctrl.ShowResults(); // CurrentWave(1) > _bestWaveAtStart(10) → false → badge hidden

            Assert.IsFalse(badge.activeSelf,
                "Badge must be hidden: BestWave at run start (10) exceeds current wave (1).");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(badge);
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(mgr);
            Object.DestroyImmediate(waveStartedCh);
        }

        // ── OnDisable unregisters ─────────────────────────────────────────────

        [Test]
        public void OnDisable_UnregistersChannels()
        {
            var survivalEnded = ScriptableObject.CreateInstance<VoidGameEvent>();
            var waveStarted   = ScriptableObject.CreateInstance<VoidGameEvent>();

            // External counters — these must be the only callbacks that fire
            // after the controller is disabled.
            int survivalFires = 0;
            int waveFires     = 0;
            survivalEnded.RegisterCallback(() => survivalFires++);
            waveStarted.RegisterCallback(()   => waveFires++);

            MakeController(out GameObject go);
            var ctrl  = go.GetComponent<SurvivalResultsController>();
            var panel = new GameObject("Panel");
            SetField(ctrl, "_resultsPanel",   panel);
            SetField(ctrl, "_onSurvivalEnded", survivalEnded);
            SetField(ctrl, "_onWaveStarted",   waveStarted);

            go.SetActive(true);  // OnEnable → subscribe
            go.SetActive(false); // OnDisable → unsubscribe

            panel.SetActive(false); // reset to known state
            survivalEnded.Raise(); // controller must NOT react
            waveStarted.Raise();   // controller must NOT react

            Assert.IsFalse(panel.activeSelf,
                "Results panel must not be shown after OnDisable when _onSurvivalEnded fires.");
            Assert.AreEqual(1, survivalFires,
                "Only the external counter must fire for _onSurvivalEnded after disable.");
            Assert.AreEqual(1, waveFires,
                "Only the external counter must fire for _onWaveStarted after disable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(survivalEnded);
            Object.DestroyImmediate(waveStarted);
        }
    }
}
