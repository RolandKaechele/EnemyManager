#if ENEMYMANAGER_MLF
using UnityEngine;
using MapLoaderFramework.Runtime;

namespace EnemyManager.Runtime
{
    /// <summary>
    /// Optional bridge between EnemyManager and MapLoaderFramework.
    /// Enable define <c>ENEMYMANAGER_MLF</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// Calls <see cref="EnemyManager.AbortWave"/> and <see cref="EnemyManager.ClearAllInstances"/>
    /// whenever the active chapter changes (<see cref="MapLoaderManager.OnChapterChanged"/>),
    /// clearing stale enemy instances when a new map is loaded.
    /// </para>
    /// </summary>
    [AddComponentMenu("EnemyManager/Map Loader Bridge")]
    [DisallowMultipleComponent]
    public class MapLoaderBridge : MonoBehaviour
    {
        private EnemyManager _em;
        private MapLoaderManager _mlf;

        private void Awake()
        {
            _em  = GetComponent<EnemyManager>() ?? FindFirstObjectByType<EnemyManager>();
            _mlf = GetComponent<MapLoaderManager>() ?? FindFirstObjectByType<MapLoaderManager>();

            if (_em  == null) Debug.LogWarning("[EnemyManager/MapLoaderBridge] EnemyManager not found.");
            if (_mlf == null) Debug.LogWarning("[EnemyManager/MapLoaderBridge] MapLoaderManager not found.");
        }

        private void OnEnable()
        {
            if (_mlf != null) _mlf.OnChapterChanged += OnChapterChanged;
        }

        private void OnDisable()
        {
            if (_mlf != null) _mlf.OnChapterChanged -= OnChapterChanged;
        }

        private void OnChapterChanged(int previous, int current)
        {
            _em?.AbortWave();
            _em?.ClearAllInstances();
        }
    }
}
#else
namespace EnemyManager.Runtime
{
    /// <summary>No-op stub — enable define <c>ENEMYMANAGER_MLF</c> to activate.</summary>
    [UnityEngine.AddComponentMenu("EnemyManager/Map Loader Bridge")]
    public class MapLoaderBridge : UnityEngine.MonoBehaviour { }
}
#endif
