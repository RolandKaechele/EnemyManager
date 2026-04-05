#if ENEMYMANAGER_SM
using UnityEngine;
using SaveManager.Runtime;

namespace EnemyManager.Runtime
{
    /// <summary>
    /// Optional bridge between EnemyManager and SaveManager.
    /// Enable define <c>ENEMYMANAGER_SM</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// Persists defeated enemy instance ids as save flags in the active save slot so they
    /// remain defeated across sessions. Uses flag keys of the form <c>enemy_defeated_&lt;instanceId&gt;</c>.
    /// On save load, flags are not automatically re-applied to EnemyManager state — the game
    /// should query <see cref="SaveManager.Runtime.SaveManager.IsSet"/> at level initialisation.
    /// </para>
    /// </summary>
    [AddComponentMenu("EnemyManager/Save Manager Bridge")]
    [DisallowMultipleComponent]
    public class SaveManagerBridge : MonoBehaviour
    {
        private const string FlagPrefix = "enemy_defeated_";

        private EnemyManager _em;
        private SaveManager.Runtime.SaveManager _save;

        private void Awake()
        {
            _em   = GetComponent<EnemyManager>() ?? FindFirstObjectByType<EnemyManager>();
            _save = GetComponent<SaveManager.Runtime.SaveManager>()
                    ?? FindFirstObjectByType<SaveManager.Runtime.SaveManager>();

            if (_em   == null) Debug.LogWarning("[EnemyManager/SaveManagerBridge] EnemyManager not found.");
            if (_save == null) Debug.LogWarning("[EnemyManager/SaveManagerBridge] SaveManager not found.");
        }

        private void OnEnable()
        {
            if (_em != null) _em.OnEnemyDefeated += OnEnemyDefeated;
        }

        private void OnDisable()
        {
            if (_em != null) _em.OnEnemyDefeated -= OnEnemyDefeated;
        }

        private void OnEnemyDefeated(string enemyId, string instanceId)
        {
            if (_save == null) return;
            _save.SetFlag(FlagPrefix + instanceId);
        }

        /// <summary>
        /// Checks whether the given instance was recorded as defeated in the current save slot.
        /// Use at level initialisation to skip re-spawning permanently defeated enemies.
        /// </summary>
        public bool WasDefeated(string instanceId) =>
            _save != null && _save.IsSet(FlagPrefix + instanceId);
    }
}
#else
namespace EnemyManager.Runtime
{
    /// <summary>No-op stub — enable define <c>ENEMYMANAGER_SM</c> to activate.</summary>
    [UnityEngine.AddComponentMenu("EnemyManager/Save Manager Bridge")]
    public class SaveManagerBridge : UnityEngine.MonoBehaviour { }
}
#endif
