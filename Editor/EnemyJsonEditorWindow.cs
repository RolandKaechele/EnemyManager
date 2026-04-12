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
    /// Editor window for creating and editing <c>enemies.json</c> and <c>waves.json</c> in StreamingAssets.
    /// Enemies and waves are stored in separate files.
    /// Open via <b>JSON Editors → Enemy Manager</b> or via the Manager Inspector button.
    /// </summary>
    public class EnemyJsonEditorWindow : EditorWindow
    {
        private const string EnemiesFolderName = "enemies";
        private const string WavesFolderName   = "waves";

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
                $"StreamingAssets/{EnemiesFolderName}/ + {WavesFolderName}/",
                EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(50))) Load();
            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50))) Save();
            EditorGUILayout.EndHorizontal();
        }

        private void Load()
        {
            string enemyFolder = Path.Combine(Application.streamingAssetsPath, EnemiesFolderName);
            string wavesFolder = Path.Combine(Application.streamingAssetsPath, WavesFolderName);
            try
            {
                var enemyList = new List<EnemyDefinition>();
                if (Directory.Exists(enemyFolder))
                {
                    foreach (var file in Directory.GetFiles(enemyFolder, "*.json", SearchOption.TopDirectoryOnly))
                    {
                        var ew = JsonUtility.FromJson<EnemyRosterEditorWrapper>(File.ReadAllText(file));
                        if (ew?.enemies != null) enemyList.AddRange(ew.enemies);
                    }
                }
                else
                {
                    Directory.CreateDirectory(enemyFolder);
                    AssetDatabase.Refresh();
                }

                var waveList = new List<WaveDefinition>();
                if (Directory.Exists(wavesFolder))
                {
                    foreach (var file in Directory.GetFiles(wavesFolder, "*.json", SearchOption.TopDirectoryOnly))
                    {
                        var ww = JsonUtility.FromJson<WaveRosterEditorWrapper>(File.ReadAllText(file));
                        if (ww?.waves != null) waveList.AddRange(ww.waves);
                    }
                }
                else
                {
                    Directory.CreateDirectory(wavesFolder);
                    AssetDatabase.Refresh();
                }

                _bridge.enemies = enemyList;
                _bridge.waves   = waveList;
                if (_bridgeEditor != null) { DestroyImmediate(_bridgeEditor); _bridgeEditor = null; }
                _status = $"Loaded {enemyList.Count} enemies from {EnemiesFolderName}/, {waveList.Count} waves from {WavesFolderName}/.";
                _statusError = false;
            }
            catch (Exception e) { _status = $"Load error: {e.Message}"; _statusError = true; }
        }

        private void Save()
        {
            try
            {
                string enemyFolder = Path.Combine(Application.streamingAssetsPath, EnemiesFolderName);
                if (!Directory.Exists(enemyFolder)) Directory.CreateDirectory(enemyFolder);
                int savedEnemies = 0;
                foreach (var entry in _bridge.enemies)
                {
                    if (string.IsNullOrEmpty(entry.id)) continue;
                    var ew = new EnemyRosterEditorWrapper { enemies = new[] { entry } };
                    File.WriteAllText(Path.Combine(enemyFolder, $"{entry.id}.json"), JsonUtility.ToJson(ew, true));
                    savedEnemies++;
                }

                string wavesFolder = Path.Combine(Application.streamingAssetsPath, WavesFolderName);
                if (!Directory.Exists(wavesFolder)) Directory.CreateDirectory(wavesFolder);
                int savedWaves = 0;
                foreach (var entry in _bridge.waves)
                {
                    if (string.IsNullOrEmpty(entry.id)) continue;
                    var ww = new WaveRosterEditorWrapper { waves = new[] { entry } };
                    File.WriteAllText(Path.Combine(wavesFolder, $"{entry.id}.json"), JsonUtility.ToJson(ww, true));
                    savedWaves++;
                }

                AssetDatabase.Refresh();
                _status = $"Saved {savedEnemies} enemy file(s) to {EnemiesFolderName}/, {savedWaves} wave file(s) to {WavesFolderName}/";
                _statusError = false;
            }
            catch (Exception e) { _status = $"Save error: {e.Message}"; _statusError = true; }
        }
    }

    // ── ScriptableObject bridge ──────────────────────────────────────────────
    internal class EnemyRosterEditorBridge : ScriptableObject
    {
        public List<EnemyDefinition> enemies = new List<EnemyDefinition>();
        public List<WaveDefinition>  waves   = new List<WaveDefinition>();
    }

    // ── Local wrappers mirror the internal roster JSON classes ───────────────
    [Serializable]
    internal class EnemyRosterEditorWrapper
    {
        public EnemyDefinition[] enemies = Array.Empty<EnemyDefinition>();
    }

    [Serializable]
    internal class WaveRosterEditorWrapper
    {
        public WaveDefinition[] waves = Array.Empty<WaveDefinition>();
    }
}
#endif
