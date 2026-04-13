using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using ProfileIssue = BattleRobots.Core.OpponentDamageProfileAnalyser.ProfileIssue;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="OpponentDamageProfileAnalyser.Analyse"/>.
    ///
    /// Covers:
    ///   Null / empty guards:
    ///   • Null input list returns empty (non-null) issue list.
    ///   • Empty input list returns empty issue list.
    ///   • Null entries within the list are skipped silently.
    ///
    ///   Fully-configured profiles:
    ///   • Profile with both configs assigned produces no issue.
    ///   • All-configured list returns zero issues.
    ///
    ///   Missing-config detection:
    ///   • Profile missing only resistance → single issue with MissingResistance=true.
    ///   • Profile missing only vulnerability → single issue with MissingVulnerability=true.
    ///   • Profile missing both → single issue with both flags true.
    ///   • Mixed list (some configured, some not) → issues only for unconfigured profiles.
    ///
    ///   Issue data:
    ///   • Issue exposes the correct Profile reference.
    ///   • Correct flag values for MissingResistance-only case.
    ///   • Correct flag values for MissingVulnerability-only case.
    ///   • Three-profile mixed list yields exactly two issues.
    /// </summary>
    public class OpponentDamageProfileAnalyserTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic |
                                BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static OpponentProfileSO CreateProfile(
            DamageResistanceConfig resistance       = null,
            DamageVulnerabilityConfig vulnerability = null,
            string displayName                      = "TestOpponent")
        {
            var profile = ScriptableObject.CreateInstance<OpponentProfileSO>();
            SetField(profile, "_displayName",              displayName);
            SetField(profile, "_damageResistanceConfig",   resistance);
            SetField(profile, "_damageVulnerabilityConfig", vulnerability);
            return profile;
        }

        // ── Null / empty guards ───────────────────────────────────────────────

        [Test]
        public void Analyse_NullList_ReturnsEmptyList()
        {
            List<ProfileIssue> result = OpponentDamageProfileAnalyser.Analyse(null);

            Assert.IsNotNull(result, "Result must be non-null even for null input.");
            Assert.AreEqual(0, result.Count, "Null input must produce an empty issue list.");
        }

        [Test]
        public void Analyse_EmptyList_ReturnsEmptyList()
        {
            var profiles = new List<OpponentProfileSO>();
            List<ProfileIssue> result = OpponentDamageProfileAnalyser.Analyse(profiles);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count, "Empty input must produce an empty issue list.");
        }

        [Test]
        public void Analyse_NullEntry_SkippedSilently_NoIssue()
        {
            var profiles = new List<OpponentProfileSO> { null };
            List<ProfileIssue> result = OpponentDamageProfileAnalyser.Analyse(profiles);

            Assert.AreEqual(0, result.Count,
                "Null entry in the list must be skipped — no issue generated.");
        }

        // ── Fully-configured profiles ─────────────────────────────────────────

        [Test]
        public void Analyse_BothConfigsAssigned_NoIssue()
        {
            var resist = ScriptableObject.CreateInstance<DamageResistanceConfig>();
            var vuln   = ScriptableObject.CreateInstance<DamageVulnerabilityConfig>();
            var profile = CreateProfile(resistance: resist, vulnerability: vuln);

            List<ProfileIssue> result = OpponentDamageProfileAnalyser.Analyse(
                new List<OpponentProfileSO> { profile });

            Assert.AreEqual(0, result.Count,
                "Fully-configured profile must produce no issue.");

            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(resist);
            Object.DestroyImmediate(vuln);
        }

        [Test]
        public void Analyse_AllConfigured_IssueCount_Zero()
        {
            var resist = ScriptableObject.CreateInstance<DamageResistanceConfig>();
            var vuln   = ScriptableObject.CreateInstance<DamageVulnerabilityConfig>();
            var p1 = CreateProfile(resist, vuln, "Opp1");
            var p2 = CreateProfile(resist, vuln, "Opp2");
            var p3 = CreateProfile(resist, vuln, "Opp3");

            List<ProfileIssue> result = OpponentDamageProfileAnalyser.Analyse(
                new List<OpponentProfileSO> { p1, p2, p3 });

            Assert.AreEqual(0, result.Count,
                "All-configured list must produce zero issues.");

            Object.DestroyImmediate(p1);
            Object.DestroyImmediate(p2);
            Object.DestroyImmediate(p3);
            Object.DestroyImmediate(resist);
            Object.DestroyImmediate(vuln);
        }

        // ── Missing-config detection ──────────────────────────────────────────

        [Test]
        public void Analyse_MissingResistance_SingleIssue()
        {
            var vuln    = ScriptableObject.CreateInstance<DamageVulnerabilityConfig>();
            var profile = CreateProfile(resistance: null, vulnerability: vuln);

            List<ProfileIssue> result = OpponentDamageProfileAnalyser.Analyse(
                new List<OpponentProfileSO> { profile });

            Assert.AreEqual(1, result.Count,
                "Profile missing resistance must generate exactly one issue.");

            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(vuln);
        }

        [Test]
        public void Analyse_MissingResistance_MissingResistanceFlag_True()
        {
            var vuln    = ScriptableObject.CreateInstance<DamageVulnerabilityConfig>();
            var profile = CreateProfile(resistance: null, vulnerability: vuln);

            List<ProfileIssue> result = OpponentDamageProfileAnalyser.Analyse(
                new List<OpponentProfileSO> { profile });

            Assert.IsTrue(result[0].MissingResistance,
                "MissingResistance flag must be true when resistance config is null.");

            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(vuln);
        }

        [Test]
        public void Analyse_MissingResistance_MissingVulnerabilityFlag_False()
        {
            var vuln    = ScriptableObject.CreateInstance<DamageVulnerabilityConfig>();
            var profile = CreateProfile(resistance: null, vulnerability: vuln);

            List<ProfileIssue> result = OpponentDamageProfileAnalyser.Analyse(
                new List<OpponentProfileSO> { profile });

            Assert.IsFalse(result[0].MissingVulnerability,
                "MissingVulnerability flag must be false when vulnerability config is assigned.");

            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(vuln);
        }

        [Test]
        public void Analyse_MissingVulnerability_SingleIssue()
        {
            var resist  = ScriptableObject.CreateInstance<DamageResistanceConfig>();
            var profile = CreateProfile(resistance: resist, vulnerability: null);

            List<ProfileIssue> result = OpponentDamageProfileAnalyser.Analyse(
                new List<OpponentProfileSO> { profile });

            Assert.AreEqual(1, result.Count,
                "Profile missing vulnerability must generate exactly one issue.");

            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(resist);
        }

        [Test]
        public void Analyse_BothMissing_BothFlagsTrue()
        {
            var profile = CreateProfile(resistance: null, vulnerability: null);

            List<ProfileIssue> result = OpponentDamageProfileAnalyser.Analyse(
                new List<OpponentProfileSO> { profile });

            Assert.AreEqual(1, result.Count,
                "Profile missing both configs must generate exactly one issue.");
            Assert.IsTrue(result[0].MissingResistance,
                "MissingResistance must be true when resistance is null.");
            Assert.IsTrue(result[0].MissingVulnerability,
                "MissingVulnerability must be true when vulnerability is null.");

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void Analyse_MixedProfiles_OnlyUnconfiguredIssued()
        {
            var resist  = ScriptableObject.CreateInstance<DamageResistanceConfig>();
            var vuln    = ScriptableObject.CreateInstance<DamageVulnerabilityConfig>();
            var good    = CreateProfile(resist, vuln, "Good");
            var bad     = CreateProfile(null,   null, "Bad");

            List<ProfileIssue> result = OpponentDamageProfileAnalyser.Analyse(
                new List<OpponentProfileSO> { good, bad });

            Assert.AreEqual(1, result.Count,
                "Only the unconfigured profile must produce an issue.");
            Assert.AreSame(bad, result[0].Profile,
                "The issue must reference the unconfigured profile.");

            Object.DestroyImmediate(good);
            Object.DestroyImmediate(bad);
            Object.DestroyImmediate(resist);
            Object.DestroyImmediate(vuln);
        }

        // ── Issue data ────────────────────────────────────────────────────────

        [Test]
        public void Analyse_IssueExposes_CorrectProfile()
        {
            var profile = CreateProfile(null, null, "SpecificOpponent");

            List<ProfileIssue> result = OpponentDamageProfileAnalyser.Analyse(
                new List<OpponentProfileSO> { profile });

            Assert.AreSame(profile, result[0].Profile,
                "The issue's Profile property must reference the analysed SO.");

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void Analyse_ThreeProfiles_TwoIssues_CountTwo()
        {
            var resist  = ScriptableObject.CreateInstance<DamageResistanceConfig>();
            var vuln    = ScriptableObject.CreateInstance<DamageVulnerabilityConfig>();
            var good    = CreateProfile(resist, vuln, "Good");
            var bad1    = CreateProfile(null,   null, "Bad1");
            var bad2    = CreateProfile(null,   null, "Bad2");

            List<ProfileIssue> result = OpponentDamageProfileAnalyser.Analyse(
                new List<OpponentProfileSO> { good, bad1, bad2 });

            Assert.AreEqual(2, result.Count,
                "Two unconfigured profiles in a three-profile list must produce two issues.");

            Object.DestroyImmediate(good);
            Object.DestroyImmediate(bad1);
            Object.DestroyImmediate(bad2);
            Object.DestroyImmediate(resist);
            Object.DestroyImmediate(vuln);
        }
    }
}
