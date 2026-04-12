#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using EnemyManager.Runtime;
using UnityEditor;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif

namespace EnemyManager.Editor
{
    // ODIN Inspector: when ODIN_INSPECTOR is defined the manager extends
    // SerializedMonoBehaviour. The custom editor must derive from OdinEditor
    // so ODIN's full property tree (including OdinSerialize fields) is drawn.
    // Without ODIN the standard UnityEditor.Editor base is used.
#if ODIN_INSPECTOR
    [CustomEditor(typeof(EnemyManager.Runtime.EnemyManager))]
    public class EnemyManagerEditor : OdinEditor
#else
    [CustomEditor(typeof(EnemyManager.Runtime.EnemyManager))]
    public class EnemyManagerEditor : UnityEditor.Editor
#endif
    {
        private string _spawnId = "";
        private string _defeatId = "";
        private string _waveId = "";

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI() renders the full ODIN property tree when
            // inheriting from OdinEditor, or the standard Unity inspector otherwise.
            base.OnInspectorGUI();

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Open JSON Editor")) EnemyJsonEditorWindow.ShowWindow();

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

    // ── Prefab generation ──────────────────────────────────────────────────────

    [Serializable]
    internal class EnemyRosterProxy
    {
        public List<EnemyDefinition> enemies = new List<EnemyDefinition>();
        public List<WaveDefinition>  waves   = new List<WaveDefinition>();
    }

    /// <summary>
    /// Generates enemy prefabs from enemies.json. Lives in EnemyManager.Editor so it
    /// has direct access to all runtime types without a separate assembly dependency.
    /// </summary>
    internal static class EnemyPrefabHelper
    {
        internal const string EnemiesJson = "Assets/StreamingAssets/enemies.json";
        private  const string OutDir      = "Assets/Resources/Prefabs/Enemies";

        [MenuItem("Generate Prefabs/Enemies", priority = 101)]
        public static void GenerateEnemyPrefabs()
        {
            var path = Path.Combine(Application.dataPath, "StreamingAssets", "enemies.json");
            if (!File.Exists(path)) { Debug.LogError("[EnemyPrefabHelper] enemies.json not found: " + path); return; }

            var roster = JsonUtility.FromJson<EnemyRosterProxy>(File.ReadAllText(path));
            if (roster?.enemies == null || roster.enemies.Count == 0)
            { Debug.LogWarning("[EnemyPrefabHelper] enemies.json contained no entries."); return; }

            EnsureDirectory(OutDir);
            EnsureTag("Enemy");
            EnsureLayer("Enemies");

            int n = 0;
            foreach (var def in roster.enemies)
            {
                if (string.IsNullOrEmpty(def.id)) continue;
                var go = BuildEnemyGo(def);
                SavePrefab(go, $"{OutDir}/{def.id}.prefab");
                UnityEngine.Object.DestroyImmediate(go);
                n++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[EnemyPrefabHelper] Generated {n} enemy prefabs \u2192 {OutDir}");
        }

        private static GameObject BuildEnemyGo(EnemyDefinition def)
        {
            var go = new GameObject(def.id);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = def.type == EnemyType.Boss ? 1 : 0;
            var sp = Resources.Load<Sprite>($"Enemies/{def.id}_sprite");
            if (sp != null) sr.sprite = sp;

            var anim = go.AddComponent<Animator>();
            var ctrl = Resources.Load<RuntimeAnimatorController>($"Animators/{def.id}_controller");
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;

            go.AddComponent<CapsuleCollider2D>().size = new Vector2(0.8f, 1.2f);

            var det = go.AddComponent<CircleCollider2D>();
            det.isTrigger = true;
            det.radius = def.type switch
            {
                EnemyType.Boss  => 4.0f,
                EnemyType.Elite => 3.0f,
                EnemyType.Swarm => 1.5f,
                _               => 2.5f,
            };

            go.AddComponent<AudioSource>().playOnAwake = false;

            go.tag = "Enemy";
            int layer = LayerMask.NameToLayer("Enemies");
            if (layer >= 0) go.layer = layer;

            if (def.type == EnemyType.Boss && def.bossPhases > 1)
            {
                for (int p = 1; p <= def.bossPhases; p++)
                {
                    var phase = new GameObject($"Phase_{p}");
                    phase.transform.SetParent(go.transform, false);
                    phase.AddComponent<SpriteRenderer>().sortingOrder = 2;
                    phase.AddComponent<Animator>();
                    phase.SetActive(p == 1);
                }
            }

            return go;
        }

        internal static void SavePrefab(GameObject go, string assetPath)
        {
            bool ex = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null;
            PrefabUtility.SaveAsPrefabAsset(go, assetPath, out bool ok);
            if (!ok) Debug.LogWarning($"[PrefabGen] \u2717 {assetPath}");
            else     Debug.Log(ex ? $"[PrefabGen] \u21ba {assetPath}" : $"[PrefabGen] \u2713 {assetPath}");
        }

        internal static void EnsureDirectory(string assetPath)
        {
            Directory.CreateDirectory(Path.Combine(
                Path.GetDirectoryName(Application.dataPath)!,
                assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        internal static void EnsureTag(string tag)
        {
            var so = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
            var t  = so.FindProperty("tags");
            for (int i = 0; i < t.arraySize; i++)
                if (t.GetArrayElementAtIndex(i).stringValue == tag) return;
            t.InsertArrayElementAtIndex(t.arraySize);
            t.GetArrayElementAtIndex(t.arraySize - 1).stringValue = tag;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void EnsureLayer(string layerName)
        {
            var so = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
            var l  = so.FindProperty("layers");
            for (int i = 8; i < l.arraySize; i++)
            {
                var p = l.GetArrayElementAtIndex(i);
                if (p.stringValue == layerName) return;
                if (!string.IsNullOrEmpty(p.stringValue)) continue;
                p.stringValue = layerName;
                so.ApplyModifiedPropertiesWithoutUndo();
                return;
            }
            Debug.LogWarning($"[PrefabGen] No free layer slot for '{layerName}'.");
        }

        [UnityEditor.InitializeOnLoadMethod]
        static void RegisterWithPrefabGenerator()
        {
            PrefabGenerator.Register("Enemies", GenerateEnemyPrefabs);
        }
    }

    internal class EnemyPrefabPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var p in imported)
            {
                if (p == EnemyPrefabHelper.EnemiesJson)
                {
                    EnemyPrefabHelper.GenerateEnemyPrefabs();
                    return;
                }
            }
        }
    }
}
#endif
