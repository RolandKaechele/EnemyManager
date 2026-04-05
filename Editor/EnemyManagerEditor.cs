#if UNITY_EDITOR
using EnemyManager.Runtime;
using UnityEditor;
using UnityEngine;

namespace EnemyManager.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="EnemyManager.Runtime.EnemyManager"/>.
    /// Shows live instances, wave controls, and spawn/defeat tools at runtime.
    /// </summary>
    [CustomEditor(typeof(EnemyManager.Runtime.EnemyManager))]
    public class EnemyManagerEditor : UnityEditor.Editor
    {
        private string _spawnId = "";
        private string _defeatId = "";
        private string _waveId = "";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(6);

            var loadFromJsonProp = serializedObject.FindProperty("loadFromJson");
            if (loadFromJsonProp != null && !loadFromJsonProp.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "JSON loading is disabled. Enable 'Load From Json' to support modded enemy definitions.",
                    MessageType.Info);
            }

            if (!Application.isPlaying) return;

            var mgr = (EnemyManager.Runtime.EnemyManager)target;

            EditorGUILayout.Space(4);

            // ── Status ──────────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Spawning Paused", mgr.IsSpawningPaused ? "YES" : "no");
            EditorGUILayout.LabelField("Active Wave",     mgr.ActiveWaveId ?? "(none)");
            EditorGUILayout.LabelField("Live Enemies",    mgr.ActiveEnemyCount.ToString());

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Pause Spawning"))  mgr.PauseSpawning();
            if (GUILayout.Button("Resume Spawning")) mgr.ResumeSpawning();
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Abort Wave")) mgr.AbortWave();

            // ── Spawn a test enemy ───────────────────────────────────────────────
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Test Spawn", EditorStyles.miniBoldLabel);
            _spawnId = EditorGUILayout.TextField("Enemy Id", _spawnId);
            if (GUILayout.Button("Spawn at origin"))
                mgr.SpawnEnemy(_spawnId, Vector3.zero);

            // ── Start a test wave ────────────────────────────────────────────────
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Test Wave", EditorStyles.miniBoldLabel);
            _waveId = EditorGUILayout.TextField("Wave Id", _waveId);
            if (GUILayout.Button("Start Wave")) mgr.StartWave(_waveId);

            // ── Manual defeat ────────────────────────────────────────────────────
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Manual Defeat", EditorStyles.miniBoldLabel);
            _defeatId = EditorGUILayout.TextField("Instance Id", _defeatId);
            if (GUILayout.Button("Defeat Instance")) mgr.DefeatEnemy(_defeatId);

            // ── Live instances ───────────────────────────────────────────────────
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Live Instances", EditorStyles.miniBoldLabel);
            foreach (var kv in mgr.LiveInstances)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  {kv.Value.instanceId}  ({kv.Value.enemyId})");
                if (GUILayout.Button("Defeat", GUILayout.Width(60)))
                    mgr.DefeatEnemy(kv.Value.instanceId);
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
#endif
