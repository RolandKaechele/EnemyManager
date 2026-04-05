#if ENEMYMANAGER_EM
using UnityEngine;
using EventManager.Runtime;

namespace EnemyManager.Runtime
{
    /// <summary>
    /// Optional bridge between EnemyManager and EventManager.
    /// Enable define <c>ENEMYMANAGER_EM</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// Fires the following named <see cref="GameEvent"/>s:
    /// <list type="bullet">
    ///   <item><c>"enemy.spawned"</c>        — <see cref="GameEvent.stringValue"/> = instanceId.</item>
    ///   <item><c>"enemy.defeated"</c>       — <see cref="GameEvent.stringValue"/> = instanceId.</item>
    ///   <item><c>"enemy.wave.started"</c>   — <see cref="GameEvent.stringValue"/> = waveId.</item>
    ///   <item><c>"enemy.wave.completed"</c> — <see cref="GameEvent.stringValue"/> = waveId.</item>
    ///   <item><c>"enemy.wave.aborted"</c>   — <see cref="GameEvent.stringValue"/> = waveId.</item>
    /// </list>
    /// </para>
    /// </summary>
    [AddComponentMenu("EnemyManager/Event Manager Bridge")]
    [DisallowMultipleComponent]
    public class EventManagerBridge : MonoBehaviour
    {
        [Tooltip("Event name fired when an enemy spawns.")]
        [SerializeField] private string spawnedEventName       = "enemy.spawned";

        [Tooltip("Event name fired when an enemy is defeated.")]
        [SerializeField] private string defeatedEventName      = "enemy.defeated";

        [Tooltip("Event name fired when a wave starts.")]
        [SerializeField] private string waveStartedEventName   = "enemy.wave.started";

        [Tooltip("Event name fired when a wave completes.")]
        [SerializeField] private string waveCompletedEventName = "enemy.wave.completed";

        [Tooltip("Event name fired when a wave is aborted.")]
        [SerializeField] private string waveAbortedEventName   = "enemy.wave.aborted";

        private EventManager.Runtime.EventManager _events;
        private EnemyManager _em;

        private void Awake()
        {
            _events = GetComponent<EventManager.Runtime.EventManager>()
                      ?? FindFirstObjectByType<EventManager.Runtime.EventManager>();
            _em     = GetComponent<EnemyManager>() ?? FindFirstObjectByType<EnemyManager>();

            if (_events == null) Debug.LogWarning("[EnemyManager/EventManagerBridge] EventManager not found.");
            if (_em     == null) Debug.LogWarning("[EnemyManager/EventManagerBridge] EnemyManager not found.");
        }

        private void OnEnable()
        {
            if (_em == null) return;
            _em.OnEnemySpawned   += OnEnemySpawned;
            _em.OnEnemyDefeated  += OnEnemyDefeated;
            _em.OnWaveStarted    += OnWaveStarted;
            _em.OnWaveCompleted  += OnWaveCompleted;
            _em.OnWaveAborted    += OnWaveAborted;
        }

        private void OnDisable()
        {
            if (_em == null) return;
            _em.OnEnemySpawned   -= OnEnemySpawned;
            _em.OnEnemyDefeated  -= OnEnemyDefeated;
            _em.OnWaveStarted    -= OnWaveStarted;
            _em.OnWaveCompleted  -= OnWaveCompleted;
            _em.OnWaveAborted    -= OnWaveAborted;
        }

        private void OnEnemySpawned(string enemyId, string instanceId)   => _events?.Fire(new GameEvent(spawnedEventName,       instanceId));
        private void OnEnemyDefeated(string enemyId, string instanceId)  => _events?.Fire(new GameEvent(defeatedEventName,      instanceId));
        private void OnWaveStarted(string waveId)                         => _events?.Fire(new GameEvent(waveStartedEventName,   waveId));
        private void OnWaveCompleted(string waveId)                       => _events?.Fire(new GameEvent(waveCompletedEventName, waveId));
        private void OnWaveAborted(string waveId)                         => _events?.Fire(new GameEvent(waveAbortedEventName,   waveId));
    }
}
#else
namespace EnemyManager.Runtime
{
    /// <summary>No-op stub — enable define <c>ENEMYMANAGER_EM</c> to activate.</summary>
    [UnityEngine.AddComponentMenu("EnemyManager/Event Manager Bridge")]
    public class EventManagerBridge : UnityEngine.MonoBehaviour { }
}
#endif
