using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using BattleRobots.Core;
using ProfileIssue = BattleRobots.Core.OpponentDamageProfileAnalyser.ProfileIssue;

namespace BattleRobots.Editor
{
    /// <summary>
    /// Editor window that scans all <see cref="OpponentProfileSO"/> assets in the
    /// project and reports any that are missing a <see cref="DamageResistanceConfig"/>
    /// or <see cref="DamageVulnerabilityConfig"/>, surfacing under-configured opponents
    /// before they cause silent type-matchup gaps at runtime.
    ///
    /// Open via:  Tools ▶ BattleRobots ▶ Opponent Damage Profile Validator
    ///
    /// ── How it works ─────────────────────────────────────────────────────────────
    ///   Clicking "Scan Project" calls
    ///   <see cref="AssetDatabase.FindAssets"/> with <c>"t:OpponentProfileSO"</c>,
    ///   loads each hit via <see cref="AssetDatabase.LoadAssetAtPath{T}"/>, and
    ///   delegates analysis to <see cref="OpponentDamageProfileAnalyser.Analyse"/> —
    ///   a Core-namespace static method that is unit-tested independently of this window.
    ///
    /// ── Issue rows ───────────────────────────────────────────────────────────────
    ///   Each row shows the profile name, which config(s) are missing, and a
    ///   "Select" button that pings the asset in the Project window so designers
    ///   can assign configs immediately.
    /// </summary>
    public sealed class OpponentDamageProfileValidator : EditorWindow
    {
        // ── State ─────────────────────────────────────────────────────────────

        private List<ProfileIssue> _issues = new List<ProfileIssue>();
        private int    _scannedCount;
        private bool   _scanned;
        private Vector2 _scrollPos;

        // ── Menu entry ────────────────────────────────────────────────────────

        [MenuItem("Tools/BattleRobots/Opponent Damage Profile Validator")]
        private static void Open()
        {
            var window = GetWindow<OpponentDamageProfileValidator>(
                title: "Damage Profile Validator",
                focus:  true);
            window.minSize = new Vector2(500f, 300f);
            window.Show();
        }

        // ── GUI ───────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(4f);

            if (GUILayout.Button("Scan Project", GUILayout.Height(28f)))
                ScanProject();

            if (!_scanned) return;

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                $"Scanned: {_scannedCount} opponent profile(s).  Issues: {_issues.Count}",
                _issues.Count > 0 ? EditorStyles.boldLabel : EditorStyles.label);

            if (_issues.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "All opponent profiles have both DamageResistanceConfig and " +
                    "DamageVulnerabilityConfig assigned.  No issues found.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                $"{_issues.Count} profile(s) are missing damage config(s). " +
                "Assign the missing SOs to ensure accurate type-matchup hints in advisors.",
                MessageType.Warning);

            EditorGUILayout.Space(4f);
            DrawColumnHeaders();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            foreach (ProfileIssue issue in _issues)
                DrawIssueRow(issue);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6f);
            if (GUILayout.Button("Copy Report to Clipboard"))
                CopyReport();
        }

        // ── Scan ──────────────────────────────────────────────────────────────

        private void ScanProject()
        {
            _issues.Clear();
            _scanned = true;

            string[] guids   = AssetDatabase.FindAssets("t:OpponentProfileSO");
            var profiles     = new List<OpponentProfileSO>(guids.Length);

            foreach (string guid in guids)
            {
                string path    = AssetDatabase.GUIDToAssetPath(guid);
                var    profile = AssetDatabase.LoadAssetAtPath<OpponentProfileSO>(path);
                if (profile != null) profiles.Add(profile);
            }

            _scannedCount = profiles.Count;
            _issues       = OpponentDamageProfileAnalyser.Analyse(profiles);
            Repaint();
        }

        // ── Drawing helpers ───────────────────────────────────────────────────

        private static void DrawHeader()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                "BattleRobots — Opponent Damage Profile Validator",
                EditorStyles.largeLabel);
            EditorGUILayout.LabelField(
                "Finds OpponentProfileSO assets that are missing a DamageResistanceConfig " +
                "or DamageVulnerabilityConfig, surfacing under-configured opponents for designers.",
                EditorStyles.wordWrappedMiniLabel);
        }

        private static void DrawColumnHeaders()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Opponent Profile", EditorStyles.toolbarButton, GUILayout.Width(200f));
            EditorGUILayout.LabelField("Missing Configs",  EditorStyles.toolbarButton, GUILayout.Width(180f));
            EditorGUILayout.LabelField("Select",           EditorStyles.toolbarButton, GUILayout.Width(60f));
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawIssueRow(ProfileIssue issue)
        {
            EditorGUILayout.BeginHorizontal();

            string profileName = issue.Profile != null ? issue.Profile.name : "(null)";
            EditorGUILayout.LabelField(profileName, GUILayout.Width(200f));

            var missing = new StringBuilder();
            if (issue.MissingResistance)    missing.Append("Resistance ");
            if (issue.MissingVulnerability) missing.Append("Vulnerability");
            EditorGUILayout.LabelField(missing.ToString().Trim(), GUILayout.Width(180f));

            if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(60f))
                && issue.Profile != null)
            {
                Selection.activeObject = issue.Profile;
                EditorGUIUtility.PingObject(issue.Profile);
            }

            EditorGUILayout.EndHorizontal();
        }

        // ── Utilities ─────────────────────────────────────────────────────────

        private void CopyReport()
        {
            if (_issues.Count == 0)
            {
                EditorGUIUtility.systemCopyBuffer = "No issues found.";
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine(
                $"BattleRobots Damage Profile Report — {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"Scanned: {_scannedCount}  Issues: {_issues.Count}");
            sb.AppendLine();

            foreach (ProfileIssue issue in _issues)
            {
                var missing = new StringBuilder();
                if (issue.MissingResistance)    missing.Append("Resistance ");
                if (issue.MissingVulnerability) missing.Append("Vulnerability");
                sb.AppendLine($"  {issue.Profile?.name ?? "(null)"}  →  {missing.ToString().Trim()}");
            }

            EditorGUIUtility.systemCopyBuffer = sb.ToString();
            Debug.Log("[OpponentDamageProfileValidator] Report copied to clipboard.");
        }
    }
}
