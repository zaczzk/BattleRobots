using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="SurvivalMatchManager"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all-null refs — no throw.
    ///   • OnEnable / OnDisable with null event channels — no throw.
    ///   • StartSurvival null waveManager / null waveConfig — no-op.
    ///   • StartSurvival with both assigned sets WaveManagerSO.IsActive.
    ///   • HandleWaveCompleted null waveManager / null waveConfig — no-op.
    ///   • HandleWaveCompleted awards credits via GetRewardForWave(currentWave).
    ///   • HandleWaveCompleted advances wave (CurrentWave increments).
    ///   • HandleWaveCompleted persists BestWave to SaveData.survivalBestWave.
    ///   • HandlePlayerDeath null waveManager — no-op.
    ///   • HandlePlayerDeath when waveManager not active — no-op.
    ///   • HandlePlayerDeath ends active survival (IsActive becomes false).
    ///   • HandlePlayerDeath persists BestWave to SaveData.survivalBestWave.
    ///   • _onWaveCompleted raised externally triggers HandleWaveCompleted.
    ///   • _onPlayerDeath raised externally triggers HandlePlayerDeath.
    ///   • OnDisable unregisters both event channels.
    ///
    /// <c>SurvivalMatchManager</c> is a <c>MonoBehaviour</c>; a headless
    /// <c>GameObject</c> is created per-test and destroyed in TearDown.
    /// Private serialised fields are injected via reflection — the same pattern
    /// used throughout this test suite.
    ///
    /// <see cref="SaveSystem.Delete"/> is called in <see cref="SetUp"/> and
    /// <see cref="TearDown"/> to prevent test pollution from disk writes.
    /// </summary>
    public class SurvivalMatchManagerTests
    {
        // ── Per-test scene objects ─────────────────────────────────────────────

        private GameObject           _go;
        private SurvivalMatchManager _smm;
        private WaveConfigSO         _config;
        private WaveManagerSO        _waveManager;
        private PlayerWallet         _wallet;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        /// <summary>
        /// Creates a headless GO with <see cref="SurvivalMatchManager"/> but leaves
        /// the GO inactive so Awake/OnEnable do not fire until explicitly requested.
        /// </summary>
        private static SurvivalMatchManager MakeController(out GameObject go)
        {
            go = new GameObject("TestSurvivalMatchManager");
            go.SetActive(false); // prevent OnEnable before wiring
            return go.AddComponent<SurvivalMatchManager>();
        }

        /// <summary>
        /// Returns a fresh <see cref="WaveManagerSO"/> with an active survival run
        /// (wave 1 started using default config: 1 bot per wave).
        /// </summary>
        private static WaveManagerSO MakeActiveManager(WaveConfigSO config)
        {
            var mgr = ScriptableObject.CreateInstance<WaveManagerSO>();
            mgr.StartSurvival(config);
            return mgr;
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            SaveSystem.Delete(); // start each persistence test from a clean slate

            _go     = new GameObject("SurvivalMatchManagerTestGO");
            _go.SetActive(false);
            _smm    = _go.AddComponent<SurvivalMatchManager>();

            _config      = ScriptableObject.CreateInstance<WaveConfigSO>();
            _waveManager = ScriptableObject.CreateInstance<WaveManagerSO>();

            _wallet = ScriptableObject.CreateInstance<PlayerWallet>();
            _wallet.Reset(); // applies default starting balance
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_waveManager);
            Object.DestroyImmediate(_wallet);
            SaveSystem.Delete(); // leave disk clean after each test

            _go          = null;
            _smm         = null;
            _config      = null;
            _waveManager = null;
            _wallet      = null;
        }

        // ── OnEnable / OnDisable — null guards ────────────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            // All fields remain null
            Assert.DoesNotThrow(() => _go.SetActive(true),
                "OnEnable with all null refs must not throw.");
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            _go.SetActive(true);
            Assert.DoesNotThrow(() => _go.SetActive(false),
                "OnDisable with all null refs must not throw.");
        }

        [Test]
        public void OnEnable_NullChannels_DoesNotThrow()
        {
            // Assign data refs; leave event channels null
            SetField(_smm, "_waveConfig",  _config);
            SetField(_smm, "_waveManager", _waveManager);
            SetField(_smm, "_playerWallet", _wallet);
            // _onWaveCompleted and _onPlayerDeath remain null

            Assert.DoesNotThrow(() => _go.SetActive(true),
                "OnEnable with null channels must not throw.");
        }

        [Test]
        public void OnDisable_NullChannels_DoesNotThrow()
        {
            SetField(_smm, "_waveConfig",  _config);
            SetField(_smm, "_waveManager", _waveManager);
            _go.SetActive(true);

            Assert.DoesNotThrow(() => _go.SetActive(false),
                "OnDisable with null channels must not throw.");
        }

        // ── StartSurvival ─────────────────────────────────────────────────────

        [Test]
        public void StartSurvival_NullWaveManager_IsNoOp()
        {
            // _waveManager left null; _waveConfig assigned
            SetField(_smm, "_waveConfig", _config);
            _go.SetActive(true);

            Assert.DoesNotThrow(() => _smm.StartSurvival(),
                "StartSurvival with null _waveManager must not throw.");
            // No IsActive to verify since manager is null — just confirming no exception.
        }

        [Test]
        public void StartSurvival_NullWaveConfig_IsNoOp()
        {
            // _waveConfig left null; _waveManager assigned
            SetField(_smm, "_waveManager", _waveManager);
            _go.SetActive(true);

            Assert.DoesNotThrow(() => _smm.StartSurvival(),
                "StartSurvival with null _waveConfig must not throw.");
            Assert.IsFalse(_waveManager.IsActive,
                "WaveManager must not become active when _waveConfig is null.");
        }

        [Test]
        public void StartSurvival_BothAssigned_SetsWaveManagerActive()
        {
            SetField(_smm, "_waveConfig",  _config);
            SetField(_smm, "_waveManager", _waveManager);
            _go.SetActive(true);

            _smm.StartSurvival();

            Assert.IsTrue(_waveManager.IsActive,
                "WaveManager must be active after StartSurvival.");
        }

        // ── HandleWaveCompleted ───────────────────────────────────────────────

        [Test]
        public void HandleWaveCompleted_NullWaveManager_IsNoOp()
        {
            // _waveManager left null
            SetField(_smm, "_waveConfig", _config);
            SetField(_smm, "_playerWallet", _wallet);
            _go.SetActive(true);

            int balanceBefore = _wallet.Balance;
            Assert.DoesNotThrow(() => _smm.HandleWaveCompleted(),
                "HandleWaveCompleted with null _waveManager must not throw.");
            Assert.AreEqual(balanceBefore, _wallet.Balance,
                "Wallet balance must not change when _waveManager is null.");
        }

        [Test]
        public void HandleWaveCompleted_NullWaveConfig_IsNoOp()
        {
            // Start survival directly so manager is active
            _waveManager.StartSurvival(_config);

            // Now clear _waveConfig reference so SurvivalMatchManager sees null
            SetField(_smm, "_waveManager", _waveManager);
            // _waveConfig left null
            SetField(_smm, "_playerWallet", _wallet);
            _go.SetActive(true);

            int waveBefore    = _waveManager.CurrentWave;
            int balanceBefore = _wallet.Balance;

            Assert.DoesNotThrow(() => _smm.HandleWaveCompleted(),
                "HandleWaveCompleted with null _waveConfig must not throw.");
            Assert.AreEqual(waveBefore, _waveManager.CurrentWave,
                "CurrentWave must not advance when _waveConfig is null.");
            Assert.AreEqual(balanceBefore, _wallet.Balance,
                "Wallet balance must not change when _waveConfig is null.");
        }

        [Test]
        public void HandleWaveCompleted_AwardsCreditsForCurrentWave()
        {
            // Active survival on wave 1; default config: baseWaveReward=50, bonus=10
            _waveManager.StartSurvival(_config);
            int expectedReward = Mathf.RoundToInt(_config.GetRewardForWave(1));

            SetField(_smm, "_waveConfig",   _config);
            SetField(_smm, "_waveManager",  _waveManager);
            SetField(_smm, "_playerWallet", _wallet);
            _go.SetActive(true);

            int balanceBefore = _wallet.Balance;
            _smm.HandleWaveCompleted();

            Assert.AreEqual(balanceBefore + expectedReward, _wallet.Balance,
                $"Wallet must increase by {expectedReward} after completing wave 1.");
        }

        [Test]
        public void HandleWaveCompleted_AdvancesToNextWave()
        {
            _waveManager.StartSurvival(_config); // wave = 1

            SetField(_smm, "_waveConfig",   _config);
            SetField(_smm, "_waveManager",  _waveManager);
            SetField(_smm, "_playerWallet", _wallet);
            _go.SetActive(true);

            _smm.HandleWaveCompleted();

            Assert.AreEqual(2, _waveManager.CurrentWave,
                "CurrentWave must be 2 after HandleWaveCompleted advances the wave.");
        }

        [Test]
        public void HandleWaveCompleted_PersistsBestWaveToSaveData()
        {
            // Complete wave 1 so BestWave becomes 1 when RecordBotDefeated fires,
            // then call HandleWaveCompleted to persist.
            _waveManager.StartSurvival(_config);       // wave 1, 1 bot (default config)
            _waveManager.RecordBotDefeated();          // last bot → BestWave = 1

            SetField(_smm, "_waveConfig",  _config);
            SetField(_smm, "_waveManager", _waveManager);
            _go.SetActive(true);

            _smm.HandleWaveCompleted();

            SaveData save = SaveSystem.Load();
            Assert.AreEqual(_waveManager.BestWave, save.survivalBestWave,
                "survivalBestWave in SaveData must equal WaveManagerSO.BestWave after HandleWaveCompleted.");
        }

        // ── HandlePlayerDeath ─────────────────────────────────────────────────

        [Test]
        public void HandlePlayerDeath_NullWaveManager_IsNoOp()
        {
            // _waveManager left null
            _go.SetActive(true);

            Assert.DoesNotThrow(() => _smm.HandlePlayerDeath(),
                "HandlePlayerDeath with null _waveManager must not throw.");
        }

        [Test]
        public void HandlePlayerDeath_WhenNotActive_IsNoOp()
        {
            // waveManager exists but IsActive = false
            SetField(_smm, "_waveManager", _waveManager);
            _go.SetActive(true);

            Assert.IsFalse(_waveManager.IsActive);
            Assert.DoesNotThrow(() => _smm.HandlePlayerDeath(),
                "HandlePlayerDeath when waveManager not active must not throw.");
            Assert.IsFalse(_waveManager.IsActive,
                "IsActive must remain false if HandlePlayerDeath called while not active.");
        }

        [Test]
        public void HandlePlayerDeath_EndsActiveSurvival()
        {
            _waveManager.StartSurvival(_config); // IsActive = true

            SetField(_smm, "_waveConfig",  _config);
            SetField(_smm, "_waveManager", _waveManager);
            _go.SetActive(true);

            _smm.HandlePlayerDeath();

            Assert.IsFalse(_waveManager.IsActive,
                "WaveManagerSO.IsActive must be false after HandlePlayerDeath.");
        }

        [Test]
        public void HandlePlayerDeath_PersistsBestWaveToSaveData()
        {
            // Run to wave 2 so BestWave > 0 when EndSurvival fires.
            _waveManager.StartSurvival(_config);        // wave 1
            _waveManager.RecordBotDefeated();           // wave 1 complete → BestWave=1
            _waveManager.StartNextWave(_config);        // advance to wave 2

            SetField(_smm, "_waveConfig",  _config);
            SetField(_smm, "_waveManager", _waveManager);
            _go.SetActive(true);

            _smm.HandlePlayerDeath(); // EndSurvival → BestWave updated to max(1, 2)=2, then persist

            SaveData save = SaveSystem.Load();
            Assert.AreEqual(_waveManager.BestWave, save.survivalBestWave,
                "survivalBestWave in SaveData must equal WaveManagerSO.BestWave after HandlePlayerDeath.");
        }

        // ── Event-channel wiring ──────────────────────────────────────────────

        [Test]
        public void OnWaveCompleted_Raised_TriggersHandleWaveCompleted()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            _waveManager.StartSurvival(_config); // wave 1 active

            SetField(_smm, "_waveConfig",      _config);
            SetField(_smm, "_waveManager",     _waveManager);
            SetField(_smm, "_playerWallet",    _wallet);
            SetField(_smm, "_onWaveCompleted", channel);
            _go.SetActive(true); // OnEnable → subscribe

            int waveBefore = _waveManager.CurrentWave;
            channel.Raise(); // → HandleWaveCompleted → StartNextWave

            Object.DestroyImmediate(channel);
            Assert.AreEqual(waveBefore + 1, _waveManager.CurrentWave,
                "Raising _onWaveCompleted must trigger HandleWaveCompleted (advances wave).");
        }

        [Test]
        public void OnPlayerDeath_Raised_TriggersHandlePlayerDeath()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            _waveManager.StartSurvival(_config); // IsActive = true

            SetField(_smm, "_waveConfig",    _config);
            SetField(_smm, "_waveManager",   _waveManager);
            SetField(_smm, "_onPlayerDeath", channel);
            _go.SetActive(true); // OnEnable → subscribe

            channel.Raise(); // → HandlePlayerDeath → EndSurvival

            Object.DestroyImmediate(channel);
            Assert.IsFalse(_waveManager.IsActive,
                "Raising _onPlayerDeath must trigger HandlePlayerDeath (ends survival).");
        }

        // ── OnDisable unregisters ─────────────────────────────────────────────

        [Test]
        public void OnDisable_UnregistersChannels()
        {
            var waveCompleted = ScriptableObject.CreateInstance<VoidGameEvent>();
            var playerDeath   = ScriptableObject.CreateInstance<VoidGameEvent>();

            // External counters registered before the controller — these must
            // still fire after disable; the controller's internal handlers must not.
            int waveCompletedFires = 0;
            int playerDeathFires   = 0;
            waveCompleted.RegisterCallback(() => waveCompletedFires++);
            playerDeath.RegisterCallback(()   => playerDeathFires++);

            _waveManager.StartSurvival(_config);

            SetField(_smm, "_waveConfig",      _config);
            SetField(_smm, "_waveManager",     _waveManager);
            SetField(_smm, "_onWaveCompleted", waveCompleted);
            SetField(_smm, "_onPlayerDeath",   playerDeath);
            _go.SetActive(true);  // OnEnable → subscribe
            _go.SetActive(false); // OnDisable → unsubscribe

            int waveBeforeRaise = _waveManager.CurrentWave;

            // Raise both channels — the controller must NOT react.
            waveCompleted.Raise();
            playerDeath.Raise();

            Object.DestroyImmediate(waveCompleted);
            Object.DestroyImmediate(playerDeath);

            // External counters fired once each (one Raise each); controller's
            // HandleWaveCompleted must not have incremented the wave.
            Assert.AreEqual(1, waveCompletedFires,
                "External counter for _onWaveCompleted must fire once.");
            Assert.AreEqual(1, playerDeathFires,
                "External counter for _onPlayerDeath must fire once.");
            Assert.AreEqual(waveBeforeRaise, _waveManager.CurrentWave,
                "HandleWaveCompleted must NOT fire after OnDisable (CurrentWave unchanged).");
        }
    }
}
