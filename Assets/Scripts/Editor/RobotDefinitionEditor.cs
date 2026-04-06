using UnityEditor;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Editor
{
    /// <summary>
    /// Custom Inspector for RobotDefinition.
    /// Draws all default fields then appends a validation banner
    /// so designers can see slot errors without entering Play mode.
    /// </summary>
    [CustomEditor(typeof(RobotDefinition))]
    public sealed class RobotDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Slot Validation", EditorStyles.boldLabel);

            var def = (RobotDefinition)target;

            if (def.ValidateSlots(out string error))
            {
                EditorGUILayout.HelpBox(
                    $"OK — {def.Slots.Count} slot(s) configured, all IDs unique.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }
        }
    }
}
