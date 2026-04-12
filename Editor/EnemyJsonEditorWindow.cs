#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using EnemyManager.Runtime;
using UnityEditor;
using UnityEngine;

namespace EnemyManager.Editor
{
    // ────────────────────────────────────────────────────────────────────────────
    // Enemy Roster JSON Editor Window
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Editor window for creating and editing <c>enemies.json</c> in StreamingAssets.
    /// Contains both enemy definitions and wave configurations.
    /// Open via <b>JSON Editors → Enemy Manager</b> or via the Manager Inspector button.
    /// </summary>
    public class EnemyJsonEditorWindow : EditorWindow
    {
        private const string JsonFileName = "enemies.json";

        private EnemyRosterEditorBridge  _bridge;
        private UnityEditor.Editor       _bridgeEditor;
        private Vector2                  _scroll;
        private string                   _status;
        private bool                     _statusError;

        [MenuItem("JSON Editors/Enemy Manager")]
        public static void ShowWindow() =>
            GetWindow<EnemyJsonEditorWindow>("Enemy Roster JSON");

        private void OnEnable()
        {
            _bridge = CreateInstance<EnemyRosterEditorBridge>();
            Load();
        }

        private void OnDisable()
        {
            if (_bridgeEditor != null) DestroyImmediate(_bridgeEditor);
            if (_bridge      != null) DestroyImmediate(_bridge);
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, _statusError ? MessageType.Error : MessageType.Info);

            if (_bridge == null) return;
            if (_bridgeEditor == null)
                _bridgeEditor = UnityEditor.Editor.CreateEditor(_bridge);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _bridgeEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(
                Path.Combine("StreamingAssets", JsonFileName),
                EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(50))) Load();
            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50))) Save();
            EditorGUILayout.EndHorizontal();
        }

        private void Load()
        {
            var path = Path.Combine(Application.streamingAssetsPath, JsonFileName);
            try
            {
                if (!File.Exists(path))
                {
                    File.WriteAllText(path, JsonUtility.ToJson(new EnemyRosterEditorWrapper(), true));
                    AssetDatabase.Refresh();
                }

                var w = JsonUtility.FromJson<EnemyRosterEditorWrapper>(File.ReadAllText(path));
                _bridge.enemies = new List<EnemyDefinition>(w.enemies ?? Array.Empty<EnemyDefinition>());
                _bridge.waves   = new List<WaveDefinition>(w.waves   ?? Array.Empty<WaveDefinition>());

                if (_bridgeEditor != null) { DestroyImmediate(_bridgeEditor); _bridgeEditor = null; }

                _status     = $"Loaded {_bridge.enemies.Count} enemies and {_bridge.waves.Count} waves.";
                _statusError = false;
            }
            catch (Exception e)
            {
                _status     = $"Load error: {e.Message}";
                _statusError = true;
            }
        }

        private void Save()
        {
            try
            {
                var w = new EnemyRosterEditorWrapper
                {
                    enemies = _bridge.enemies.ToArray(),
                    waves   = _bridge.waves.ToArray()
                };
                var path = Path.Combine(Application.streamingAssetsPath, JsonFileName);
                File.WriteAllText(path, JsonUtility.ToJson(w, true));
                AssetDatabase.Refresh();
                _status     = $"Saved {_bridge.enemies.Count} enemies and {_bridge.waves.Count} waves to {JsonFileName}.";
                _statusError = false;
            }
            catch (Exception e)
            {
                _status     = $"Save error: {e.Message}";
                _statusError = true;
            }
        }
    }

    // ── ScriptableObject bridge ──────────────────────────────────────────────
    internal class EnemyRosterEditorBridge : ScriptableObject
    {
        public List<EnemyDefinition> enemies = new List<EnemyDefinition>();
        public List<WaveDefinition>  waves   = new List<WaveDefinition>();
    }

    // ── Local wrapper mirrors the internal EnemyRosterJson ───────────────────
    [Serializable]
    internal class EnemyRosterEditorWrapper
    {
        public EnemyDefinition[] enemies = Array.Empty<EnemyDefinition>();
        public WaveDefinition[]  waves   = Array.Empty<WaveDefinition>();
    }
}
#endif
