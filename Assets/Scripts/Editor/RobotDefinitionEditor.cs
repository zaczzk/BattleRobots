using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="RobotDefinition"/>.
    /// Draws validation warnings directly beneath the slot list so designers
    /// catch data errors before entering play mode.
    /// </summary>
    [CustomEditor(typeof(RobotDefinition))]
    public sealed class RobotDefinitionEditor : UnityEditor.Editor
    {
        // Cached styles — created once, reused across OnInspectorGUI calls.
        private GUIStyle _errorStyle;
        private GUIStyle _warnStyle;
        private GUIStyle _okStyle;

        public override void OnInspectorGUI()
        {
            // Always draw the default inspector first so fields remain editable.
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Slot Validation", EditorStyles.boldLabel);

            var definition = (RobotDefinition)target;
            bool isValid = definition.Validate(out List<string> errors);

            EnsureStyles();

            if (isValid)
            {
                EditorGUILayout.LabelField(
                    $"✓ {definition.PartSlots.Count} slot(s) — no issues found.",
                    _okStyle);
            }
            else
            {
                foreach (string error in errors)
                {
                    // Distinguish hard errors from warnings by prefix convention.
                    bool isWarning = error.StartsWith("Slot count") || error.StartsWith("Missing mandatory");
                    EditorGUILayout.HelpBox(error, isWarning ? MessageType.Warning : MessageType.Error);
                }
            }

            // Convenience button: auto-populate slot IDs based on slot type name.
            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Auto-generate missing Slot IDs"))
            {
                AutoGenerateSlotIds(definition);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void EnsureStyles()
        {
            if (_okStyle != null) return;

            _okStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.2f, 0.7f, 0.2f) },
                fontStyle = FontStyle.Bold,
            };
            _errorStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.9f, 0.2f, 0.2f) },
                fontStyle = FontStyle.Bold,
            };
            _warnStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.9f, 0.7f, 0.1f) },
                fontStyle = FontStyle.Bold,
            };
        }

        /// <summary>
        /// Uses SerializedProperty to safely set empty SlotId fields to a
        /// generated value based on the slot type, maintaining Undo support.
        /// </summary>
        private void AutoGenerateSlotIds(RobotDefinition definition)
        {
            SerializedObject so = new SerializedObject(definition);
            so.Update();

            SerializedProperty slotsProp = so.FindProperty("_partSlots");
            if (slotsProp == null || !slotsProp.isArray) return;

            bool changed = false;
            for (int i = 0; i < slotsProp.arraySize; i++)
            {
                SerializedProperty slotProp  = slotsProp.GetArrayElementAtIndex(i);
                SerializedProperty idProp    = slotProp.FindPropertyRelative("_slotId");
                SerializedProperty typeProp  = slotProp.FindPropertyRelative("_slotType");

                if (idProp == null || typeProp == null) continue;
                if (!string.IsNullOrWhiteSpace(idProp.stringValue)) continue;

                // Generate: "slot_<typename_lower>_<index>"
                string typeName = ((PartSlotType)typeProp.enumValueIndex).ToString().ToLowerInvariant();
                idProp.stringValue = $"slot_{typeName}_{i}";
                changed = true;
            }

            if (changed)
            {
                so.ApplyModifiedProperties();
                Debug.Log("[RobotDefinitionEditor] Auto-generated missing Slot IDs.");
            }
            else
            {
                Debug.Log("[RobotDefinitionEditor] All slots already have IDs — nothing changed.");
            }
        }
    }
}
