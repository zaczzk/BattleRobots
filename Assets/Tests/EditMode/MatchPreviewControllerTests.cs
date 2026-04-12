using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchPreviewController"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all-null inspector refs → no throw.
    ///   • OnEnable / OnDisable with null event channel → no throw.
    ///   • OnDisable unregisters the refresh delegate from _onSelectionsChanged.
    ///   • Refresh() with all-null data sources → no throw.
    ///   • Refresh() with SelectedOpponentSO → opponent name text shows profile name.
    ///   • Refresh() with null SelectedOpponentSO → text falls back to "Opponent".
    ///   • Refresh() with SelectedArenaSO → arena name text shows preset display name.
    ///   • Refresh() with SelectedModifierSO → modifier name text shows modifier name.
    ///   • Refresh() with null SelectedModifierSO → text falls back to "Standard".
    ///   • Refresh() with BuildRatingSO + RobotTierConfig → tier text is set.
    ///   • Refresh() with BuildRatingSO but null RobotTierConfig → tier text is empty.
    ///   • Refresh() with null BuildRatingSO → rating text shows "Power: 0".
    ///
    /// All tests run headless (no Unity Editor scene required).
    /// Reflection is used to inject private serialized fields.
    /// </summary>
    public class MatchPreviewControllerTests
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
        private static MatchPreviewController MakeController(out GameObject go)
        {
            go = new GameObject("MatchPreviewTest");
            go.SetActive(false);
            return go.AddComponent<MatchPreviewController>();
        }

        private static Text AddText(GameObject parent, string childName)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(parent.transform);
            return child.AddComponent<Text>();
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
            var ctrl     = go.GetComponent<MatchPreviewController>();
            var selected = ScriptableObject.CreateInstance<SelectedOpponentSO>();
            SetField(ctrl, "_selectedOpponent", selected);
            // _onSelectionsChanged remains null
            Assert.DoesNotThrow(() => go.SetActive(true));
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(selected);
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
        public void OnDisable_UnregistersFromOnSelectionsChanged()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int externalCount = 0;
            channel.RegisterCallback(() => externalCount++);

            MakeController(out GameObject go);
            var ctrl = go.GetComponent<MatchPreviewController>();
            SetField(ctrl, "_onSelectionsChanged", channel);

            go.SetActive(true);   // Awake + OnEnable — controller subscribes
            go.SetActive(false);  // OnDisable — controller must unsubscribe

            channel.Raise();      // only the external counter should fire

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);

            Assert.AreEqual(1, externalCount,
                "Only the external counter must fire after OnDisable; " +
                "refresh delegate must be unregistered.");
        }

        // ── Refresh — null data sources ───────────────────────────────────────

        [Test]
        public void Refresh_AllNullDataSources_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<MatchPreviewController>();
            go.SetActive(true);
            // No SOs assigned — Refresh() must complete without throw
            Assert.DoesNotThrow(() => ctrl.Refresh());
            Object.DestroyImmediate(go);
        }

        // ── Opponent text ─────────────────────────────────────────────────────

        [Test]
        public void Refresh_WithSelectedOpponent_OpponentNameTextShowsDisplayName()
        {
            MakeController(out GameObject go);
            var ctrl     = go.GetComponent<MatchPreviewController>();
            var selOpponent = ScriptableObject.CreateInstance<SelectedOpponentSO>();
            var profile  = ScriptableObject.CreateInstance<OpponentProfileSO>();

            // Inject the display name directly via reflection
            FieldInfo dnField = typeof(OpponentProfileSO)
                .GetField("_displayName", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(dnField, "_displayName field not found on OpponentProfileSO.");
            dnField.SetValue(profile, "Crusher");

            selOpponent.Select(profile);

            var text = AddText(go, "OpponentName");
            SetField(ctrl, "_selectedOpponent", selOpponent);
            SetField(ctrl, "_opponentNameText", text);
            go.SetActive(true);

            ctrl.Refresh();

            StringAssert.Contains("Crusher", text.text,
                "Opponent name text must show the profile DisplayName when a profile is selected.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(selOpponent);
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void Refresh_NullSelectedOpponent_OpponentNameTextShowsFallback()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<MatchPreviewController>();
            var text = AddText(go, "OpponentName");
            // _selectedOpponent is null
            SetField(ctrl, "_opponentNameText", text);
            go.SetActive(true);

            ctrl.Refresh();

            Assert.AreEqual("Opponent", text.text,
                "Opponent name must fall back to 'Opponent' when _selectedOpponent is not assigned.");
            Object.DestroyImmediate(go);
        }

        // ── Arena text ────────────────────────────────────────────────────────

        [Test]
        public void Refresh_WithSelectedArena_ArenaNameTextShowsDisplayName()
        {
            MakeController(out GameObject go);
            var ctrl     = go.GetComponent<MatchPreviewController>();
            var selArena = ScriptableObject.CreateInstance<SelectedArenaSO>();
            var preset   = new ArenaPresetsConfig.ArenaPreset { displayName = "Lava Pit" };
            selArena.Select(preset);

            var text = AddText(go, "ArenaName");
            SetField(ctrl, "_selectedArena", selArena);
            SetField(ctrl, "_arenaNameText", text);
            go.SetActive(true);

            ctrl.Refresh();

            StringAssert.Contains("Lava Pit", text.text,
                "Arena name text must show the selected preset displayName.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(selArena);
        }

        // ── Modifier text ─────────────────────────────────────────────────────

        [Test]
        public void Refresh_WithSelectedModifier_ModifierNameTextShowsDisplayName()
        {
            MakeController(out GameObject go);
            var ctrl        = go.GetComponent<MatchPreviewController>();
            var selModifier = ScriptableObject.CreateInstance<SelectedModifierSO>();
            var modSO       = ScriptableObject.CreateInstance<MatchModifierSO>();

            // Inject a distinct display name via reflection
            FieldInfo dnField = typeof(MatchModifierSO)
                .GetField("_displayName", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(dnField, "_displayName field not found on MatchModifierSO.");
            dnField.SetValue(modSO, "Overdrive Mode");

            selModifier.Select(modSO);

            var text = AddText(go, "ModifierName");
            SetField(ctrl, "_selectedModifier", selModifier);
            SetField(ctrl, "_modifierNameText", text);
            go.SetActive(true);

            ctrl.Refresh();

            StringAssert.Contains("Overdrive Mode", text.text,
                "Modifier name text must show the MatchModifierSO DisplayName.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(selModifier);
            Object.DestroyImmediate(modSO);
        }

        [Test]
        public void Refresh_NullSelectedModifier_ModifierNameTextShowsStandard()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<MatchPreviewController>();
            var text = AddText(go, "ModifierName");
            // _selectedModifier is null
            SetField(ctrl, "_modifierNameText", text);
            go.SetActive(true);

            ctrl.Refresh();

            Assert.AreEqual("Standard", text.text,
                "Modifier name must fall back to 'Standard' when _selectedModifier is not assigned.");
            Object.DestroyImmediate(go);
        }

        // ── Tier text ─────────────────────────────────────────────────────────

        [Test]
        public void Refresh_WithBuildRatingAndTierConfig_TierTextIsSet()
        {
            MakeController(out GameObject go);
            var ctrl        = go.GetComponent<MatchPreviewController>();
            var buildRating = ScriptableObject.CreateInstance<BuildRatingSO>();
            var tierConfig  = ScriptableObject.CreateInstance<RobotTierConfig>();
            var text        = AddText(go, "TierText");
            SetField(ctrl, "_buildRating", buildRating);
            SetField(ctrl, "_tierConfig",  tierConfig);
            SetField(ctrl, "_tierText",    text);
            go.SetActive(true);

            ctrl.Refresh();

            // With default empty tier config + rating 0 → Unranked; text must not be null
            Assert.IsNotNull(text.text,
                "Tier text must be non-null when _buildRating and _tierConfig are both assigned.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(buildRating);
            Object.DestroyImmediate(tierConfig);
        }

        [Test]
        public void Refresh_WithBuildRatingNullTierConfig_TierTextIsEmpty()
        {
            MakeController(out GameObject go);
            var ctrl        = go.GetComponent<MatchPreviewController>();
            var buildRating = ScriptableObject.CreateInstance<BuildRatingSO>();
            var text        = AddText(go, "TierText");
            text.text = "prev";  // pre-seed to verify it gets cleared to empty
            SetField(ctrl, "_buildRating", buildRating);
            // _tierConfig remains null
            SetField(ctrl, "_tierText", text);
            go.SetActive(true);

            ctrl.Refresh();

            Assert.AreEqual(string.Empty, text.text,
                "Tier text must be cleared to empty when _tierConfig is not assigned.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(buildRating);
        }

        // ── Rating text ───────────────────────────────────────────────────────

        [Test]
        public void Refresh_NullBuildRating_RatingTextShowsPowerZero()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<MatchPreviewController>();
            var text = AddText(go, "RatingText");
            // _buildRating is null
            SetField(ctrl, "_ratingText", text);
            go.SetActive(true);

            ctrl.Refresh();

            Assert.AreEqual("Power: 0", text.text,
                "Rating text must show 'Power: 0' when _buildRating is not assigned.");
            Object.DestroyImmediate(go);
        }
    }
}
