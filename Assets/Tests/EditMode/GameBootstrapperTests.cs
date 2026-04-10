using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="GameBootstrapper"/>.
    ///
    /// Covers two areas:
    ///
    /// 1. <c>RecordMatchAndSave</c> (public API):
    ///    • Null-guard: null record → no throw, disk unchanged.
    ///    • Happy path: record appended to matchHistory with correct wallet snapshot.
    ///    • Wallet balance persisted in SaveData.walletBalance.
    ///    • Null wallet → snapshot = 0, record still persisted.
    ///    • Multiple records accumulate correctly.
    ///
    /// 2. <c>LoadAndApplySaveData</c> (private, invoked via reflection):
    ///    • All-zero SaveData (first launch) → <c>_playerWallet.Reset()</c> is called,
    ///      giving the inspector starting balance (500).
    ///    • SaveData with history and non-zero balance (returning player) →
    ///      <c>_playerWallet.LoadSnapshot(balance)</c> is called, restoring the
    ///      persisted balance.
    ///
    /// Design notes:
    ///   • GameBootstrapper.Awake() calls LoadAndApplySaveData() immediately on
    ///     AddComponent (Awake runs even for inactive GOs in Unity).  All inspector
    ///     fields are null at that point, so the first call is a no-op.  Tests then
    ///     inject the desired fields via reflection and invoke LoadAndApplySaveData()
    ///     a second time to exercise the actual logic.
    ///   • DontDestroyOnLoad() in Awake is effectively a no-op in EditMode (no scene
    ///     loads occur) and does not interfere with Object.DestroyImmediate in TearDown.
    ///   • SaveSystem.Delete() is called in both SetUp and TearDown to guarantee a
    ///     clean slate regardless of test ordering.
    /// </summary>
    public class GameBootstrapperTests
    {
        // ── Scene objects ─────────────────────────────────────────────────────
        private GameObject       _go;
        private GameBootstrapper _bootstrapper;

        // ── ScriptableObjects ─────────────────────────────────────────────────
        private PlayerWallet _wallet;

        // ── Reflection helpers ────────────────────────────────────────────────

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(mi, $"Method '{methodName}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            // Clear any save file left by previous tests or sessions.
            SaveSystem.Delete();

            // Create GO active — Awake fires immediately during AddComponent.
            // All inspector fields are null at this point, so Awake is a no-op.
            _go           = new GameObject("TestGameBootstrapper");
            _bootstrapper = _go.AddComponent<GameBootstrapper>();

            // Fresh wallet: Balance = 0 (property default before Reset() is called).
            _wallet = ScriptableObject.CreateInstance<PlayerWallet>();
            // Do NOT call Reset() here — tests that need a specific starting state
            // set it explicitly to avoid baking in assumptions.
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_wallet);
            SaveSystem.Delete();
            _go           = null;
            _bootstrapper = null;
            _wallet       = null;
        }

        // ── RecordMatchAndSave — null guard ───────────────────────────────────

        [Test]
        public void RecordMatchAndSave_NullRecord_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _bootstrapper.RecordMatchAndSave(null));
        }

        [Test]
        public void RecordMatchAndSave_NullRecord_NothingAddedToDisk()
        {
            _bootstrapper.RecordMatchAndSave(null);

            SaveData data = SaveSystem.Load();
            Assert.AreEqual(0, data.matchHistory.Count,
                "A null record must not be persisted to matchHistory.");
        }

        // ── RecordMatchAndSave — happy path ───────────────────────────────────

        [Test]
        public void RecordMatchAndSave_ValidRecord_AppendedToMatchHistory()
        {
            // Inject wallet so the bootstrapper can read its balance.
            SetField(_bootstrapper, "_playerWallet", _wallet);
            _wallet.Reset(); // Balance = 500

            var record = new MatchRecord { playerWon = true, currencyEarned = 200 };

            _bootstrapper.RecordMatchAndSave(record);

            SaveData data = SaveSystem.Load();
            Assert.AreEqual(1, data.matchHistory.Count,
                "RecordMatchAndSave must append exactly one record to matchHistory.");
            Assert.IsTrue(data.matchHistory[0].playerWon);
            Assert.AreEqual(200, data.matchHistory[0].currencyEarned);
        }

        [Test]
        public void RecordMatchAndSave_ValidRecord_WalletBalancePersisted()
        {
            SetField(_bootstrapper, "_playerWallet", _wallet);
            _wallet.Reset();      // Balance = 500
            _wallet.AddFunds(300); // Balance = 800

            var record = new MatchRecord { currencyEarned = 100 };

            _bootstrapper.RecordMatchAndSave(record);

            SaveData data = SaveSystem.Load();
            Assert.AreEqual(800, data.walletBalance,
                "RecordMatchAndSave must persist the current wallet balance to SaveData.walletBalance.");
        }

        [Test]
        public void RecordMatchAndSave_ValidRecord_SnapshotMatchesWalletBalance()
        {
            SetField(_bootstrapper, "_playerWallet", _wallet);
            _wallet.Reset();      // Balance = 500
            _wallet.AddFunds(150); // Balance = 650

            var record = new MatchRecord { currencyEarned = 100 };

            _bootstrapper.RecordMatchAndSave(record);

            Assert.AreEqual(650, record.walletSnapshot,
                "record.walletSnapshot must be set to the current wallet balance before saving.");
        }

        [Test]
        public void RecordMatchAndSave_NullWallet_UsesZeroBalance()
        {
            // _playerWallet is null by default — snapshot must default to 0.
            var record = new MatchRecord { currencyEarned = 50 };

            _bootstrapper.RecordMatchAndSave(record);

            Assert.AreEqual(0, record.walletSnapshot,
                "When _playerWallet is null, walletSnapshot must be 0.");

            SaveData data = SaveSystem.Load();
            Assert.AreEqual(0, data.walletBalance);
        }

        [Test]
        public void RecordMatchAndSave_MultipleRecords_AllPersisted()
        {
            SetField(_bootstrapper, "_playerWallet", _wallet);
            _wallet.Reset(); // Balance = 500

            var r1 = new MatchRecord { playerWon = true,  currencyEarned = 200 };
            var r2 = new MatchRecord { playerWon = false, currencyEarned = 50  };

            _bootstrapper.RecordMatchAndSave(r1);
            _bootstrapper.RecordMatchAndSave(r2);

            SaveData data = SaveSystem.Load();
            Assert.AreEqual(2, data.matchHistory.Count,
                "Both records must be appended to matchHistory.");
            Assert.IsTrue(data.matchHistory[0].playerWon,  "First record: playerWon should be true.");
            Assert.IsFalse(data.matchHistory[1].playerWon, "Second record: playerWon should be false.");
        }

        // ── LoadAndApplySaveData — isFirstLaunch detection ────────────────────

        [Test]
        public void LoadAndApplySaveData_AllDefaultSave_CallsWalletReset()
        {
            // Empty save: matchHistory.Count == 0, walletBalance == 0, unlockedPartIds.Count == 0
            // → isFirstLaunch = true → Reset() is called → wallet.Balance = _startingBalance (500).
            SetField(_bootstrapper, "_playerWallet", _wallet);
            Assert.AreEqual(0, _wallet.Balance, "Fresh wallet must start at 0 before Reset().");

            // SaveSystem.Load() with no file returns a default-constructed SaveData (all zeros).
            InvokePrivate(_bootstrapper, "LoadAndApplySaveData");

            Assert.AreEqual(500, _wallet.Balance,
                "First-launch detection (all-zero SaveData) must call Reset(), " +
                "setting balance to the inspector starting value of 500.");
        }

        [Test]
        public void LoadAndApplySaveData_ReturningPlayerWithBalance_CallsLoadSnapshot()
        {
            // Persist a save with walletBalance = 350 and one match record.
            // → isFirstLaunch = false (matchHistory.Count > 0)
            // → LoadSnapshot(350) is called → wallet.Balance = 350.
            var existingSave = new SaveData { walletBalance = 350 };
            existingSave.matchHistory.Add(new MatchRecord { playerWon = true });
            SaveSystem.Save(existingSave);

            SetField(_bootstrapper, "_playerWallet", _wallet);
            Assert.AreEqual(0, _wallet.Balance, "Fresh wallet must start at 0.");

            InvokePrivate(_bootstrapper, "LoadAndApplySaveData");

            Assert.AreEqual(350, _wallet.Balance,
                "Returning player: LoadSnapshot must restore the persisted wallet balance (350).");
        }
    }
}
