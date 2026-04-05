using UnityEditor;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="ArenaConfig"/>.
    /// Adds a Validate button that checks spawn-point count and co-location.
    /// </summary>
    [CustomEditor(typeof(ArenaConfig))]
    public sealed class ArenaConfigEditor : UnityEditor.Editor
    {
        private string _validationMessage = string.Empty;
        private bool   _lastResultValid   = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Arena Validation", EditorStyles.boldLabel);

            if (GUILayout.Button("Validate Arena Config"))
            {
                var config = (ArenaConfig)target;
                _lastResultValid = config.Validate(out _validationMessage);
                if (_lastResultValid)
                    _validationMessage = $"Config OK — {config.SpawnPoints.Count} spawn point(s) valid.";
            }

            if (!string.IsNullOrEmpty(_validationMessage))
            {
                var msgType = _lastResultValid ? MessageType.Info : MessageType.Error;
                EditorGUILayout.HelpBox(_validationMessage, msgType);
            }
        }
    }
}
