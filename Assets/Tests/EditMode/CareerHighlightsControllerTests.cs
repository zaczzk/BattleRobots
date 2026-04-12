using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="CareerHighlightsController"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all fields null — no throw.
    ///   • OnEnable / OnDisable with null _onHighlightsUpdated channel — no throw.
    ///   • Refresh() with null _highlights — label texts set to '—' fallback strings.
    ///   • Refresh() with valid _highlights but null Text refs — no throw.
    ///   • Refresh() with _highlights that has a non-zero FastestWinSeconds — text set.
    ///   • Refresh() with _highlights.FastestWinSeconds == 0 — shows '—' fallback.
    ///   • Refresh() wires _newHighlightBanner correctly (active when IsNew* true).
    ///   • OnDisable unregisters from _onHighlightsUpdated (external-counter pattern).
    ///
    /// All tests run headless (no full scene required).
    /// </summary>
    public class CareerHighlightsControllerTests
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

        private static (GameObject go, CareerHighlightsController ctrl) MakeCtrl()
        {
            var go   = new GameObject("CareerHighlightsController");
            go.SetActive(false);   // keep inactive until test is ready
            var ctrl = go.AddComponent<CareerHighlightsController>();
            return (go, ctrl);
        }

        private static CareerHighlightsSO MakeHighlights(
            float bestDamage   = 0f,
            float fastestWin   = 0f,
            int   bestCurrency = 0,
            float longestMatch = 0f)
        {
            var so   = ScriptableObject.CreateInstance<CareerHighlightsSO>();
            var snap = new CareerHighlightsSnapshot
            {
                bestSingleMatchDamage   = bestDamage,
                fastestWinSeconds       = fastestWin,
                bestSingleMatchCurrency = bestCurrency,
                longestMatchSeconds     = longestMatch,
            };
            so.LoadSnapshot(snap);
            return so;
        }

        // ── OnEnable / OnDisable — all null refs ──────────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            var (go, _) = MakeCtrl();
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            var (go, _) = MakeCtrl();
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        // ── OnEnable / OnDisable — null channel ───────────────────────────────

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            var (go, ctrl) = MakeCtrl();
            var highlights = MakeHighlights();
            SetField(ctrl, "_highlights", highlights);
            // _onHighlightsUpdated left null
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with null _onHighlightsUpdated must not throw.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(highlights);
        }

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            var (go, ctrl) = MakeCtrl();
            var highlights = MakeHighlights();
            SetField(ctrl, "_highlights", highlights);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with null _onHighlightsUpdated must not throw.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(highlights);
        }

        // ── Refresh — null highlights → dash fallback ─────────────────────────

        [Test]
        public void Refresh_NullHighlights_SetsDashOnAllLabels()
        {
            var (go, ctrl) = MakeCtrl();

            // Create Text components and wire them.
            var bestDamageGO   = new GameObject("BestDamage");
            var fastestWinGO   = new GameObject("FastestWin");
            var bestCurrencyGO = new GameObject("BestCurrency");
            var longestMatchGO = new GameObject("LongestMatch");

            var bestDamageText   = bestDamageGO.AddComponent<Text>();
            var fastestWinText   = fastestWinGO.AddComponent<Text>();
            var bestCurrencyText = bestCurrencyGO.AddComponent<Text>();
            var longestMatchText = longestMatchGO.AddComponent<Text>();

            SetField(ctrl, "_bestDamageText",   bestDamageText);
            SetField(ctrl, "_fastestWinText",   fastestWinText);
            SetField(ctrl, "_bestCurrencyText", bestCurrencyText);
            SetField(ctrl, "_longestMatchText", longestMatchText);
            // _highlights left null

            go.SetActive(true);  // triggers Refresh via OnEnable

            Assert.IsTrue(bestDamageText.text.Contains("\u2014"),
                "_bestDamageText must contain em-dash when _highlights is null.");
            Assert.IsTrue(fastestWinText.text.Contains("\u2014"),
                "_fastestWinText must contain em-dash when _highlights is null.");
            Assert.IsTrue(bestCurrencyText.text.Contains("\u2014"),
                "_bestCurrencyText must contain em-dash when _highlights is null.");
            Assert.IsTrue(longestMatchText.text.Contains("\u2014"),
                "_longestMatchText must contain em-dash when _highlights is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(bestDamageGO);
            Object.DestroyImmediate(fastestWinGO);
            Object.DestroyImmediate(bestCurrencyGO);
            Object.DestroyImmediate(longestMatchGO);
        }

        // ── Refresh — valid highlights, all UI refs null → no throw ───────────

        [Test]
        public void Refresh_WithHighlights_NullUIRefs_DoesNotThrow()
        {
            var (go, ctrl) = MakeCtrl();
            var highlights = MakeHighlights(bestDamage: 300f, fastestWin: 45f,
                                            bestCurrency: 250, longestMatch: 120f);
            SetField(ctrl, "_highlights", highlights);
            // All Text refs left null.
            Assert.DoesNotThrow(() => go.SetActive(true),
                "Refresh() with valid highlights but null Text refs must not throw.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(highlights);
        }

        // ── Refresh — FastestWin text ─────────────────────────────────────────

        [Test]
        public void Refresh_FastestWinSeconds_Zero_ShowsDash()
        {
            var (go, ctrl) = MakeCtrl();
            var highlights = MakeHighlights(fastestWin: 0f);
            var textGO     = new GameObject("FastestWin");
            var text       = textGO.AddComponent<Text>();

            SetField(ctrl, "_highlights",   highlights);
            SetField(ctrl, "_fastestWinText", text);

            go.SetActive(true);

            Assert.IsTrue(text.text.Contains("\u2014"),
                "FastestWinSeconds = 0 must show em-dash (player has never won).");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(highlights);
            Object.DestroyImmediate(textGO);
        }

        [Test]
        public void Refresh_FastestWinSeconds_NonZero_ShowsFormattedDuration()
        {
            var (go, ctrl) = MakeCtrl();
            var highlights = MakeHighlights(fastestWin: 90f);  // 1m 30s
            var textGO     = new GameObject("FastestWin");
            var text       = textGO.AddComponent<Text>();

            SetField(ctrl, "_highlights",   highlights);
            SetField(ctrl, "_fastestWinText", text);

            go.SetActive(true);

            Assert.IsTrue(text.text.Contains("1m"),
                "FastestWinSeconds=90 must show '1m' in _fastestWinText.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(highlights);
            Object.DestroyImmediate(textGO);
        }

        // ── Refresh — _newHighlightBanner ─────────────────────────────────────

        [Test]
        public void Refresh_NewHighlightBanner_Active_WhenIsNewBestDamage()
        {
            var so     = ScriptableObject.CreateInstance<CareerHighlightsSO>();
            // Trigger IsNewBestDamage via Update().
            var result = ScriptableObject.CreateInstance<MatchResultSO>();
            result.Write(false, 60f, 0, 0, damageDone: 250f);
            so.Update(result);
            Object.DestroyImmediate(result);

            var bannerGO   = new GameObject("Banner");
            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_highlights",         so);
            SetField(ctrl, "_newHighlightBanner", bannerGO);

            go.SetActive(true);

            Assert.IsTrue(bannerGO.activeSelf,
                "_newHighlightBanner must be active when any IsNew* flag is true.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(bannerGO);
            Object.DestroyImmediate(so);
        }

        // ── OnDisable — unregisters callback ──────────────────────────────────

        [Test]
        public void OnDisable_UnregistersFromChannel()
        {
            var channel    = ScriptableObject.CreateInstance<VoidGameEvent>();
            int externalCount = 0;
            channel.RegisterCallback(() => externalCount++);

            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_onHighlightsUpdated", channel);

            go.SetActive(true);   // OnEnable registers delegate
            go.SetActive(false);  // OnDisable unregisters it

            channel.Raise();      // only external counter should fire

            Assert.AreEqual(1, externalCount,
                "After OnDisable, only the external counter must fire when channel is raised.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }
    }
}
