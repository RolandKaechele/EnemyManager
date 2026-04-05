#if ENEMYMANAGER_STM
using UnityEngine;
using StateManager.Runtime;

namespace EnemyManager.Runtime
{
    /// <summary>
    /// Optional bridge between EnemyManager and StateManager.
    /// Enable define <c>ENEMYMANAGER_STM</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// Calls <see cref="EnemyManager.PauseSpawning"/> when the app enters
    /// <see cref="AppState.Cutscene"/>, <see cref="AppState.Dialogue"/>,
    /// <see cref="AppState.Paused"/>, or <see cref="AppState.MiniGame"/> states,
    /// and calls <see cref="EnemyManager.ResumeSpawning"/> when returning to
    /// <see cref="AppState.Gameplay"/>.
    /// </para>
    /// </summary>
    [AddComponentMenu("EnemyManager/State Manager Bridge")]
    [DisallowMultipleComponent]
    public class StateManagerBridge : MonoBehaviour
    {
        private EnemyManager _em;
        private StateManager.Runtime.StateManager _state;

        private void Awake()
        {
            _em    = GetComponent<EnemyManager>() ?? FindFirstObjectByType<EnemyManager>();
            _state = GetComponent<StateManager.Runtime.StateManager>()
                     ?? FindFirstObjectByType<StateManager.Runtime.StateManager>();

            if (_em    == null) Debug.LogWarning("[EnemyManager/StateManagerBridge] EnemyManager not found.");
            if (_state == null) Debug.LogWarning("[EnemyManager/StateManagerBridge] StateManager not found.");
        }

        private void OnEnable()
        {
            if (_state != null) _state.OnStateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            if (_state != null) _state.OnStateChanged -= OnStateChanged;
        }

        private void OnStateChanged(AppState previous, AppState next)
        {
            if (_em == null) return;

            switch (next)
            {
                case AppState.Cutscene:
                case AppState.Dialogue:
                case AppState.Paused:
                case AppState.MiniGame:
                    _em.PauseSpawning();
                    break;

                case AppState.Gameplay:
                    _em.ResumeSpawning();
                    break;
            }
        }
    }
}
#else
namespace EnemyManager.Runtime
{
    /// <summary>No-op stub — enable define <c>ENEMYMANAGER_STM</c> to activate.</summary>
    [UnityEngine.AddComponentMenu("EnemyManager/State Manager Bridge")]
    public class StateManagerBridge : UnityEngine.MonoBehaviour { }
}
#endif
