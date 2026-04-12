using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests verifying the tier/rating gating added to
    /// <see cref="TournamentManager.StartTournament"/> in T113.
    ///
    /// Covers:
    ///   • Backwards-compatibility: null _buildRating → gate bypassed, StartTournament proceeds.
    ///   • RequiresBronze + player Unranked → StartTournament blocked (no state change, no fee).
    ///   • RequiresBronze + player Bronze → StartTournament proceeds, entry fee deducted.
    ///   • RequiresMinRating 500 + player 499 → blocked (no fee deducted).
    ///   • RequiresMinRating 500 + player 500 → proceeds, entry fee deducted.
    ///   • Gate fail never deducts entry fee (safety property).
    ///   • Both gates pass → tournament activated and entry fee deducted.
    /// </summary>
    public class TournamentManagerGatingTests
    {
        // ── Scene objects ─────────────────────────────────────────────────────

        private GameObject        _go;
        private TournamentManager _manager;

        // ── ScriptableObjects ─────────────────────────────────────────────────

        private TournamentStateSO _state;
        private TournamentConfig  _config;
        private PlayerWallet      _wallet;
        private BuildRatingSO     _buildRating;
        private RobotTierConfig   _tierConfig;
        private IntGameEvent      _balanceEvent;

        // ── Reflection helper ─────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        /// <summary>Sets all required manager fields.</summary>
        private void WireManager(BuildRatingSO buildRating, RobotTierConfig tierConfig)
        {
            SetField(_manager, "_tournament",   _state);
            SetField(_manager, "_config",       _config);
            SetField(_manager, "_wallet",       _wallet);
            SetField(_manager, "_buildRating",  buildRating);
            SetField(_manager, "_tierConfig",   tierConfig);
        }

        /// <summary>Activates the GameObject (Awake → OnEnable).</summary>
        private void Activate() => _go.SetActive(true);

        /// <summary>
        /// Builds a minimal <see cref="RobotTierConfig"/> with Bronze threshold = 100.
        /// </summary>
        private RobotTierConfig BuildTierConfig()
        {
            var cfg = ScriptableObject.CreateInstance<RobotTierConfig>();
            var thresholds = new List<TierThresholdEntry>
            {
                new TierThresholdEntry
                {
                    ratingThreshold = 100,
                    tier            = RobotTierLevel.Bronze,
                    displayName     = "Bronze",
                    tintColor       = Color.white
                }
            };
            SetField(cfg, "_thresholds", thresholds);
            return cfg;
        }

        /// <summary>Creates a <see cref="BuildRatingSO"/> with the given rating.</summary>
        private BuildRatingSO MakeBuildRating(int rating)
        {
            var br = ScriptableObject.CreateInstance<BuildRatingSO>();
            SetField(br, "_currentRating", rating);
            return br;
        }

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TournamentManagerGatingHost");
            _go.SetActive(false);
            _manager = _go.AddComponent<TournamentManager>();

            // TournamentStateSO with minimal event channels
            _state = ScriptableObject.CreateInstance<TournamentStateSO>();

            // TournamentConfig: 3 rounds, entry fee 100, requires Bronze, minRating 0 by default
            _config = ScriptableObject.CreateInstance<TournamentConfig>();
            SetField(_config, "_roundCount", 3);
            SetField(_config, "_entryFee",   100);

            // PlayerWallet: start balance 1000
            _balanceEvent = ScriptableObject.CreateInstance<IntGameEvent>();
            _wallet       = ScriptableObject.CreateInstance<PlayerWallet>();
            SetField(_wallet, "_startingBalance",  1000);
            SetField(_wallet, "_onBalanceChanged", _balanceEvent);
            _wallet.Reset();

            _buildRating = MakeBuildRating(0);
            _tierConfig  = BuildTierConfig();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_state);
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_wallet);
            Object.DestroyImmediate(_buildRating);
            Object.DestroyImmediate(_tierConfig);
            Object.DestroyImmediate(_balanceEvent);
        }

        // ── Backwards-compatibility ───────────────────────────────────────────

        [Test]
        public void StartTournament_NullBuildRating_GateBypassed_TournamentStarts()
        {
            // _buildRating null → gate skipped entirely
            WireManager(buildRating: null, tierConfig: null);
            Activate();

            _manager.StartTournament();

            Assert.IsTrue(_state.IsActive,
                "Tournament must start when _buildRating is null (gate bypassed).");
        }

        // ── Tier gating ───────────────────────────────────────────────────────

        [Test]
        public void StartTournament_RequiresBronze_PlayerUnranked_DoesNotStart()
        {
            SetField(_config, "_requiredTier", RobotTierLevel.Bronze);
            // _buildRating has rating 0 → Unranked → fails Bronze
            WireManager(_buildRating, _tierConfig);
            Activate();

            _manager.StartTournament();

            Assert.IsFalse(_state.IsActive,
                "Tournament must NOT start when tier requirement is not met.");
        }

        [Test]
        public void StartTournament_RequiresBronze_PlayerBronze_Starts()
        {
            SetField(_config, "_requiredTier", RobotTierLevel.Bronze);
            var br = MakeBuildRating(150); // 150 ≥ 100 → Bronze
            WireManager(br, _tierConfig);
            Activate();

            _manager.StartTournament();
            Object.DestroyImmediate(br);

            Assert.IsTrue(_state.IsActive,
                "Tournament must start when player meets the tier requirement.");
        }

        [Test]
        public void StartTournament_TierGateFails_DoesNotDeductEntryFee()
        {
            SetField(_config, "_requiredTier", RobotTierLevel.Bronze);
            // rating 0 → Unranked → tier gate fails
            WireManager(_buildRating, _tierConfig);
            Activate();
            int balanceBefore = _wallet.Balance;

            _manager.StartTournament();

            Assert.AreEqual(balanceBefore, _wallet.Balance,
                "Entry fee must NOT be deducted when the tier gate blocks entry.");
        }

        // ── Rating gating ─────────────────────────────────────────────────────

        [Test]
        public void StartTournament_RequiresMinRating500_PlayerHas499_DoesNotStart()
        {
            SetField(_config, "_minRating", 500);
            var br = MakeBuildRating(499);
            WireManager(br, _tierConfig);
            Activate();

            _manager.StartTournament();
            Object.DestroyImmediate(br);

            Assert.IsFalse(_state.IsActive,
                "Tournament must NOT start when rating is below MinRating.");
        }

        [Test]
        public void StartTournament_RequiresMinRating500_PlayerHas500_Starts()
        {
            SetField(_config, "_minRating", 500);
            var br = MakeBuildRating(500);
            WireManager(br, _tierConfig);
            Activate();

            _manager.StartTournament();
            Object.DestroyImmediate(br);

            Assert.IsTrue(_state.IsActive,
                "Tournament must start when player meets the MinRating requirement exactly.");
        }

        [Test]
        public void StartTournament_RatingGateFails_DoesNotDeductEntryFee()
        {
            SetField(_config, "_minRating", 500);
            var br = MakeBuildRating(499);
            WireManager(br, _tierConfig);
            Activate();
            int balanceBefore = _wallet.Balance;

            _manager.StartTournament();
            Object.DestroyImmediate(br);

            Assert.AreEqual(balanceBefore, _wallet.Balance,
                "Entry fee must NOT be deducted when the rating gate blocks entry.");
        }

        [Test]
        public void StartTournament_BothGatesPass_TournamentActivatedAndEntryFeeDeducted()
        {
            SetField(_config, "_requiredTier", RobotTierLevel.Bronze);
            SetField(_config, "_minRating",    100);
            var br = MakeBuildRating(200); // 200 ≥ 100 → Bronze; 200 ≥ 100 minRating
            WireManager(br, _tierConfig);
            Activate();
            int balanceBefore = _wallet.Balance;

            _manager.StartTournament();
            Object.DestroyImmediate(br);

            Assert.IsTrue(_state.IsActive,
                "Tournament must be activated when all gate requirements are met.");
            Assert.AreEqual(balanceBefore - _config.EntryFee, _wallet.Balance,
                "Entry fee must be deducted exactly once when all gates pass.");
        }
    }
}
