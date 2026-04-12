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
        private const string EnemiesFolderName   = "enemies";
        private const string EnemiesSaveFileName  = "enemies.json";
        private const string WavesFolderName      = "waves";
        private const string WavesSaveFileName    = "waves.json";

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
                    File.WriteAllText(Path.Combine(enemyFolder, EnemiesSaveFileName), JsonUtility.ToJson(new EnemyRosterEditorWrapper(), true));
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
                    File.WriteAllText(Path.Combine(wavesFolder, WavesSaveFileName), JsonUtility.ToJson(new WaveRosterEditorWrapper(), true));
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
                var ew = new EnemyRosterEditorWrapper { enemies = _bridge.enemies.ToArray() };
                File.WriteAllText(Path.Combine(enemyFolder, EnemiesSaveFileName), JsonUtility.ToJson(ew, true));

                string wavesFolder = Path.Combine(Application.streamingAssetsPath, WavesFolderName);
                if (!Directory.Exists(wavesFolder)) Directory.CreateDirectory(wavesFolder);
                var ww = new WaveRosterEditorWrapper { waves = _bridge.waves.ToArray() };
                File.WriteAllText(Path.Combine(wavesFolder, WavesSaveFileName), JsonUtility.ToJson(ww, true));

                AssetDatabase.Refresh();
                _status = $"Saved {_bridge.enemies.Count} enemies to {EnemiesFolderName}/{EnemiesSaveFileName}, {_bridge.waves.Count} waves to {WavesFolderName}/{WavesSaveFileName}.";
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
