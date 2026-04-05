#if ENEMYMANAGER_AIM
using UnityEngine;
using AiManager.Runtime;

namespace EnemyManager.Runtime
{
    /// <summary>
    /// Optional bridge between EnemyManager and AiManager.
    /// Enable define <c>ENEMYMANAGER_AIM</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// <list type="bullet">
    ///   <item>On <see cref="EnemyManager.OnEnemySpawned"/>: registers the spawned
    ///     instance with <see cref="AiManager.RegisterAgent"/>.</item>
    ///   <item>On <see cref="EnemyManager.OnEnemyDefeated"/>: deregisters the instance
    ///     from <see cref="AiManager.DeregisterAgent"/>.</item>
    ///   <item>On <see cref="EnemyManager.OnWaveStarted"/>: escalates the global alert
    ///     level to at least <see cref="AiAlertLevel.Alert"/>.</item>
    ///   <item>On <see cref="EnemyManager.OnWaveCompleted"/>: deescalates alert level
    ///     back toward <see cref="AiAlertLevel.Quiet"/>.</item>
    /// </list>
    /// </para>
    /// </summary>
    [AddComponentMenu("EnemyManager/AI Manager Bridge")]
    [DisallowMultipleComponent]
    public class AiManagerBridge : MonoBehaviour
    {
        private EnemyManager _em;
        private AiManager.Runtime.AiManager _ai;

        private void Awake()
        {
            _em = GetComponent<EnemyManager>() ?? FindFirstObjectByType<EnemyManager>();
            _ai = GetComponent<AiManager.Runtime.AiManager>()
                  ?? FindFirstObjectByType<AiManager.Runtime.AiManager>();

            if (_em == null) Debug.LogWarning("[EnemyManager/AiManagerBridge] EnemyManager not found.");
            if (_ai == null) Debug.LogWarning("[EnemyManager/AiManagerBridge] AiManager not found.");
        }

        private void OnEnable()
        {
            if (_em == null) return;
            _em.OnEnemySpawned   += OnEnemySpawned;
            _em.OnEnemyDefeated  += OnEnemyDefeated;
            _em.OnWaveStarted    += OnWaveStarted;
            _em.OnWaveCompleted  += OnWaveCompleted;
        }

        private void OnDisable()
        {
            if (_em == null) return;
            _em.OnEnemySpawned   -= OnEnemySpawned;
            _em.OnEnemyDefeated  -= OnEnemyDefeated;
            _em.OnWaveStarted    -= OnWaveStarted;
            _em.OnWaveCompleted  -= OnWaveCompleted;
        }

        private void OnEnemySpawned(string enemyId, string instanceId)
        {
            if (_ai == null) return;
            var record = _em?.GetInstance(instanceId);
            if (record?.gameObject != null)
                _ai.RegisterAgent(instanceId, record.gameObject);
        }

        private void OnEnemyDefeated(string enemyId, string instanceId) =>
            _ai?.DeregisterAgent(instanceId);

        private void OnWaveStarted(string waveId)
        {
            if (_ai == null) return;
            if (_ai.AlertLevel < AiAlertLevel.Alert)
                _ai.SetAlertLevel(AiAlertLevel.Alert);
        }

        private void OnWaveCompleted(string waveId) =>
            _ai?.DeescalateAlertLevel();
    }
}
#else
namespace EnemyManager.Runtime
{
    /// <summary>No-op stub — enable define <c>ENEMYMANAGER_AIM</c> to activate.</summary>
    [UnityEngine.AddComponentMenu("EnemyManager/AI Manager Bridge")]
    public class AiManagerBridge : UnityEngine.MonoBehaviour { }
}
#endif
