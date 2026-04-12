using System;
using System.Collections.Generic;
using UnityEngine;

namespace EnemyManager.Runtime
{
    // -------------------------------------------------------------------------
    // EnemyType
    // -------------------------------------------------------------------------

    /// <summary>Broad classification for enemy entities.</summary>
    public enum EnemyType
    {
        /// <summary>A regular enemy unit.</summary>
        Standard,

        /// <summary>Part of a group/swarm with collective behaviour.</summary>
        Swarm,

        /// <summary>A named, powerful elite enemy.</summary>
        Elite,

        /// <summary>A boss with multiple phases.</summary>
        Boss
    }

    // -------------------------------------------------------------------------
    // EnemyStats
    // -------------------------------------------------------------------------

    /// <summary>Base numeric stats for an enemy. Extend by subclassing or adding fields.</summary>
    [Serializable]
    public class EnemyStats
    {
        [Tooltip("Maximum health points.")]
        public int health = 50;

        [Tooltip("Movement speed.")]
        public float speed = 3f;

        [Tooltip("Base attack power.")]
        public int attackPower = 8;

        [Tooltip("Score points awarded when this enemy is defeated.")]
        public int pointValue = 100;
    }

    // -------------------------------------------------------------------------
    // EnemyDefinition
    // -------------------------------------------------------------------------

    /// <summary>
    /// Defines a single enemy type in the roster.
    /// Serializable so it can be defined in the Inspector and loaded from JSON.
    /// </summary>
    [Serializable]
    public class EnemyDefinition
    {
        [Tooltip("Unique identifier (e.g. 'green_spider').")]
        public string id;

        [Tooltip("Display name shown in the UI.")]
        public string displayName;

        [Tooltip("Classification used to group enemies in the AiManager.")]
        public EnemyType type = EnemyType.Standard;

        public EnemyStats stats;

        [Tooltip("Path to the prefab inside a Resources/ folder (e.g. 'Enemies/GreenSpider').")]
        public string prefabResourcePath;

        [Tooltip("AI behavior id from AiManager (e.g. 'patrol_guard'). Leave empty to skip AiManager registration.")]
        public string aiBehaviorId;

        [Tooltip("If true, this enemy always respawns between saves (e.g. respawning crawler).")]
        public bool alwaysRespawns = false;

        [Tooltip("Number of boss phases. Only relevant for EnemyType.Boss.")]
        public int bossPhases = 1;
    }

    // -------------------------------------------------------------------------
    // WaveEntry
    // -------------------------------------------------------------------------

    /// <summary>A single enemy entry inside a WaveDefinition.</summary>
    [Serializable]
    public class WaveEntry
    {
        [Tooltip("Enemy definition id.")]
        public string enemyId;

        [Tooltip("Number of this enemy type to spawn in the wave.")]
        public int count = 1;
    }

    // -------------------------------------------------------------------------
    // WaveDefinition
    // -------------------------------------------------------------------------

    /// <summary>Defines a wave of enemies to spawn sequentially.</summary>
    [Serializable]
    public class WaveDefinition
    {
        [Tooltip("Unique identifier for this wave (e.g. 'wave_01').")]
        public string id;

        [Tooltip("Human-readable name (e.g. 'Spider Assault').")]
        public string displayName;

        [Tooltip("List of enemy types and counts for this wave.")]
        public List<WaveEntry> enemies = new List<WaveEntry>();

        [Tooltip("Seconds between individual enemy spawns within the wave.")]
        public float timeBetweenSpawns = 1f;

        [Tooltip("Whether to loop the wave until manually aborted.")]
        public bool loop = false;
    }

    // -------------------------------------------------------------------------
    // EnemyInstanceRecord
    // -------------------------------------------------------------------------

    /// <summary>A live record of a spawned enemy instance.</summary>
    [Serializable]
    public class EnemyInstanceRecord
    {
        [Tooltip("Unique instance id generated at spawn time (e.g. 'green_spider_3').")]
        public string instanceId;

        [Tooltip("Definition id of the enemy type.")]
        public string enemyId;

        [Tooltip("The spawned GameObject.")]
        public GameObject gameObject;

        [Tooltip("Whether this instance has been defeated.")]
        public bool defeated;
    }

    // -------------------------------------------------------------------------
    // JSON wrappers
    // -------------------------------------------------------------------------

    [Serializable]
    internal class EnemyRosterJson
    {
        public List<EnemyDefinition> enemies = new List<EnemyDefinition>();
    }

    [Serializable]
    internal class WaveRosterJson
    {
        public List<WaveDefinition> waves = new List<WaveDefinition>();
    }
}
