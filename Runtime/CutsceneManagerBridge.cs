#if ENEMYMANAGER_CSM
using UnityEngine;
using CutsceneManager.Runtime;

namespace EnemyManager.Runtime
{
    /// <summary>
    /// Optional bridge between EnemyManager and CutsceneManager.
    /// Enable define <c>ENEMYMANAGER_CSM</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// Calls <see cref="EnemyManager.PauseSpawning"/> when a cutscene starts and
    /// <see cref="EnemyManager.ResumeSpawning"/> when it ends or is skipped.
    /// </para>
    /// </summary>
    [AddComponentMenu("EnemyManager/Cutscene Manager Bridge")]
    [DisallowMultipleComponent]
    public class CutsceneManagerBridge : MonoBehaviour
    {
        private EnemyManager _em;
        private CutsceneManager.Runtime.CutsceneManager _csm;

        private void Awake()
        {
            _em  = GetComponent<EnemyManager>() ?? FindFirstObjectByType<EnemyManager>();
            _csm = GetComponent<CutsceneManager.Runtime.CutsceneManager>()
                   ?? FindFirstObjectByType<CutsceneManager.Runtime.CutsceneManager>();

            if (_em  == null) Debug.LogWarning("[EnemyManager/CutsceneManagerBridge] EnemyManager not found.");
            if (_csm == null) Debug.LogWarning("[EnemyManager/CutsceneManagerBridge] CutsceneManager not found.");
        }

        private void OnEnable()
        {
            if (_csm == null) return;
            _csm.OnSequenceStarted   += OnCutsceneStarted;
            _csm.OnSequenceCompleted += OnCutsceneEnded;
            _csm.OnSequenceSkipped   += OnCutsceneEnded;
        }

        private void OnDisable()
        {
            if (_csm == null) return;
            _csm.OnSequenceStarted   -= OnCutsceneStarted;
            _csm.OnSequenceCompleted -= OnCutsceneEnded;
            _csm.OnSequenceSkipped   -= OnCutsceneEnded;
        }

        private void OnCutsceneStarted(string sequenceId) => _em?.PauseSpawning();
        private void OnCutsceneEnded(string sequenceId)   => _em?.ResumeSpawning();
    }
}
#else
namespace EnemyManager.Runtime
{
    /// <summary>No-op stub — enable define <c>ENEMYMANAGER_CSM</c> to activate.</summary>
    [UnityEngine.AddComponentMenu("EnemyManager/Cutscene Manager Bridge")]
    public class CutsceneManagerBridge : UnityEngine.MonoBehaviour { }
}
#endif
