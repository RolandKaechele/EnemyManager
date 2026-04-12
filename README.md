# EnemyManager

A modular enemy roster and wave spawning manager for Unity.  
Manages enemy definitions, live instances, wave-based spawning, defeat tracking, and optional JSON modding support.


## Features

- **Enemy definitions** — define enemies (id, type, stats, prefab path, AI behavior id) in the Inspector
- **Four enemy types** — `Standard`, `Swarm`, `Elite`, `Boss`
- **Wave definitions** — sequential spawn waves with configurable counts and inter-spawn delays
- **Live instance tracking** — each spawned enemy gets a unique instance id; query or defeat by id
- **Defeat registry** — record defeated enemies in-memory; persist via SaveManager bridge
- **Pause / resume spawning** — block spawning during cutscenes, dialogue, or menu states
- **JSON / Modding** — merge enemy definitions from `StreamingAssets/enemies/` and wave definitions from `StreamingAssets/waves/` at startup
- **AiManager integration** — register spawned enemies with AiManager; escalate alert on wave start (activated via `ENEMYMANAGER_AIM`)
- **SaveManager integration** — persist defeated enemy ids as save flags (activated via `ENEMYMANAGER_SM`)
- **CutsceneManager integration** — pause/resume spawning on cutscene start/end (activated via `ENEMYMANAGER_CSM`)
- **StateManager integration** — pause during non-Gameplay states (activated via `ENEMYMANAGER_STM`)
- **MapLoaderFramework integration** — abort wave and clear instances on chapter change (activated via `ENEMYMANAGER_MLF`)
- **EventManager integration** — fires `enemy.spawned`, `enemy.defeated`, and wave events (activated via `ENEMYMANAGER_EM`)
- **Custom Inspector** — live instance list, test spawn/defeat/wave controls at runtime
- **Odin Inspector integration** — `SerializedMonoBehaviour` base for full Inspector serialization (activated via `ODIN_INSPECTOR`)


## Installation

### Option A — Unity Package Manager (Git URL)

1. Open **Window → Package Manager**
2. Click **+** → **Add package from git URL…**
3. Enter:

   ```
   https://github.com/RolandKaechele/EnemyManager.git
   ```

### Option B — Clone into Assets

```bash
git clone https://github.com/RolandKaechele/EnemyManager.git Assets/EnemyManager
```

### Option C — npm / postinstall

```bash
cd Assets/EnemyManager
npm install
```

`postinstall.js` creates the required `StreamingAssets/` folder and optionally copies example JSON files to `enemies/` and `waves/`.


## Scene Setup

1. Attach `EnemyManager` to a persistent GameObject.
2. Define enemy and wave entries in the Inspector or via JSON.
3. (Optional) Add bridge components on the same GameObject for cross-module integration.


## Quick Start

### 1. Inspector fields

| Field | Default | Description |
| ----- | ------- | ----------- |
| `enemies` | *(empty)* | Enemy definition list |
| `waves` | *(empty)* | Wave definition list |
| `spawnParent` | `null` | Parent transform for spawned enemies |
| `loadFromJson` | `false` | Merge from `StreamingAssets/` JSON files on Awake |
| `jsonPath` | `"enemies/"` | Folder relative to `StreamingAssets/` containing `.json` files to merge. Falls back to single-file mode if the value points to an existing file. |
| `waveJsonPath` | `"waves/"` | Folder relative to `StreamingAssets/` containing `.json` files to merge. Falls back to single-file mode if the value points to an existing file. |
| `verboseLogging` | `false` | Log all spawns/defeats to the Console |

### 2. EnemyDefinition fields

| Field | Description |
| ----- | ----------- |
| `id` | Unique identifier (e.g. `"green_spider"`) |
| `displayName` | Human-readable name |
| `type` | `Standard`, `Swarm`, `Elite`, or `Boss` |
| `stats` | `EnemyStats` (health, speed, attackPower, pointValue) |
| `prefabResourcePath` | Path inside `Resources/` (e.g. `"Enemies/GreenSpider"`) |
| `aiBehaviorId` | Optional AiManager behavior id |
| `alwaysRespawns` | Whether to re-spawn between saves |
| `bossPhases` | Phase count (Boss type only) |

### 3. Spawn and defeat enemies

```csharp
var em = FindFirstObjectByType<EnemyManager.Runtime.EnemyManager>();

// Spawn at a position — returns a unique instance id
string instanceId = em.SpawnEnemy("green_spider", spawnPoint.position);

// Defeat by instance id (call from the enemy's script on death)
em.DefeatEnemy(instanceId);

// Check if an enemy was defeated
bool defeated = em.IsDefeated(instanceId);
```

### 4. Wave management

```csharp
em.StartWave("wave_spiders_01");   // start a defined wave
em.AbortWave();                    // abort the current wave
bool waveActive = em.IsWaveActive;
string waveId   = em.ActiveWaveId;
```

### 5. Pause / resume spawning

```csharp
em.PauseSpawning();
em.ResumeSpawning();
bool paused = em.IsSpawningPaused;
```

### 6. React to events

```csharp
em.OnEnemySpawned   += (enemyId, instanceId) => Debug.Log($"Spawned: {instanceId}");
em.OnEnemyDefeated  += (enemyId, instanceId) => Debug.Log($"Defeated: {instanceId}");
em.OnWaveStarted    += waveId => Debug.Log($"Wave started: {waveId}");
em.OnWaveCompleted  += waveId => Debug.Log($"Wave complete: {waveId}");
em.OnWaveAborted    += waveId => Debug.Log($"Wave aborted: {waveId}");
```


## JSON / Modding

Enable `loadFromJson` and place one or more `.json` files in `StreamingAssets/enemies/` and `StreamingAssets/waves/`.
All `*.json` files in each folder are loaded and merged by `id` at startup.

**`StreamingAssets/enemies/`** — enemy definitions (example: `StreamingAssets/enemies/main.json`):

```json
{
  "enemies": [
    {
      "id": "green_spider",
      "displayName": "Grüne Spinne",
      "type": "Swarm",
      "stats": { "health": 30, "speed": 4.0, "attackPower": 5, "pointValue": 50 },
      "prefabResourcePath": "Prefabs/Enemies/GreenSpider",
      "aiBehaviorId": "spider_drone",
      "alwaysRespawns": true,
      "bossPhases": 1
    }
  ]
}
```

**`StreamingAssets/waves/`** — wave definitions (example: `StreamingAssets/waves/main.json`):

```json
{
  "waves": [
    {
      "id": "wave_spiders_01",
      "displayName": "Spinnen-Angriff",
      "enemies": [
        { "enemyId": "green_spider", "count": 5 }
      ],
      "timeBetweenSpawns": 1.5,
      "loop": false
    }
  ]
}
```

JSON and Inspector entries are **merged by id**.


## Runtime API

| Member | Description |
| ------ | ----------- |
| `IsSpawningPaused` | Whether spawning is currently paused |
| `ActiveWaveId` | Id of the running wave, or null |
| `IsWaveActive` | Whether a wave coroutine is running |
| `ActiveEnemyCount` | Number of live (non-defeated) instances |
| `LiveInstances` | `IReadOnlyDictionary<string, EnemyInstanceRecord>` |
| `Enemies` | `IReadOnlyDictionary<string, EnemyDefinition>` |
| `GetDefinition(id)` | Returns an `EnemyDefinition` by id |
| `GetWave(id)` | Returns a `WaveDefinition` by id |
| `SpawnEnemy(id, pos)` | Spawn and return instanceId |
| `DefeatEnemy(instanceId)` | Record defeat, fire events, check wave complete |
| `IsDefeated(id)` | True if the instance id is in the defeated registry |
| `StartWave(waveId)` | Start a predefined wave |
| `AbortWave()` | Stop the active wave |
| `PauseSpawning()` | Block all spawning |
| `ResumeSpawning()` | Resume spawning |
| `GetInstance(instanceId)` | Returns a live `EnemyInstanceRecord` |
| `ClearAllInstances()` | Remove all live instance records |
| `OnEnemySpawned` | `event Action<string, string>` — enemyId, instanceId |
| `OnEnemyDefeated` | `event Action<string, string>` — enemyId, instanceId |
| `OnWaveStarted` | `event Action<string>` — waveId |
| `OnWaveCompleted` | `event Action<string>` — waveId |
| `OnWaveAborted` | `event Action<string>` — waveId |


## Optional Integrations

### AiManager (`ENEMYMANAGER_AIM`)

Attach `AiManagerBridge` to the same GameObject as EnemyManager.
Requires `ENEMYMANAGER_AIM` define and [AiManager](https://github.com/RolandKaechele/AiManager).

Registers/deregisters spawned enemies with AiManager; escalates alert level to at least `Alert` on wave start; deescalates on wave complete.

### SaveManager (`ENEMYMANAGER_SM`)

Attach `SaveManagerBridge` to the same GameObject as EnemyManager.
Requires `ENEMYMANAGER_SM` define and [SaveManager](https://github.com/RolandKaechele/SaveManager).

Persists `enemy_defeated_<instanceId>` flags in the save slot. Query `WasDefeated(instanceId)` during level init to skip re-spawning permanently defeated enemies.

### CutsceneManager (`ENEMYMANAGER_CSM`)

Requires `ENEMYMANAGER_CSM` define and [CutsceneManager](https://github.com/RolandKaechele/CutsceneManager). Pauses spawning on cutscene start; resumes on end/skip.

### StateManager (`ENEMYMANAGER_STM`)

Requires `ENEMYMANAGER_STM` define and [StateManager](https://github.com/RolandKaechele/StateManager). Pauses during `Cutscene`, `Dialogue`, `Paused`, `MiniGame`; resumes on `Gameplay`.

### MapLoaderFramework (`ENEMYMANAGER_MLF`)

Requires `ENEMYMANAGER_MLF` define and [MapLoaderFramework](https://github.com/RolandKaechele/MapLoaderFramework). Aborts active wave and clears all live instances on `OnChapterChanged`.

### EventManager (`ENEMYMANAGER_EM`)

Requires `ENEMYMANAGER_EM` define and [EventManager](https://github.com/RolandKaechele/EventManager).

| Event | When | Value |
| ----- | ---- | ----- |
| `enemy.spawned` | `SpawnEnemy()` succeeds | `stringValue` = instanceId |
| `enemy.defeated` | `DefeatEnemy()` | `stringValue` = instanceId |
| `enemy.wave.started` | `StartWave()` | `stringValue` = waveId |
| `enemy.wave.completed` | last instance defeated | `stringValue` = waveId |
| `enemy.wave.aborted` | `AbortWave()` | `stringValue` = waveId |

### EventManager reverse bridge (`EVENTMANAGER_ENM`)

`EnemyEventBridge` in EventManager subscribes to EnemyManager and re-broadcasts events on the global bus. Activate define on the **EventManager** side.


## Defines Reference

| Define | Dependency | Effect |
| ------ | ---------- | ------ |
| `ENEMYMANAGER_AIM` | AiManager | Register agents; escalate/deescalate alert on waves |
| `ENEMYMANAGER_SM` | SaveManager | Persist defeated enemy flags |
| `ENEMYMANAGER_CSM` | CutsceneManager | Pause/resume spawning on cutscene events |
| `ENEMYMANAGER_STM` | StateManager | Pause/resume spawning on state changes |
| `ENEMYMANAGER_MLF` | MapLoaderFramework | Abort wave and clear instances on chapter change |
| `ENEMYMANAGER_EM` | EventManager | Fire `enemy.*` events |
| `EVENTMANAGER_ENM` | EventManager | EventManager re-broadcasts enemy events |
| `SPAWNMANAGER_EEM` | SpawnManager | SpawnManager notifies EnemyManager when a spawn definition matches a registered enemy |
| `ODIN_INSPECTOR` | Odin Inspector (Asset Store) | `SerializedMonoBehaviour`; `[ReadOnly]` on runtime fields |


## Editor Tools — Prefab Generation

`EnemyManagerEditor.cs` in `Editor/` doubles as a prefab generator.
`EnemyPrefabHelper` reads all `*.json` from `StreamingAssets/enemies/` and outputs one prefab per `EnemyDefinition` into `Assets/Resources/Prefabs/Enemies/`.

**Manual**

- **Generate Prefabs → Enemies** in the Unity menu bar
- **Generate Prefabs → All** (`Ctrl+Shift+G`) — regenerates all registered prefab generators in one step

**Automatic**
Saving any `.json` file to `StreamingAssets/enemies/` triggers `EnemyPrefabPostprocessor` via `AssetPostprocessor.OnPostprocessAllAssets` — no manual action required.

**What is generated per prefab**

| Component | Details |
| --------- | ------- |
| `SpriteRenderer` | `sortingOrder` 0 (1 for Boss); loads `Resources/Enemies/<id>_sprite` if present |
| `Animator` | Loads `Resources/Animators/<id>_controller` if present |
| `Rigidbody2D` | Dynamic, Continuous collision, freeze rotation Z |
| `CapsuleCollider2D` | Hitbox, 0.8 × 1.2 |
| `CircleCollider2D` | Detection trigger, radius 1.5 (Swarm) / 2.5 (Standard) / 3.0 (Elite) / 4.0 (Boss) |
| `AudioSource` | `playOnAwake = false` |
| Tag / Layer | `Enemy` tag, `Enemies` layer — auto-registered in TagManager if absent |
| Phase children | Boss prefabs get `Phase_1 … Phase_N` child GameObjects (count from `bossPhases`) |

> Generated prefabs are starting points. Wire sprites, `AnimatorController` assets, and gameplay AI scripts before shipping.

**ODIN Inspector compatibility**
When `ODIN_INSPECTOR` is defined, `EnemyManagerEditor` inherits `OdinEditor` so the full ODIN property tree (including `[OdinSerialize]` fields) is rendered. The prefab generation helpers are plain static classes and are completely ODIN-independent.


## JSON Editor Window

Open via **JSON Editors → Enemy Manager** in the Unity menu bar, or via the **Open JSON Editor** button in the EnemyManager Inspector.

Edits enemies and waves stored as per-entity files in `StreamingAssets/enemies/` (`EnemyDefinition`) and `StreamingAssets/waves/` (`WaveDefinition`).

| Action | Result |
| ------ | ------ |
| **Load** | Reads all `*.json` from `StreamingAssets/enemies/` and `StreamingAssets/waves/`; creates missing folders automatically |
| **Edit** | Add / remove / reorder enemies and waves using the Inspector list |
| **Save** | Writes each enemy as `<id>.json` to `StreamingAssets/enemies/` and each wave as `<id>.json` to `StreamingAssets/waves/`; entries without an `id` are skipped. Calls `AssetDatabase.Refresh()` |

With **ODIN_INSPECTOR** active, lists use Odin's enhanced drawer (drag-to-sort, collapsible entries).


## Dependencies

| Dependency | Required | Notes |
| ---------- | -------- | ----- |
| Unity 2022.3+ | ✓ | |
| AiManager | optional | Required when `ENEMYMANAGER_AIM` is defined |
| SaveManager | optional | Required when `ENEMYMANAGER_SM` is defined |
| CutsceneManager | optional | Required when `ENEMYMANAGER_CSM` is defined |
| StateManager | optional | Required when `ENEMYMANAGER_STM` is defined |
| MapLoaderFramework | optional | Required when `ENEMYMANAGER_MLF` is defined |
| EventManager | optional | Required when `ENEMYMANAGER_EM` is defined |
| Odin Inspector | optional | Required when `ODIN_INSPECTOR` is defined |


## Repository

[https://github.com/RolandKaechele/EnemyManager](https://github.com/RolandKaechele/EnemyManager)
