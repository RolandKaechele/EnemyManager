using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace EnemyManager.Runtime
{
    /// <summary>
    /// <b>EnemyManager</b> manages the enemy roster: definitions, wave spawning, live instances, and defeat tracking.
    ///
    /// <para><b>Responsibilities:</b>
    /// <list type="number">
    ///   <item>Store enemy definitions (id, type, stats, prefab path, AI behavior id).</item>
    ///   <item>Spawn enemies and track live instances by unique instance ids.</item>
    ///   <item>Record defeated enemies; persist via SaveManager (optional bridge).</item>
    ///   <item>Drive waves: sequential spawning with configurable delays and optional looping.</item>
    ///   <item>Pause/resume spawning on demand (e.g. during cutscenes or dialogue).</item>
    ///   <item>Optionally merge definitions and waves from a JSON file for modding.</item>
    /// </list>
    /// </para>
    ///
    /// <para><b>Modding / JSON:</b> Enable <c>loadFromJson</c> and place an
    /// <c>enemies.json</c> in <c>StreamingAssets/</c>.
    /// JSON entries are <b>merged by id</b>: JSON overrides Inspector entries with the same id
    /// and can add new ones.</para>
    ///
    /// <para><b>Optional integration defines:</b>
    /// <list type="bullet">
    ///   <item><c>ENEMYMANAGER_EM</c>  — EventManager: fires <c>enemy.spawned</c>, <c>enemy.defeated</c>, <c>enemy.wave.started</c>, <c>enemy.wave.completed</c>, <c>enemy.wave.aborted</c>.</item>
    ///   <item><c>ENEMYMANAGER_SM</c>  — SaveManager: persists defeated enemy ids as save flags.</item>
    ///   <item><c>ENEMYMANAGER_AIM</c> — AiManager: registers spawned enemies as AI agents and escalates alert on wave start.</item>
    ///   <item><c>ENEMYMANAGER_CSM</c> — CutsceneManager: pauses spawning on cutscene start; resumes on end/skip.</item>
    ///   <item><c>ENEMYMANAGER_STM</c> — StateManager: pauses spawning during non-Gameplay states.</item>
    ///   <item><c>ENEMYMANAGER_MLF</c> — MapLoaderFramework: clears all instances on chapter change.</item>
    /// </list>
    /// </para>
    /// </summary>
    [AddComponentMenu("EnemyManager/Enemy Manager")]
    [DisallowMultipleComponent]
#if ODIN_INSPECTOR
    public class EnemyManager : SerializedMonoBehaviour
#else
    public class EnemyManager : MonoBehaviour
#endif
    {
        // ─── Inspector ───────────────────────────────────────────────────────────

        [Header("Roster")]
        [Tooltip("Enemy definitions. JSON entries are merged on top by id.")]
        [SerializeField] private List<EnemyDefinition> enemies = new List<EnemyDefinition>();

        [Header("Waves")]
        [Tooltip("Wave definitions. JSON entries are merged on top by id.")]
        [SerializeField] private List<WaveDefinition> waves = new List<WaveDefinition>();

        [Header("Spawn Settings")]
        [Tooltip("Default parent transform for spawned enemies. Leave null to spawn at the root.")]
        [SerializeField] private Transform spawnParent;

        [Header("Modding / JSON")]
        [Tooltip("When enabled, merge definitions and waves from a JSON file in StreamingAssets/ at startup.")]
        [SerializeField] private bool loadFromJson = false;

        [Tooltip("Path relative to StreamingAssets/ (e.g. 'enemies.json').")]
        [SerializeField] private string jsonPath = "enemies.json";

        [Header("Debug")]
        [Tooltip("Log all spawns, defeats, and wave events to the Unity Console.")]
        [SerializeField] private bool verboseLogging = false;

        // ─── Events ──────────────────────────────────────────────────────────────

        /// <summary>Fired when an enemy is spawned. Parameters: enemyId, instanceId.</summary>
        public event Action<string, string> OnEnemySpawned;

        /// <summary>Fired when an enemy is defeated. Parameters: enemyId, instanceId.</summary>
        public event Action<string, string> OnEnemyDefeated;

        /// <summary>Fired when a wave starts. Parameter: waveId.</summary>
        public event Action<string> OnWaveStarted;

        /// <summary>Fired when a wave completes all spawns and all remaining enemies are defeated. Parameter: waveId.</summary>
        public event Action<string> OnWaveCompleted;

        /// <summary>Fired when a wave is aborted manually. Parameter: waveId.</summary>
        public event Action<string> OnWaveAborted;

        // ─── State ───────────────────────────────────────────────────────────────

        private readonly Dictionary<string, EnemyDefinition>     _enemyIndex    = new Dictionary<string, EnemyDefinition>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, WaveDefinition>      _waveIndex     = new Dictionary<string, WaveDefinition>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, EnemyInstanceRecord> _liveInstances = new Dictionary<string, EnemyInstanceRecord>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string>                          _defeatedIds   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private bool _spawningPaused;
        private int  _instanceCounter;
        private string _activeWaveId;
        private Coroutine _waveCoroutine;

        // ─── Properties ──────────────────────────────────────────────────────────

        /// <summary>Whether enemy spawning is currently paused.</summary>
        public bool IsSpawningPaused => _spawningPaused;

        /// <summary>Id of the currently running wave, or null if no wave is active.</summary>
        public string ActiveWaveId => _activeWaveId;

        /// <summary>Whether a wave is currently running.</summary>
        public bool IsWaveActive => _waveCoroutine != null;

        /// <summary>Number of live (not yet defeated) enemy instances.</summary>
        public int ActiveEnemyCount => _liveInstances.Count;

        // ─── Unity lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            BuildIndices();
            if (loadFromJson) LoadJson();
        }

        // ─── Definition API ───────────────────────────────────────────────────────

        /// <summary>Returns the <see cref="EnemyDefinition"/> for <paramref name="id"/>, or null.</summary>
        public EnemyDefinition GetDefinition(string id) =>
            _enemyIndex.TryGetValue(id, out var def) ? def : null;

        /// <summary>Returns the <see cref="WaveDefinition"/> for <paramref name="id"/>, or null.</summary>
        public WaveDefinition GetWave(string id) =>
            _waveIndex.TryGetValue(id, out var wave) ? wave : null;

        /// <summary>Read-only enemy roster keyed by id.</summary>
        public IReadOnlyDictionary<string, EnemyDefinition> Enemies => _enemyIndex;

        // ─── Spawn API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Spawns the enemy with <paramref name="enemyId"/> at <paramref name="position"/>.
        /// Returns the unique instance id of the spawned enemy, or null if spawning failed.
        /// </summary>
        public string SpawnEnemy(string enemyId, Vector3 position, Quaternion rotation = default)
        {
            if (_spawningPaused)
            {
                if (verboseLogging) Debug.Log($"[EnemyManager] SpawnEnemy blocked — spawning is paused.");
                return null;
            }

            if (!_enemyIndex.TryGetValue(enemyId, out var def))
            {
                Debug.LogWarning($"[EnemyManager] SpawnEnemy: unknown enemy id '{enemyId}'.");
                return null;
            }

            GameObject go = null;

            if (!string.IsNullOrEmpty(def.prefabResourcePath))
            {
                var prefab = Resources.Load<GameObject>(def.prefabResourcePath);
                if (prefab != null)
                    go = Instantiate(prefab, position, rotation, spawnParent);
                else
                    Debug.LogWarning($"[EnemyManager] Prefab not found at Resources/{def.prefabResourcePath}.");
            }

            if (go == null)
            {
                go = new GameObject($"Enemy_{enemyId}");
                go.transform.position = position;
                go.transform.rotation = rotation;
                if (spawnParent != null) go.transform.SetParent(spawnParent);
            }

            _instanceCounter++;
            string instanceId = $"{enemyId}_{_instanceCounter}";
            go.name = instanceId;

            var record = new EnemyInstanceRecord
            {
                instanceId = instanceId,
                enemyId    = enemyId,
                gameObject = go,
                defeated   = false
            };
            _liveInstances[instanceId] = record;

            if (verboseLogging)
                Debug.Log($"[EnemyManager] Spawned: {instanceId} at {position}");

            OnEnemySpawned?.Invoke(enemyId, instanceId);
            return instanceId;
        }

        // ─── Defeat API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Records the defeat of the enemy instance with <paramref name="instanceId"/>.
        /// Fires <see cref="OnEnemyDefeated"/> and removes the instance from the live registry.
        /// If all wave instances are defeated, fires <see cref="OnWaveCompleted"/>.
        /// </summary>
        public void DefeatEnemy(string instanceId)
        {
            if (!_liveInstances.TryGetValue(instanceId, out var record)) return;

            record.defeated = true;
            string enemyId = record.enemyId;
            _defeatedIds.Add(instanceId);
            _liveInstances.Remove(instanceId);

            if (verboseLogging)
                Debug.Log($"[EnemyManager] Defeated: {instanceId}");

            OnEnemyDefeated?.Invoke(enemyId, instanceId);

            // Check if active wave is complete (no remaining live instances from the wave)
            if (_activeWaveId != null && _liveInstances.Count == 0 && _waveCoroutine == null)
            {
                var waveId = _activeWaveId;
                _activeWaveId = null;
                OnWaveCompleted?.Invoke(waveId);

                if (verboseLogging)
                    Debug.Log($"[EnemyManager] Wave completed: {waveId}");
            }
        }

        /// <summary>
        /// Returns <c>true</c> if an enemy with instance id or enemy definition id <paramref name="id"/>
        /// is in the defeated registry.
        /// </summary>
        public bool IsDefeated(string id) => _defeatedIds.Contains(id);

        // ─── Wave API ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Starts the wave with <paramref name="waveId"/>. Any active wave is first aborted.
        /// Fires <see cref="OnWaveStarted"/>.
        /// </summary>
        public void StartWave(string waveId)
        {
            if (!_waveIndex.TryGetValue(waveId, out var waveDef))
            {
                Debug.LogWarning($"[EnemyManager] StartWave: unknown wave id '{waveId}'.");
                return;
            }

            AbortWave();
            _activeWaveId = waveId;

            if (verboseLogging)
                Debug.Log($"[EnemyManager] Wave started: {waveId}");

            OnWaveStarted?.Invoke(waveId);
            _waveCoroutine = StartCoroutine(RunWave(waveDef));
        }

        /// <summary>
        /// Aborts the active wave immediately.
        /// Fires <see cref="OnWaveAborted"/> if a wave was running.
        /// </summary>
        public void AbortWave()
        {
            if (_waveCoroutine == null) return;
            StopCoroutine(_waveCoroutine);
            _waveCoroutine = null;

            var abortedId = _activeWaveId;
            _activeWaveId = null;

            if (!string.IsNullOrEmpty(abortedId))
            {
                if (verboseLogging)
                    Debug.Log($"[EnemyManager] Wave aborted: {abortedId}");

                OnWaveAborted?.Invoke(abortedId);
            }
        }

        // ─── Pause / Resume API ───────────────────────────────────────────────────

        /// <summary>Pauses all enemy spawning (wave coroutines are suspended).</summary>
        public void PauseSpawning()
        {
            if (_spawningPaused) return;
            _spawningPaused = true;

            if (verboseLogging)
                Debug.Log("[EnemyManager] Spawning paused.");
        }

        /// <summary>Resumes enemy spawning.</summary>
        public void ResumeSpawning()
        {
            if (!_spawningPaused) return;
            _spawningPaused = false;

            if (verboseLogging)
                Debug.Log("[EnemyManager] Spawning resumed.");
        }

        // ─── Query API ────────────────────────────────────────────────────────────

        /// <summary>Returns the live instance record for <paramref name="instanceId"/>, or null.</summary>
        public EnemyInstanceRecord GetInstance(string instanceId) =>
            _liveInstances.TryGetValue(instanceId, out var rec) ? rec : null;

        /// <summary>Returns a snapshot of all currently live enemy instances.</summary>
        public IReadOnlyDictionary<string, EnemyInstanceRecord> LiveInstances => _liveInstances;

        /// <summary>Clears all live instances (e.g. on scene/chapter change). Does not fire defeat events.</summary>
        public void ClearAllInstances()
        {
            _liveInstances.Clear();

            if (verboseLogging)
                Debug.Log("[EnemyManager] All live instances cleared.");
        }

        // ─── Wave coroutine ───────────────────────────────────────────────────────

        private IEnumerator RunWave(WaveDefinition waveDef)
        {
            foreach (var entry in waveDef.enemies)
            {
                for (int i = 0; i < entry.count; i++)
                {
                    // Wait while spawning is paused
                    while (_spawningPaused)
                        yield return null;

                    SpawnEnemy(entry.enemyId, GetSpawnPosition());

                    if (waveDef.timeBetweenSpawns > 0f)
                        yield return new WaitForSeconds(waveDef.timeBetweenSpawns);
                }
            }

            _waveCoroutine = null;

            // If loop is set, immediately restart
            if (waveDef.loop && _activeWaveId == waveDef.id)
            {
                _waveCoroutine = StartCoroutine(RunWave(waveDef));
            }
        }

        /// <summary>
        /// Returns a spawn position. If <see cref="spawnParent"/> is set, spawns at the parent's position;
        /// otherwise at Vector3.zero. Override by subclassing or using the SpawnEnemy overload directly.
        /// </summary>
        protected virtual Vector3 GetSpawnPosition() =>
            spawnParent != null ? spawnParent.position : Vector3.zero;

        // ─── Internal ─────────────────────────────────────────────────────────────

        private void BuildIndices()
        {
            _enemyIndex.Clear();
            foreach (var e in enemies)
            {
                if (e == null || string.IsNullOrEmpty(e.id)) continue;
                _enemyIndex[e.id] = e;
            }

            _waveIndex.Clear();
            foreach (var w in waves)
            {
                if (w == null || string.IsNullOrEmpty(w.id)) continue;
                _waveIndex[w.id] = w;
            }
        }

        private void LoadJson()
        {
            string path = Path.Combine(Application.streamingAssetsPath, jsonPath);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[EnemyManager] JSON not found: {path}");
                return;
            }
            try
            {
                var roster = JsonUtility.FromJson<EnemyRosterJson>(File.ReadAllText(path));

                if (roster?.enemies != null)
                {
                    foreach (var e in roster.enemies)
                    {
                        if (e == null || string.IsNullOrEmpty(e.id)) continue;
                        _enemyIndex[e.id] = e;
                    }
                }

                if (roster?.waves != null)
                {
                    foreach (var w in roster.waves)
                    {
                        if (w == null || string.IsNullOrEmpty(w.id)) continue;
                        _waveIndex[w.id] = w;
                    }
                }

                Debug.Log($"[EnemyManager] Roster merged from {path}.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EnemyManager] Failed to parse {jsonPath}: {ex.Message}");
            }
        }
    }
}
