using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BattleRobots.Editor
{
    /// <summary>
    /// Editor window that scans the open scene(s) for all MonoBehaviours in the
    /// BattleRobots namespaces and lists every serialized UnityEngine.Object
    /// reference that is currently null.
    ///
    /// Open via:  Tools ▶ BattleRobots ▶ Scene Wiring Validator
    ///
    /// ── How it works ─────────────────────────────────────────────────────────
    ///   For each active MonoBehaviour whose namespace starts with "BattleRobots",
    ///   the validator uses <c>SerializedObject</c> + <c>SerializedProperty</c>
    ///   to walk every serialized field in the Inspector.  Any field whose type
    ///   derives from <c>UnityEngine.Object</c> and whose value is null is
    ///   flagged as a potential missing wire.
    ///
    ///   Note: some fields are legitimately optional (e.g. optional AudioEvent
    ///   channels, optional HUD text labels).  The validator reports them all;
    ///   decide per-field whether the null is intentional.
    ///
    /// ── Usage workflow ────────────────────────────────────────────────────────
    ///   1. Open the Arena (or Shop) scene in the Editor.
    ///   2. Open this window (Tools ▶ BattleRobots ▶ Scene Wiring Validator).
    ///   3. Click "Scan Scene".
    ///   4. For each row, click the component name to ping the GameObject in the
    ///      Hierarchy, then assign the missing SO / prefab in the Inspector.
    ///   5. Re-scan until the list is empty (or only legitimately-optional fields
    ///      remain).
    /// </summary>
    public sealed class SceneWiringValidator : EditorWindow
    {
        // ── Data ──────────────────────────────────────────────────────────────

        private sealed class MissingRef
        {
            public MonoBehaviour Component;
            public string        FieldName;
            public string        ComponentTypeName;
            public string        GameObjectPath;
        }

        private readonly List<MissingRef> _issues = new List<MissingRef>();
        private Vector2 _scrollPos;
        private bool    _scanned;

        // ── Menu entry ────────────────────────────────────────────────────────

        [MenuItem("Tools/BattleRobots/Scene Wiring Validator")]
        private static void Open()
        {
            var window = GetWindow<SceneWiringValidator>(
                title: "Scene Wiring Validator",
                focus:  true);
            window.minSize = new Vector2(520f, 300f);
            window.Show();
        }

        // ── GUI ───────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(4f);

            if (GUILayout.Button("Scan Scene", GUILayout.Height(28f)))
                ScanScene();

            if (!_scanned) return;

            EditorGUILayout.Space(6f);

            if (_issues.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No missing references found in BattleRobots components. Scene looks wired correctly.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                $"{_issues.Count} null Object reference(s) found. " +
                "Some may be intentionally optional — review each row.",
                MessageType.Warning);

            EditorGUILayout.Space(4f);
            DrawColumnHeaders();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            string lastType = null;
            foreach (MissingRef issue in _issues)
            {
                // Section header per component type
                if (issue.ComponentTypeName != lastType)
                {
                    EditorGUILayout.Space(4f);
                    EditorGUILayout.LabelField(issue.ComponentTypeName, EditorStyles.boldLabel);
                    lastType = issue.ComponentTypeName;
                }

                DrawIssueRow(issue);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6f);
            if (GUILayout.Button("Copy Report to Clipboard"))
                CopyReport();
        }

        // ── Scan logic ────────────────────────────────────────────────────────

        private void ScanScene()
        {
            _issues.Clear();
            _scanned = true;

#if UNITY_2023_1_OR_NEWER
            MonoBehaviour[] all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
#else
            MonoBehaviour[] all = Object.FindObjectsOfType<MonoBehaviour>();
#endif

            foreach (MonoBehaviour mb in all)
            {
                if (mb == null) continue;

                string ns = mb.GetType().Namespace ?? string.Empty;
                if (!ns.StartsWith("BattleRobots", StringComparison.Ordinal)) continue;

                var so   = new SerializedObject(mb);
                var iter = so.GetIterator();
                bool enterChildren = true;

                while (iter.NextVisible(enterChildren))
                {
                    enterChildren = iter.propertyType != SerializedPropertyType.String;

                    if (iter.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (iter.objectReferenceValue != null) continue;
                    // Skip the m_Script property (always null for non-missing scripts).
                    if (iter.name == "m_Script") continue;

                    _issues.Add(new MissingRef
                    {
                        Component         = mb,
                        FieldName         = iter.displayName,
                        ComponentTypeName = mb.GetType().Name,
                        GameObjectPath    = GetGameObjectPath(mb.gameObject),
                    });
                }
            }

            // Sort: component type first, then GameObject path, then field name.
            _issues.Sort((a, b) =>
            {
                int cmp = string.Compare(a.ComponentTypeName, b.ComponentTypeName,
                                         StringComparison.Ordinal);
                if (cmp != 0) return cmp;
                cmp = string.Compare(a.GameObjectPath, b.GameObjectPath,
                                     StringComparison.Ordinal);
                return cmp != 0 ? cmp : string.Compare(a.FieldName, b.FieldName,
                                                        StringComparison.Ordinal);
            });

            Repaint();
        }

        // ── Drawing helpers ───────────────────────────────────────────────────

        private static void DrawHeader()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                "BattleRobots — Scene Wiring Validator",
                EditorStyles.largeLabel);
            EditorGUILayout.LabelField(
                "Scans all BattleRobots MonoBehaviours in the open scene for null " +
                "serialized Object references.",
                EditorStyles.wordWrappedMiniLabel);
        }

        private static void DrawColumnHeaders()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("GameObject",       EditorStyles.toolbarButton, GUILayout.Width(200f));
            EditorGUILayout.LabelField("Missing Field",    EditorStyles.toolbarButton, GUILayout.Width(160f));
            EditorGUILayout.LabelField("Select",           EditorStyles.toolbarButton, GUILayout.Width(60f));
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawIssueRow(MissingRef issue)
        {
            EditorGUILayout.BeginHorizontal();

            // Truncate long paths on the left.
            string displayPath = issue.GameObjectPath.Length > 34
                ? "…" + issue.GameObjectPath.Substring(issue.GameObjectPath.Length - 33)
                : issue.GameObjectPath;

            EditorGUILayout.LabelField(
                new GUIContent(displayPath, issue.GameObjectPath),
                GUILayout.Width(200f));

            EditorGUILayout.LabelField(
                new GUIContent(issue.FieldName),
                GUILayout.Width(160f));

            if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(60f)))
            {
                Selection.activeGameObject = issue.Component.gameObject;
                EditorGUIUtility.PingObject(issue.Component.gameObject);
            }

            EditorGUILayout.EndHorizontal();
        }

        // ── Utilities ─────────────────────────────────────────────────────────

        private static string GetGameObjectPath(GameObject go)
        {
            var sb  = new StringBuilder(go.name);
            var cur = go.transform.parent;
            while (cur != null)
            {
                sb.Insert(0, cur.name + "/");
                cur = cur.parent;
            }
            return sb.ToString();
        }

        private void CopyReport()
        {
            if (_issues.Count == 0)
            {
                EditorGUIUtility.systemCopyBuffer = "No missing references found.";
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"BattleRobots Scene Wiring Report — {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"Missing references: {_issues.Count}");
            sb.AppendLine();

            string lastType = null;
            foreach (MissingRef issue in _issues)
            {
                if (issue.ComponentTypeName != lastType)
                {
                    sb.AppendLine($"[{issue.ComponentTypeName}]");
                    lastType = issue.ComponentTypeName;
                }
                sb.AppendLine($"  {issue.GameObjectPath}  →  {issue.FieldName}");
            }

            EditorGUIUtility.systemCopyBuffer = sb.ToString();
            Debug.Log("[SceneWiringValidator] Report copied to clipboard.");
        }
    }
}
