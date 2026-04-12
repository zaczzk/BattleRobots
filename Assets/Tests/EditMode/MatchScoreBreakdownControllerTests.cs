using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchScoreBreakdownController"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all-null inspector refs → no throw.
    ///   • OnEnable / OnDisable with null event channel → no throw.
    ///   • OnDisable unregisters the refresh delegate from _onMatchEnded.
    ///   • Refresh() with null MatchResultSO → does not throw (shows fallback text).
    ///   • Refresh() win result → base text contains "1000".
    ///   • Refresh() loss result → base text contains "100".
    ///   • Refresh() win at 60 s → time-bonus text contains "300".
    ///   • Refresh() loss result → time-bonus text contains em dash (no bonus).
    ///   • Refresh() DamageDone=200 → damage-dealt text contains "400" (+floor(200×2)).
    ///   • Refresh() DamageTaken=100 → damage-taken text contains "100" and "-".
    ///   • Refresh() PersonalBestSO.IsNewBest true → new-best banner activated.
    ///   • Refresh() PersonalBestSO.IsNewBest false → new-best banner hidden.
    ///   • Refresh() null PersonalBestSO → new-best banner hidden, no throw.
    ///   • Refresh() with PersonalBestSO assigned → total text shows CurrentScore.
    ///   • Refresh() win result → bonus-credits text contains "+" when bonusEarned>0.
    ///   • Refresh() loss result no bonus → bonus-credits text contains em dash.
    ///
    /// All tests run headless (no Unity Editor scene required).
    /// Reflection is used to inject private serialized fields.
    /// </summary>
    public class MatchScoreBreakdownControllerTests
    {
        // ── Reflection helpers ────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Factory helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Creates a controller on an initially-inactive GameObject so Awake / OnEnable
        /// do not fire until the test explicitly activates the object.
        /// </summary>
        private static MatchScoreBreakdownController MakeController(out GameObject go)
        {
            go = new GameObject("MatchScoreBreakdownTest");
            go.SetActive(false);
            return go.AddComponent<MatchScoreBreakdownController>();
        }

        private static Text AddText(GameObject parent, string childName)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(parent.transform);
            return child.AddComponent<Text>();
        }

        private static MatchResultSO MakeWinResult(float durationSeconds = 60f,
                                                   float damageDone      = 0f,
                                                   float damageTaken     = 0f,
                                                   int   bonusEarned     = 0)
        {
            var r = ScriptableObject.CreateInstance<MatchResultSO>();
            r.Write(playerWon: true, durationSeconds: durationSeconds,
                    currencyEarned: 200, newWalletBalance: 500,
                    damageDone: damageDone, damageTaken: damageTaken,
                    bonusEarned: bonusEarned);
            return r;
        }

        private static MatchResultSO MakeLossResult(float durationSeconds = 90f,
                                                    float damageDone      = 0f,
                                                    float damageTaken     = 0f)
        {
            var r = ScriptableObject.CreateInstance<MatchResultSO>();
            r.Write(playerWon: false, durationSeconds: durationSeconds,
                    currencyEarned: 50, newWalletBalance: 300,
                    damageDone: damageDone, damageTaken: damageTaken);
            return r;
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
            var ctrl   = go.GetComponent<MatchScoreBreakdownController>();
            var result = MakeWinResult();
            SetField(ctrl, "_matchResult", result);
            // _onMatchEnded remains null
            Assert.DoesNotThrow(() => go.SetActive(true));
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(result);
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
        public void OnDisable_UnregistersFromOnMatchEnded()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int externalCount = 0;
            channel.RegisterCallback(() => externalCount++);

            MakeController(out GameObject go);
            var ctrl = go.GetComponent<MatchScoreBreakdownController>();
            SetField(ctrl, "_onMatchEnded", channel);

            go.SetActive(true);   // Awake + OnEnable — controller subscribes
            go.SetActive(false);  // OnDisable — controller must unsubscribe

            channel.Raise();      // only the external counter should fire

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);

            Assert.AreEqual(1, externalCount,
                "Only the external counter should fire; controller must be unsubscribed after OnDisable.");
        }

        // ── Refresh — null MatchResult ────────────────────────────────────────

        [Test]
        public void Refresh_NullMatchResult_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<MatchScoreBreakdownController>();
            go.SetActive(true);
            // _matchResult is null
            Assert.DoesNotThrow(() => ctrl.Refresh());
            Object.DestroyImmediate(go);
        }

        // ── Base score text ───────────────────────────────────────────────────

        [Test]
        public void Refresh_WinResult_BaseTextContainsOneThousand()
        {
            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<MatchScoreBreakdownController>();
            var result = MakeWinResult();
            var text   = AddText(go, "BaseText");
            SetField(ctrl, "_matchResult",   result);
            SetField(ctrl, "_baseScoreText", text);
            go.SetActive(true);

            ctrl.Refresh();

            StringAssert.Contains("1000", text.text, "Win result base must contain '1000'.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Refresh_LossResult_BaseTextContainsOneHundred()
        {
            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<MatchScoreBreakdownController>();
            var result = MakeLossResult();
            var text   = AddText(go, "BaseText");
            SetField(ctrl, "_matchResult",   result);
            SetField(ctrl, "_baseScoreText", text);
            go.SetActive(true);

            ctrl.Refresh();

            StringAssert.Contains("100", text.text, "Loss result base must contain '100'.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(result);
        }

        // ── Time bonus text ───────────────────────────────────────────────────

        [Test]
        public void Refresh_WinAt60Seconds_TimeBonusContainsThreeHundred()
        {
            // time bonus = max(0, 600 - 60×5) = 300
            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<MatchScoreBreakdownController>();
            var result = MakeWinResult(durationSeconds: 60f);
            var text   = AddText(go, "TimeBonusText");
            SetField(ctrl, "_matchResult",   result);
            SetField(ctrl, "_timeBonusText", text);
            go.SetActive(true);

            ctrl.Refresh();

            StringAssert.Contains("300", text.text, "60-second win should show time bonus of 300.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Refresh_LossResult_TimeBonusContainsEmDash()
        {
            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<MatchScoreBreakdownController>();
            var result = MakeLossResult();
            var text   = AddText(go, "TimeBonusText");
            SetField(ctrl, "_matchResult",   result);
            SetField(ctrl, "_timeBonusText", text);
            go.SetActive(true);

            ctrl.Refresh();

            StringAssert.Contains("\u2014", text.text,
                "Loss result time bonus must contain em dash (no time bonus on a loss).");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(result);
        }

        // ── Damage text ───────────────────────────────────────────────────────

        [Test]
        public void Refresh_DamageDone200_DamageDealtContainsFourHundred()
        {
            // floor(200 × 2) = 400
            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<MatchScoreBreakdownController>();
            var result = MakeWinResult(damageDone: 200f);
            var text   = AddText(go, "DamageDealtText");
            SetField(ctrl, "_matchResult",    result);
            SetField(ctrl, "_damageDealtText", text);
            go.SetActive(true);

            ctrl.Refresh();

            StringAssert.Contains("400", text.text,
                "DamageDone=200 should show damage dealt contribution of 400.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Refresh_DamageTaken100_DamageTakenContainsMinus100()
        {
            // floor(100) = 100, shown as "-100"
            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<MatchScoreBreakdownController>();
            var result = MakeWinResult(damageTaken: 100f);
            var text   = AddText(go, "DamageTakenText");
            SetField(ctrl, "_matchResult",    result);
            SetField(ctrl, "_damageTakenText", text);
            go.SetActive(true);

            ctrl.Refresh();

            StringAssert.Contains("100", text.text,
                "DamageTaken=100 should appear in the damage-taken text.");
            StringAssert.Contains("-", text.text,
                "Damage-taken text must include a minus sign to indicate a penalty.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(result);
        }

        // ── New-best banner ───────────────────────────────────────────────────

        [Test]
        public void Refresh_PersonalBestIsNewBest_BannerActivated()
        {
            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<MatchScoreBreakdownController>();
            var result = MakeWinResult();
            var pb     = ScriptableObject.CreateInstance<PersonalBestSO>();
            pb.Submit(1500);                // first submission → IsNewBest = true
            var banner = new GameObject("Banner");
            banner.SetActive(false);
            SetField(ctrl, "_matchResult",   result);
            SetField(ctrl, "_personalBest",  pb);
            SetField(ctrl, "_newBestBanner", banner);
            go.SetActive(true);

            ctrl.Refresh();

            Assert.IsTrue(banner.activeSelf,
                "New-best banner must be active when PersonalBestSO.IsNewBest is true.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(banner);
            Object.DestroyImmediate(result);
            Object.DestroyImmediate(pb);
        }

        [Test]
        public void Refresh_PersonalBestNotNewBest_BannerHidden()
        {
            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<MatchScoreBreakdownController>();
            var result = MakeWinResult();
            var pb     = ScriptableObject.CreateInstance<PersonalBestSO>();
            pb.Submit(2000);   // first submit → IsNewBest = true
            pb.Submit(1500);   // second submit below previous best → IsNewBest = false
            var banner = new GameObject("Banner");
            banner.SetActive(true);
            SetField(ctrl, "_matchResult",   result);
            SetField(ctrl, "_personalBest",  pb);
            SetField(ctrl, "_newBestBanner", banner);
            go.SetActive(true);

            ctrl.Refresh();

            Assert.IsFalse(banner.activeSelf,
                "New-best banner must be hidden when PersonalBestSO.IsNewBest is false.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(banner);
            Object.DestroyImmediate(result);
            Object.DestroyImmediate(pb);
        }

        [Test]
        public void Refresh_NullPersonalBest_BannerHidden_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<MatchScoreBreakdownController>();
            var result = MakeWinResult();
            var banner = new GameObject("Banner");
            banner.SetActive(true);
            SetField(ctrl, "_matchResult",   result);
            // _personalBest is null
            SetField(ctrl, "_newBestBanner", banner);
            go.SetActive(true);

            Assert.DoesNotThrow(() => ctrl.Refresh());

            Assert.IsFalse(banner.activeSelf,
                "New-best banner must be hidden when _personalBest is not assigned.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(banner);
            Object.DestroyImmediate(result);
        }

        // ── Total score uses PersonalBestSO ───────────────────────────────────

        [Test]
        public void Refresh_WithPersonalBest_TotalTextShowsCurrentScore()
        {
            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<MatchScoreBreakdownController>();
            var result = MakeWinResult();
            var pb     = ScriptableObject.CreateInstance<PersonalBestSO>();
            pb.Submit(9999);                // sets CurrentScore = 9999
            var text   = AddText(go, "TotalScoreText");
            SetField(ctrl, "_matchResult",    result);
            SetField(ctrl, "_personalBest",   pb);
            SetField(ctrl, "_totalScoreText", text);
            go.SetActive(true);

            ctrl.Refresh();

            StringAssert.Contains("9999", text.text,
                "Total score text must show PersonalBestSO.CurrentScore when assigned.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(result);
            Object.DestroyImmediate(pb);
        }

        // ── Bonus credits text ────────────────────────────────────────────────

        [Test]
        public void Refresh_WinWithBonus_BonusCreditsContainsPlus()
        {
            // bonusEarned=175 → bonusCredits = 175×3 = 525
            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<MatchScoreBreakdownController>();
            var result = MakeWinResult(bonusEarned: 175);
            var text   = AddText(go, "BonusCreditsText");
            SetField(ctrl, "_matchResult",    result);
            SetField(ctrl, "_bonusCreditsText", text);
            go.SetActive(true);

            ctrl.Refresh();

            StringAssert.Contains("+", text.text,
                "Win with bonus conditions met must show a '+' in the bonus-credits text.");
            StringAssert.Contains("525", text.text,
                "Bonus credits = bonusEarned × 3 = 175 × 3 = 525.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Refresh_LossNoBonus_BonusCreditsContainsEmDash()
        {
            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<MatchScoreBreakdownController>();
            var result = MakeLossResult();   // bonusEarned defaults to 0
            var text   = AddText(go, "BonusCreditsText");
            SetField(ctrl, "_matchResult",    result);
            SetField(ctrl, "_bonusCreditsText", text);
            go.SetActive(true);

            ctrl.Refresh();

            StringAssert.Contains("\u2014", text.text,
                "Bonus credits must show em dash when no bonus conditions were satisfied.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(result);
        }
    }
}
