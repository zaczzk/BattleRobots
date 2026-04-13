using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PrestigeSystemSO"/> and <see cref="PrestigeController"/>.
    ///
    /// PrestigeSystemSOTests covers:
    ///   • FreshInstance: PrestigeCount==0, IsMaxPrestige==false.
    ///   • CanPrestige: null progression → false; non-max-level → false; IsMaxPrestige → false;
    ///     max-level + not-max-prestige → true.
    ///   • Prestige: null progression → no-op; non-max-level → no-op; happy path increments count;
    ///     happy path fires _onPrestige; happy path calls progression.Reset() (level back to 1);
    ///     at IsMaxPrestige cap → no further increment.
    ///   • GetRankLabel / GetRankLabelForCount: 0→"None"; 1→"Bronze I"; 3→"Bronze III";
    ///     4→"Silver I"; 6→"Silver III"; 7→"Gold I"; 9→"Gold III"; 10→"Legend"; >10→"Legend".
    ///   • LoadSnapshot: clamps negatives to 0; clamps above max to max; no event; no-op on 0.
    ///   • Reset: clears count to 0; silent (no event).
    ///
    /// PrestigeControllerTests covers:
    ///   • OnEnable / OnDisable with all-null refs → no throw.
    ///   • OnEnable / OnDisable with null channel → no throw.
    ///   • OnDisable unregisters from _onPrestige channel.
    ///   • Refresh with null _prestigeSystem → em-dash on labels, button non-interactable.
    ///   • Refresh with data → _prestigeCountText shows "×N".
    ///   • Refresh with data → _prestigeRankText shows rank label.
    ///   • DoPrestige persists count when conditions are met.
    ///
    /// All tests run headless (no Unity Editor scene required).
    /// </summary>
    public class PrestigeSystemSOTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static PlayerProgressionSO MakeProgressionAtMaxLevel(int maxLevel = 2)
        {
            var prog = ScriptableObject.CreateInstance<PlayerProgressionSO>();
            SetField(prog, "_maxLevel", maxLevel);
            // Pump enough XP to reach max level
            prog.AddXP(PlayerProgressionSO.TotalXPForLevel(maxLevel) + 1000);
            return prog;
        }

        private static PlayerProgressionSO MakeProgressionBelowMaxLevel(int maxLevel = 10)
        {
            var prog = ScriptableObject.CreateInstance<PlayerProgressionSO>();
            SetField(prog, "_maxLevel", maxLevel);
            // Level 1, no XP — definitely not at max level
            return prog;
        }

        // ── SetUp / TearDown ──────────────────────────────────────────────────

        private PrestigeSystemSO _so;
        private VoidGameEvent    _onPrestige;

        [SetUp]
        public void SetUp()
        {
            _so         = ScriptableObject.CreateInstance<PrestigeSystemSO>();
            _onPrestige = ScriptableObject.CreateInstance<VoidGameEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_so);
            Object.DestroyImmediate(_onPrestige);
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_PrestigeCountIsZero()
        {
            Assert.AreEqual(0, _so.PrestigeCount,
                "Fresh PrestigeSystemSO must have PrestigeCount == 0.");
        }

        [Test]
        public void FreshInstance_IsMaxPrestigeIsFalse()
        {
            Assert.IsFalse(_so.IsMaxPrestige,
                "Fresh PrestigeSystemSO must not be at max prestige.");
        }

        // ── CanPrestige guards ────────────────────────────────────────────────

        [Test]
        public void CanPrestige_NullProgression_ReturnsFalse()
        {
            Assert.IsFalse(_so.CanPrestige(null),
                "CanPrestige must return false when progression is null.");
        }

        [Test]
        public void CanPrestige_BelowMaxLevel_ReturnsFalse()
        {
            var prog = MakeProgressionBelowMaxLevel();
            Assert.IsFalse(_so.CanPrestige(prog),
                "CanPrestige must return false when player is not at max level.");
            Object.DestroyImmediate(prog);
        }

        [Test]
        public void CanPrestige_AtMaxLevel_NotMaxPrestige_ReturnsTrue()
        {
            var prog = MakeProgressionAtMaxLevel();
            Assert.IsTrue(_so.CanPrestige(prog),
                "CanPrestige must return true when player is at max level and not at max prestige.");
            Object.DestroyImmediate(prog);
        }

        [Test]
        public void CanPrestige_AtMaxPrestige_ReturnsFalse()
        {
            SetField(_so, "_maxPrestigeRank", 1);
            _so.LoadSnapshot(1); // already at max prestige
            var prog = MakeProgressionAtMaxLevel();
            Assert.IsFalse(_so.CanPrestige(prog),
                "CanPrestige must return false when IsMaxPrestige is true.");
            Object.DestroyImmediate(prog);
        }

        // ── Prestige — guards ─────────────────────────────────────────────────

        [Test]
        public void Prestige_NullProgression_IsNoOp()
        {
            _so.Prestige(null);
            Assert.AreEqual(0, _so.PrestigeCount,
                "Prestige(null) must not change PrestigeCount.");
        }

        [Test]
        public void Prestige_BelowMaxLevel_IsNoOp()
        {
            var prog = MakeProgressionBelowMaxLevel();
            _so.Prestige(prog);
            Assert.AreEqual(0, _so.PrestigeCount,
                "Prestige below max level must not change PrestigeCount.");
            Object.DestroyImmediate(prog);
        }

        // ── Prestige — happy path ─────────────────────────────────────────────

        [Test]
        public void Prestige_AtMaxLevel_IncrementsPrestigeCount()
        {
            var prog = MakeProgressionAtMaxLevel();
            _so.Prestige(prog);
            Assert.AreEqual(1, _so.PrestigeCount,
                "Prestige at max level must increment PrestigeCount by 1.");
            Object.DestroyImmediate(prog);
        }

        [Test]
        public void Prestige_AtMaxLevel_FiresOnPrestigeEvent()
        {
            SetField(_so, "_onPrestige", _onPrestige);
            int fired = 0;
            _onPrestige.RegisterCallback(() => fired++);

            var prog = MakeProgressionAtMaxLevel();
            _so.Prestige(prog);

            Assert.AreEqual(1, fired, "Prestige must fire _onPrestige.");
            Object.DestroyImmediate(prog);
        }

        [Test]
        public void Prestige_AtMaxLevel_ResetsProgressionToLevel1()
        {
            var prog = MakeProgressionAtMaxLevel();
            Assert.IsTrue(prog.IsMaxLevel, "Precondition: progression must be at max level.");

            _so.Prestige(prog);

            Assert.AreEqual(1, prog.CurrentLevel,
                "Prestige must reset PlayerProgressionSO back to Level 1.");
            Object.DestroyImmediate(prog);
        }

        [Test]
        public void Prestige_AtMaxPrestige_IsNoOp()
        {
            SetField(_so, "_maxPrestigeRank", 1);
            var prog = MakeProgressionAtMaxLevel();
            _so.Prestige(prog);                // reaches max prestige (count = 1)

            // prog was reset; re-pump XP to max level
            prog.AddXP(PlayerProgressionSO.TotalXPForLevel(2) + 1000);
            _so.Prestige(prog);                // should be blocked

            Assert.AreEqual(1, _so.PrestigeCount,
                "Prestige beyond IsMaxPrestige must not increment count further.");
            Object.DestroyImmediate(prog);
        }

        // ── GetRankLabel / GetRankLabelForCount ───────────────────────────────

        [Test]
        public void GetRankLabel_Zero_ReturnsNone()
        {
            Assert.AreEqual("None", _so.GetRankLabel(),
                "PrestigeCount 0 must yield rank label 'None'.");
        }

        [Test]
        public void GetRankLabelForCount_One_ReturnsBronzeI()
        {
            Assert.AreEqual("Bronze I", PrestigeSystemSO.GetRankLabelForCount(1));
        }

        [Test]
        public void GetRankLabelForCount_Three_ReturnsBronzeIII()
        {
            Assert.AreEqual("Bronze III", PrestigeSystemSO.GetRankLabelForCount(3));
        }

        [Test]
        public void GetRankLabelForCount_Four_ReturnsSilverI()
        {
            Assert.AreEqual("Silver I", PrestigeSystemSO.GetRankLabelForCount(4));
        }

        [Test]
        public void GetRankLabelForCount_Six_ReturnsSilverIII()
        {
            Assert.AreEqual("Silver III", PrestigeSystemSO.GetRankLabelForCount(6));
        }

        [Test]
        public void GetRankLabelForCount_Seven_ReturnsGoldI()
        {
            Assert.AreEqual("Gold I", PrestigeSystemSO.GetRankLabelForCount(7));
        }

        [Test]
        public void GetRankLabelForCount_Nine_ReturnsGoldIII()
        {
            Assert.AreEqual("Gold III", PrestigeSystemSO.GetRankLabelForCount(9));
        }

        [Test]
        public void GetRankLabelForCount_Ten_ReturnsLegend()
        {
            Assert.AreEqual("Legend", PrestigeSystemSO.GetRankLabelForCount(10));
        }

        [Test]
        public void GetRankLabelForCount_AboveTen_ReturnsLegend()
        {
            Assert.AreEqual("Legend", PrestigeSystemSO.GetRankLabelForCount(99));
        }

        [Test]
        public void GetRankLabelForCount_Negative_ReturnsNone()
        {
            Assert.AreEqual("None", PrestigeSystemSO.GetRankLabelForCount(-5));
        }

        // ── LoadSnapshot ──────────────────────────────────────────────────────

        [Test]
        public void LoadSnapshot_NegativeCount_ClampsToZero()
        {
            _so.LoadSnapshot(-3);
            Assert.AreEqual(0, _so.PrestigeCount,
                "LoadSnapshot with negative value must clamp PrestigeCount to 0.");
        }

        [Test]
        public void LoadSnapshot_AboveMax_ClampsToMax()
        {
            SetField(_so, "_maxPrestigeRank", 5);
            _so.LoadSnapshot(99);
            Assert.AreEqual(5, _so.PrestigeCount,
                "LoadSnapshot above MaxPrestigeRank must clamp to MaxPrestigeRank.");
        }

        [Test]
        public void LoadSnapshot_Silent_DoesNotFireOnPrestige()
        {
            SetField(_so, "_onPrestige", _onPrestige);
            int fired = 0;
            _onPrestige.RegisterCallback(() => fired++);

            _so.LoadSnapshot(3);

            Assert.AreEqual(0, fired,
                "LoadSnapshot must be silent — must not fire _onPrestige.");
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsPrestigeCount()
        {
            _so.LoadSnapshot(5);
            _so.Reset();
            Assert.AreEqual(0, _so.PrestigeCount,
                "Reset must clear PrestigeCount to 0.");
        }

        [Test]
        public void Reset_Silent_DoesNotFireOnPrestige()
        {
            SetField(_so, "_onPrestige", _onPrestige);
            int fired = 0;
            _onPrestige.RegisterCallback(() => fired++);

            _so.LoadSnapshot(3);
            _so.Reset();

            Assert.AreEqual(0, fired,
                "Reset must be silent — must not fire _onPrestige.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PrestigeController tests
    // ═══════════════════════════════════════════════════════════════════════════

    public class PrestigeControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static PrestigeController MakeController(out GameObject go)
        {
            go = new GameObject("PrestigeControllerTest");
            go.SetActive(false);
            return go.AddComponent<PrestigeController>();
        }

        private static PlayerProgressionSO MakeProgressionAtMaxLevel(int maxLevel = 2)
        {
            var prog = ScriptableObject.CreateInstance<PlayerProgressionSO>();
            SetField(prog, "_maxLevel", maxLevel);
            prog.AddXP(PlayerProgressionSO.TotalXPForLevel(maxLevel) + 1000);
            return prog;
        }

        // ── OnEnable / OnDisable — null guards ────────────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            Assert.DoesNotThrow(() => go.SetActive(true));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var so = ScriptableObject.CreateInstance<PrestigeSystemSO>();
            SetField(go.GetComponent<PrestigeController>(), "_prestigeSystem", so);
            // _onPrestige remains null
            Assert.DoesNotThrow(() => go.SetActive(true));
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            MakeController(out GameObject go);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false));
            Object.DestroyImmediate(go);
        }

        // ── OnDisable unregisters ─────────────────────────────────────────────

        [Test]
        public void OnDisable_UnregistersFromOnPrestige()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int externalCount = 0;
            channel.RegisterCallback(() => externalCount++);

            MakeController(out GameObject go);
            SetField(go.GetComponent<PrestigeController>(), "_onPrestige", channel);

            go.SetActive(true);   // Awake + OnEnable → subscribed
            go.SetActive(false);  // OnDisable → must unsubscribe

            channel.Raise();

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);

            Assert.AreEqual(1, externalCount,
                "Only the external counter should fire; controller must be unsubscribed.");
        }

        // ── Refresh — null _prestigeSystem ───────────────────────────────────

        [Test]
        public void Refresh_NullPrestigeSystem_ShowsEmDashOnLabels()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<PrestigeController>();

            var countGo  = new GameObject(); var countText  = countGo.AddComponent<UnityEngine.UI.Text>();
            var rankGo   = new GameObject(); var rankText   = rankGo.AddComponent<UnityEngine.UI.Text>();

            SetField(ctrl, "_prestigeCountText", countText);
            SetField(ctrl, "_prestigeRankText",  rankText);
            // _prestigeSystem remains null

            go.SetActive(true);

            Assert.AreEqual("\u2014", countText.text,
                "Null _prestigeSystem: _prestigeCountText must show em-dash.");
            Assert.AreEqual("\u2014", rankText.text,
                "Null _prestigeSystem: _prestigeRankText must show em-dash.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(countGo);
            Object.DestroyImmediate(rankGo);
        }

        [Test]
        public void Refresh_NullPrestigeSystem_ButtonNotInteractable()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<PrestigeController>();

            var btnGo = new GameObject();
            btnGo.AddComponent<UnityEngine.UI.Image>(); // Button requires Graphic
            var btn = btnGo.AddComponent<UnityEngine.UI.Button>();
            SetField(ctrl, "_prestigeButton", btn);
            // _prestigeSystem remains null

            go.SetActive(true);

            Assert.IsFalse(btn.interactable,
                "Null _prestigeSystem: Prestige button must not be interactable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(btnGo);
        }

        // ── Refresh — with data ───────────────────────────────────────────────

        [Test]
        public void Refresh_WithData_PrestigeCountTextShowsMultiplierAndCount()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<PrestigeController>();
            var so   = ScriptableObject.CreateInstance<PrestigeSystemSO>();
            so.LoadSnapshot(3); // PrestigeCount = 3

            var labelGo = new GameObject();
            var label   = labelGo.AddComponent<UnityEngine.UI.Text>();
            SetField(ctrl, "_prestigeSystem",    so);
            SetField(ctrl, "_prestigeCountText", label);

            go.SetActive(true);

            Assert.AreEqual("\u00d73", label.text,
                "Refresh must show '×3' on _prestigeCountText when PrestigeCount is 3.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(labelGo);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Refresh_WithData_RankTextShowsRankLabel()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<PrestigeController>();
            var so   = ScriptableObject.CreateInstance<PrestigeSystemSO>();
            so.LoadSnapshot(4); // "Silver I"

            var labelGo = new GameObject();
            var label   = labelGo.AddComponent<UnityEngine.UI.Text>();
            SetField(ctrl, "_prestigeSystem",  so);
            SetField(ctrl, "_prestigeRankText", label);

            go.SetActive(true);

            Assert.AreEqual("Silver I", label.text,
                "Refresh must show the rank label on _prestigeRankText.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(labelGo);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Refresh_WithMaxLevelProgression_ButtonInteractable()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<PrestigeController>();
            var so   = ScriptableObject.CreateInstance<PrestigeSystemSO>();
            var prog = MakeProgressionAtMaxLevel();

            var btnGo = new GameObject();
            btnGo.AddComponent<UnityEngine.UI.Image>();
            var btn = btnGo.AddComponent<UnityEngine.UI.Button>();

            SetField(ctrl, "_prestigeSystem", so);
            SetField(ctrl, "_progression",    prog);
            SetField(ctrl, "_prestigeButton", btn);

            go.SetActive(true);

            Assert.IsTrue(btn.interactable,
                "Prestige button must be interactable when player is at max level and can prestige.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(btnGo);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(prog);
        }
    }
}
