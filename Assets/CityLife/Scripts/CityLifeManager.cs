using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityLife
{
    /// <summary>
    /// Master coordinator for the City Life System.
    /// Attach this component to the same GameObject as CityGenerator.
    /// </summary>
    public class CityLifeManager : MonoBehaviour
    {
        public static CityLifeManager Instance { get; private set; }

        public enum CarSpawnMode
        {
            [Tooltip("Cars exclusively spawn at outer road terminal dead ends.")]
            DeadEndTerminalsOnly,
            [Tooltip("Cars spawn randomly distributed across all road driving lanes on the map.")]
            RandomOnRoads,
            [Tooltip("Initial spawn distributes cars randomly across the map; subsequent respawns enter from outer dead ends.")]
            Hybrid
        }

        [Header("Prefabs")]
        [SerializeField] private GameObject carPrefab;
        [Tooltip("Optional list of multiple vehicle prefab variations (Sedan, Taxi, Van, Truck). If assigned, cars are picked randomly from this array.")]
        [SerializeField] private GameObject[] carPrefabs;
        [SerializeField] private GameObject pedestrianPrefab;

        [Header("Car Spawning")]
        [SerializeField] private CarSpawnMode carSpawnMode = CarSpawnMode.Hybrid;

        [Header("Population")]
        [SerializeField] private int  maxCars         = 15;
        [SerializeField] private int  maxPedestrians  = 20;
        [SerializeField] private bool spawnOnGenerate = true;

        [Header("Car Settings")]
        [SerializeField] private float carMinSpeed  =  5f;
        [SerializeField] private float carMaxSpeed  = 12f;
        [SerializeField] private float respawnDelay =  2f;
        [SerializeField] private Color[] carColors  = new Color[]
        {
            new Color(0.85f, 0.15f, 0.10f),
            new Color(0.10f, 0.30f, 0.85f),
            new Color(0.95f, 0.80f, 0.10f),
            new Color(0.10f, 0.70f, 0.20f),
            new Color(0.85f, 0.45f, 0.10f),
            new Color(0.92f, 0.92f, 0.92f),
        };

        [Header("Pedestrian Settings")]
        [SerializeField] private float pedestrianMinSpeed = 0.8f;
        [SerializeField] private float pedestrianMaxSpeed = 1.5f;
        [SerializeField] private Color[] pedestrianColors = new Color[]
        {
            new Color(0.95f, 0.65f, 0.45f),
            new Color(0.40f, 0.40f, 0.75f),
            new Color(0.75f, 0.40f, 0.40f),
            new Color(0.40f, 0.75f, 0.45f),
            new Color(0.60f, 0.55f, 0.30f),
        };

        private CityWaypointGraph  _graph;
        private System.Random      _rng;
        private int                _activeCars;
        private bool               _initialized;
        private List<RoadNodeData> _cachedRoadData;
        private float              _cachedCellSize;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            if (CityObjectPool.Instance == null && GetComponent<CityObjectPool>() == null)
                gameObject.AddComponent<CityObjectPool>();
        }

        private void Start()
        {
            // When entering Play Mode, if not yet initialized, grab road data and activate traffic!
            if (!_initialized)
            {
                var cityGen = GetComponent<SevenDays.World.CityGenerator>() ?? FindAnyObjectByType<SevenDays.World.CityGenerator>();
                if (cityGen != null)
                {
                    var roads = cityGen.GetRoadData();
                    if (roads != null && roads.Count > 0)
                    {
                        Initialize(roads, cityGen.CellSize);
                    }
                }
            }
        }

        /// <summary>
        /// Entry point called by CityGenerator after city generation.
        /// </summary>
        public void Initialize(List<RoadNodeData> roadData, float cellSize)
        {
            _cachedRoadData = roadData;
            _cachedCellSize = cellSize;

            if (!spawnOnGenerate) return;

            Clear();

            _rng         = new System.Random(System.DateTime.Now.Millisecond);
            _activeCars  = 0;
            _initialized = false;

            // 1. Build sidewalk & lane waypoint graphs
            _graph = new CityWaypointGraph();
            _graph.Build(roadData, cellSize);

            if (_graph.SidewalkWaypoints.Count == 0 && _graph.LaneWaypoints.Count == 0)
            {
                Debug.LogWarning("[CityLifeManager] No road waypoints generated — was the city generated first?");
                return;
            }

            // In Edit Mode, build and validate graph; spawn live agents in Play Mode so they are active and moving
            if (!Application.isPlaying)
            {
                Debug.Log($"[CityLifeManager] Road graph built ({_graph.SidewalkWaypoints.Count} sidewalks, {_graph.LaneWaypoints.Count} lanes). Traffic will start automatically when you press Play (Start).");
                return;
            }

            // 2. Ensure object pool exists
            var pool = CityObjectPool.Instance;
            if (pool == null)
            {
                pool = GetComponent<CityObjectPool>() ?? gameObject.AddComponent<CityObjectPool>();
            }

            // 3. Create pools
            CreatePools();

            // 4. Spawn agents directly at their respective sidewalk/lane waypoints
            SpawnAllCars();
            SpawnAllPedestrians();

            _initialized = true;
            Debug.Log($"[CityLifeManager] Initialized in Play Mode: {maxCars} cars, {maxPedestrians} pedestrians active on {_graph.SidewalkWaypoints.Count} sidewalk points & {_graph.LaneWaypoints.Count} lane points.");
        }

        /// <summary>Public method to regenerate pedestrians and cars.</summary>
        public void RegenerateLife()
        {
            if (_cachedRoadData != null)
            {
                Initialize(_cachedRoadData, _cachedCellSize);
            }
        }

        /// <summary>Called before regenerating city.</summary>
        public void Clear()
        {
            StopAllCoroutines();
            _activeCars  = 0;
            _initialized = false;

            if (CityObjectPool.Instance != null)
            {
                CityObjectPool.Instance.ClearAll();
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("Pool_"))
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }

            var existingCars = FindObjectsByType<CarController>();
            foreach (var c in existingCars)
            {
                if (Application.isPlaying) Destroy(c.gameObject);
                else DestroyImmediate(c.gameObject);
            }

            var existingPeds = FindObjectsByType<PedestrianController>();
            foreach (var p in existingPeds)
            {
                if (Application.isPlaying) Destroy(p.gameObject);
                else DestroyImmediate(p.gameObject);
            }
        }

        private void CreatePools()
        {
            var pool = CityObjectPool.Instance;
            if (pool == null)
            {
                pool = GetComponent<CityObjectPool>() ?? gameObject.AddComponent<CityObjectPool>();
            }

            Transform carParent = transform.Find("Pool_Cars");
            if (carParent != null)
            {
                if (Application.isPlaying) Destroy(carParent.gameObject);
                else DestroyImmediate(carParent.gameObject);
            }
            carParent = new GameObject("Pool_Cars").transform;
            carParent.SetParent(transform);

            Transform pedParent = transform.Find("Pool_Pedestrians");
            if (pedParent != null)
            {
                if (Application.isPlaying) Destroy(pedParent.gameObject);
                else DestroyImmediate(pedParent.gameObject);
            }
            pedParent = new GameObject("Pool_Pedestrians").transform;
            pedParent.SetParent(transform);

            if (carPrefabs != null && carPrefabs.Length > 0)
            {
                int perPrefab = Mathf.Max(3, (maxCars + 5) / carPrefabs.Length);
                for (int i = 0; i < carPrefabs.Length; i++)
                {
                    if (carPrefabs[i] != null)
                        pool.CreatePool($"{CarController.PoolTag}_{i}", carPrefabs[i], perPrefab, carParent);
                }
            }
            else if (carPrefab != null)
            {
                pool.CreatePool(CarController.PoolTag, carPrefab, maxCars + 5, carParent);
            }
            else
            {
                Debug.LogWarning("[CityLifeManager] Car prefab not assigned!");
            }

            if (pedestrianPrefab != null)
                pool.CreatePool(PedestrianController.PoolTag, pedestrianPrefab, maxPedestrians + 5, pedParent);
            else
                Debug.LogWarning("[CityLifeManager] Pedestrian prefab not assigned!");
        }

        private void SpawnAllCars()
        {
            if (carPrefab == null && (carPrefabs == null || carPrefabs.Length == 0)) return;
            if (_graph == null) return;

            if (carSpawnMode == CarSpawnMode.DeadEndTerminalsOnly)
            {
                if (_graph.DeadEndSpawnPoints.Count > 0)
                {
                    var deadEnds = new List<LaneWaypoint>(_graph.DeadEndSpawnPoints);
                    for (int i = deadEnds.Count - 1; i > 0; i--)
                    {
                        int j = _rng.Next(i + 1);
                        (deadEnds[i], deadEnds[j]) = (deadEnds[j], deadEnds[i]);
                    }

                    int count = Mathf.Min(maxCars, deadEnds.Count);
                    for (int i = 0; i < count; i++)
                    {
                        SpawnCar(deadEnds[i]);
                    }
                }
            }
            else
            {
                // In RandomOnRoads or Hybrid mode:
                // Distribute across all driving lanes on the map
                var spawnCandidates = new List<LaneWaypoint>(_graph.LaneSpawnPoints);
                if (spawnCandidates.Count == 0) spawnCandidates = new List<LaneWaypoint>(_graph.LaneWaypoints);

                for (int i = spawnCandidates.Count - 1; i > 0; i--)
                {
                    int j = _rng.Next(i + 1);
                    (spawnCandidates[i], spawnCandidates[j]) = (spawnCandidates[j], spawnCandidates[i]);
                }

                int spawned = 0;
                for (int i = 0; i < spawnCandidates.Count && spawned < maxCars; i++)
                {
                    var candidate = spawnCandidates[i];
                    if (candidate.Connections.Count > 0 && !IsSpawnPointOccupied(candidate.Position, 6f))
                    {
                        SpawnCar(candidate);
                        spawned++;
                    }
                }
            }
        }

        private void SpawnAllPedestrians()
        {
            if (pedestrianPrefab == null || _graph == null) return;
            for (int i = 0; i < maxPedestrians; i++) SpawnPedestrian();
        }

        private void SpawnCar(LaneWaypoint preferredStartNode = null)
        {
            LaneWaypoint startNode = preferredStartNode;
            if (startNode == null)
            {
                // In DeadEndTerminalsOnly or Hybrid mode, try finding an unoccupied dead-end first
                if (carSpawnMode != CarSpawnMode.RandomOnRoads && _graph.DeadEndSpawnPoints.Count > 0)
                {
                    for (int attempt = 0; attempt < 12; attempt++)
                    {
                        var candidate = _graph.DeadEndSpawnPoints[_rng.Next(_graph.DeadEndSpawnPoints.Count)];
                        if (!IsSpawnPointOccupied(candidate.Position, 6f))
                        {
                            startNode = candidate;
                            break;
                        }
                    }
                }

                // Fallback to random road lane if in RandomOnRoads mode, or all dead ends occupied, or none exist
                if (startNode == null)
                {
                    for (int attempt = 0; attempt < 20; attempt++)
                    {
                        var candidate = _graph.GetRandomLaneWaypoint(_rng);
                        if (candidate != null && candidate.Connections.Count > 0 && !IsSpawnPointOccupied(candidate.Position, 6f))
                        {
                            startNode = candidate;
                            break;
                        }
                    }
                }
            }

            if (startNode == null) return;

            var nextNode = _graph.GetNextLaneWaypoint(startNode, null, _rng);
            if (nextNode == null) return;

            Vector3 moveDir = (nextNode.Position - startNode.Position).normalized;
            if (moveDir.sqrMagnitude < 0.001f) moveDir = Vector3.forward;

            // Spawn directly in the driving lane on the right side
            Vector3 spawnPos = startNode.Position;
            spawnPos.y = 0f;

            // Support multi-car prefab pooling
            string poolTag = CarController.PoolTag;
            if (carPrefabs != null && carPrefabs.Length > 0)
            {
                int pIdx = _rng.Next(carPrefabs.Length);
                poolTag = $"{CarController.PoolTag}_{pIdx}";
            }

            var obj = CityObjectPool.Instance.Get(poolTag, spawnPos, Quaternion.LookRotation(moveDir));
            if (obj == null) return;

            var car = obj.GetComponent<CarController>();
            if (car == null) return;

            float speed = Mathf.Lerp(carMinSpeed, carMaxSpeed, (float)_rng.NextDouble());
            car.Activate(_graph, _rng, startNode, nextNode, speed, poolTag);

            if (carColors.Length > 0)
                car.SetBodyColor(carColors[_rng.Next(carColors.Length)]);

            _activeCars++;
        }

        private bool IsSpawnPointOccupied(Vector3 position, float checkRadius)
        {
            var cars = FindObjectsByType<CarController>();
            float sqrRadius = checkRadius * checkRadius;
            foreach (var c in cars)
            {
                if (c.gameObject.activeInHierarchy && (c.transform.position - position).sqrMagnitude < sqrRadius)
                    return true;
            }
            return false;
        }

        private void SpawnPedestrian()
        {
            var startNode = _graph.GetRandomSidewalkWaypoint(_rng);
            if (startNode == null) return;

            var nextNode = _graph.GetNextSidewalkWaypoint(startNode, null, _rng);
            if (nextNode == null) return;

            Vector3 moveDir = (nextNode.Position - startNode.Position).normalized;
            if (moveDir.sqrMagnitude < 0.001f) moveDir = Vector3.forward;

            // Spawn directly on the sidewalk
            Vector3 spawnPos = startNode.Position;
            spawnPos.y = 0f;

            var obj = CityObjectPool.Instance.Get(PedestrianController.PoolTag, spawnPos, Quaternion.LookRotation(moveDir));
            if (obj == null) return;

            var ped = obj.GetComponent<PedestrianController>();
            if (ped == null) return;

            float speed = Mathf.Lerp(pedestrianMinSpeed, pedestrianMaxSpeed, (float)_rng.NextDouble());
            ped.Activate(_graph, _rng, startNode, nextNode, speed);

            if (pedestrianColors.Length > 0)
                ped.SetBodyColor(pedestrianColors[_rng.Next(pedestrianColors.Length)]);
        }

        public void OnCarReturnedToPool()
        {
            _activeCars = Mathf.Max(0, _activeCars - 1);
            if (_initialized && _activeCars < maxCars)
                StartCoroutine(RespawnCarDelayed());
        }

        private IEnumerator RespawnCarDelayed()
        {
            yield return new WaitForSeconds(respawnDelay);
            if (_activeCars < maxCars) SpawnCar();
        }
    }
}
