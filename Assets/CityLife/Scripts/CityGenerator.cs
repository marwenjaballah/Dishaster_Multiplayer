using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using CityLife;

namespace SevenDays.World
{
    /// <summary>
    /// Procedurally generates a city layout using Pandazole City/Town Pack assets.
    /// Respects building bounds, creates road networks, and places decorative props.
    /// </summary>
    public class CityGenerator : MonoBehaviour
    {
        [Header("Generation Settings")]
        [SerializeField] private int gridWidth = 30;
        [SerializeField] private int gridHeight = 30;
        [SerializeField] private float cellSize = 10f; // Pandazole roads require 10-unit spacing for seamless connection
        [SerializeField] private int seed = 12345;
        [SerializeField] private bool generateOnStart = false;

        [Header("Road Settings")]
        [SerializeField] private float roadWidth = 8f;
        [SerializeField] private int mainRoadInterval = 3; // Every N blocks

        [Header("Block & Lot Variation")]
        [Tooltip("Chance (0 to 0.6) to merge adjacent square blocks into a continuous rectangle (vertical or horizontal)")]
        [SerializeField] [Range(0f, 0.6f)] private float rectangularBlockChance = 0.35f;
        [Tooltip("Chance (0 to 1.0) that a rectangular block is designated as a full park rather than a building lot")]
        [SerializeField] [Range(0f, 1f)]   private float rectangularParkChance  = 0.40f;

        [Header("Natural Areas")]
        [SerializeField] [Range(0f, 0.3f)] private float parkChance = 0.15f; // Chance for a block to become a park/forest
        [SerializeField] private int minParkSize = 2; // Minimum park cluster size
        [SerializeField] private int maxParkSize = 4; // Maximum park cluster size

        [Header("Building Prefabs")]
        [SerializeField] private GameObject[] residentialBuildings;
        [SerializeField] private GameObject[] commercialBuildings;
        [SerializeField] private GameObject[] companyBuildings;
        [SerializeField] private GameObject[] motelBuildings;

        [Header("Road Prefabs")]
        [SerializeField] private GameObject roadStraight;
        [SerializeField] private GameObject roadCorner;
        [SerializeField] private GameObject roadCross;
        [SerializeField] private GameObject roadTJunction; // Use Env_Road_Side_03
        [SerializeField] private GameObject roadEnd;

        [Header("Prop Prefabs")]
        [SerializeField] private GameObject[] trees;
        [SerializeField] private GameObject[] streetSigns;
        [SerializeField] private GameObject[] trashCans;
        [SerializeField] private GameObject[] plants;

        [Header("Density Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float buildingDensity = 1.0f;
        [Range(0f, 1f)]
        [SerializeField] private float treeDensity = 0.3f;
        [Range(0f, 1f)]
        [SerializeField] private float propDensity = 0.2f;

        [Header("Building Distribution")]
        [Range(0f, 1f)]
        [SerializeField] private float residentialWeight = 0.5f;
        [Range(0f, 1f)]
        [SerializeField] private float commercialWeight = 0.3f;
        [Range(0f, 1f)]
        [SerializeField] private float companyWeight = 0.15f;
        [Range(0f, 1f)]
        [SerializeField] private float motelWeight = 0.05f;

        [Header("City Life")]
        [SerializeField] private CityLifeManager cityLifeManager;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;

        private System.Random random;
        private CityCell[,] cityGrid;
        private Transform cityRoot;

        private class RectangularBlock
        {
            public int minX, maxX;
            public int minZ, maxZ;
            public bool isPark;
        }

        private readonly HashSet<Vector2Int> _skippedRoadCells = new();
        private readonly List<RectangularBlock> _rectangularBlocks = new();

        public enum CellType
        {
            Empty,
            Road,
            Building,
            Park
        }

        public class CityCell
        {
            public CellType type;
            public GameObject instance;
            public Vector3 position;
            public Bounds bounds;
            public bool hasNorthRoad;
            public bool hasSouthRoad;
            public bool hasEastRoad;
            public bool hasWestRoad;
        }

        [SerializeField, HideInInspector] private List<RoadNodeData> serializedRoadData = new();

        public CityCell[,] GetCityGrid() => cityGrid;
        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public float CellSize => cellSize;

        public List<RoadNodeData> GetRoadData()
        {
            if (cityGrid != null)
                return BuildRoadData();
            if (serializedRoadData != null && serializedRoadData.Count > 0)
                return serializedRoadData;
            return RebuildRoadDataFromScene();
        }

        private void Start()
        {
            if (generateOnStart)
            {
                GenerateCity();
            }
            else
            {
                // City was already generated in Edit Mode — start City Life for the existing city!
                var roads = GetRoadData();
                if (roads != null && roads.Count > 0)
                {
                    if (cityLifeManager == null)
                        cityLifeManager = GetComponent<CityLifeManager>() ?? FindAnyObjectByType<CityLifeManager>();
                    if (cityLifeManager != null)
                        cityLifeManager.Initialize(roads, cellSize);
                }
            }
        }

        [ContextMenu("Generate City")]
        public void GenerateCity()
        {
            ClearCity();
            InitializeGenerator();
            ValidateRoadPrefabs();
            GenerateGrid();
            PlanRoadsAndBlocks();
            GenerateParks();
            PlaceRoads();
            PlaceBuildings();
            PlaceProps();

            // Cache serialized road data so City Life works seamlessly when entering Play Mode
            serializedRoadData = BuildRoadData();

            // City Life — spawn pedestrians and cars
            if (cityLifeManager == null)
                cityLifeManager = GetComponent<CityLifeManager>() ?? FindAnyObjectByType<CityLifeManager>();
            if (cityLifeManager != null)
                cityLifeManager.Initialize(serializedRoadData, cellSize);

            Debug.Log($"City generation complete! Grid: {gridWidth}x{gridHeight}, Seed: {seed}, Rectangular blocks: {_rectangularBlocks.Count}");
        }

        [ContextMenu("Clear City")]
        public void ClearCity()
        {
            serializedRoadData.Clear();
            _skippedRoadCells.Clear();
            _rectangularBlocks.Clear();
            if (cityLifeManager == null)
                cityLifeManager = GetComponent<CityLifeManager>() ?? FindAnyObjectByType<CityLifeManager>();
            cityLifeManager?.Clear();

            if (cityRoot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(cityRoot.gameObject);
                }
                else
                {
                    DestroyImmediate(cityRoot.gameObject);
                }
            }

            cityGrid = null;
        }

        [ContextMenu("Validate Road Prefabs")]
        private void ValidateRoadPrefabs()
        {
            const float expectedSize = 10f; // Updated to match actual working grid size
            const float tolerance = 0.5f;

            void CheckPrefab(GameObject prefab, string name)
            {
                if (prefab == null) return;

                Bounds bounds = GetPrefabBounds(prefab);
                float maxDimension = Mathf.Max(bounds.size.x, bounds.size.z);

                if (maxDimension > expectedSize + tolerance)
                {
                    Debug.LogWarning($"[CityGenerator] {name} '{prefab.name}' is larger than expected grid size " +
                                   $"({maxDimension:F2} > {expectedSize}). Roads may overlap. Consider adjusting cellSize or using different prefab.");
                }
            }

            CheckPrefab(roadStraight, "Road Straight");
            CheckPrefab(roadCorner, "Road Corner");
            CheckPrefab(roadCross, "Road Cross");
            CheckPrefab(roadTJunction, "Road T-Junction");
            CheckPrefab(roadEnd, "Road End");

            Debug.Log($"[CityGenerator] Road validation complete. Grid cell size: {cellSize} units (Tested optimal: 10 units)");
        }

        private void InitializeGenerator()
        {
            random = new System.Random(seed);
            cityGrid = new CityCell[gridWidth, gridHeight];

            // Create root container
            cityRoot = new GameObject("GeneratedCity").transform;
            cityRoot.SetParent(transform);
            cityRoot.localPosition = Vector3.zero;
        }

        private void GenerateGrid()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    cityGrid[x, z] = new CityCell
                    {
                        type = CellType.Empty,
                        position = new Vector3(x * cellSize, 0, z * cellSize)
                    };
                }
            }
        }

        private void PlanRoadsAndBlocks()
        {
            _skippedRoadCells.Clear();
            _rectangularBlocks.Clear();

            int interval = Mathf.Max(2, mainRoadInterval);
            int numBlocksX = (gridWidth - 1) / interval;
            int numBlocksZ = (gridHeight - 1) / interval;

            if (numBlocksX > 0 && numBlocksZ > 0 && rectangularBlockChance > 0f)
            {
                bool[,] blockMerged = new bool[numBlocksX, numBlocksZ];
                HashSet<int> mergedRowDividers = new();
                HashSet<int> mergedColDividers = new();

                List<Vector2Int> blocks = new List<Vector2Int>();
                for (int bx = 0; bx < numBlocksX; bx++)
                    for (int bz = 0; bz < numBlocksZ; bz++)
                        blocks.Add(new Vector2Int(bx, bz));

                // Shuffle blocks for random variety
                for (int i = blocks.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    (blocks[i], blocks[j]) = (blocks[j], blocks[i]);
                }

                foreach (var b in blocks)
                {
                    int bx = b.x;
                    int bz = b.y;
                    if (blockMerged[bx, bz]) continue;

                    if (random.NextDouble() >= rectangularBlockChance) continue;

                    bool preferVertical = random.Next(2) == 0;
                    bool merged = false;

                    if (preferVertical)
                    {
                        merged = TryMergeVertical(bx, bz, interval, numBlocksX, numBlocksZ, blockMerged, mergedRowDividers);
                        if (!merged)
                            merged = TryMergeHorizontal(bx, bz, interval, numBlocksX, numBlocksZ, blockMerged, mergedColDividers);
                    }
                    else
                    {
                        merged = TryMergeHorizontal(bx, bz, interval, numBlocksX, numBlocksZ, blockMerged, mergedColDividers);
                        if (!merged)
                            merged = TryMergeVertical(bx, bz, interval, numBlocksX, numBlocksZ, blockMerged, mergedRowDividers);
                    }
                }
            }

            // Mark road cells on the grid, skipping omitted segments inside rectangular blocks
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    bool isMainRoadX = (x % mainRoadInterval == 0);
                    bool isMainRoadZ = (z % mainRoadInterval == 0);

                    if (isMainRoadX || isMainRoadZ)
                    {
                        if (_skippedRoadCells.Contains(new Vector2Int(x, z)))
                        {
                            continue;
                        }

                        cityGrid[x, z].type = CellType.Road;
                    }
                }
            }
        }

        private bool TryMergeVertical(int bx, int bz, int interval, int numBlocksX, int numBlocksZ,
                                      bool[,] blockMerged, HashSet<int> mergedRowDividers)
        {
            if (bz + 1 >= numBlocksZ) return false;
            if (blockMerged[bx, bz] || blockMerged[bx, bz + 1]) return false;

            int roadZ = (bz + 1) * interval;
            if (roadZ >= gridHeight) return false;

            int dividerKey = (bx << 16) ^ roadZ;
            int leftKey    = ((bx - 1) << 16) ^ roadZ;
            int rightKey   = ((bx + 1) << 16) ^ roadZ;
            if (mergedRowDividers.Contains(leftKey) || mergedRowDividers.Contains(rightKey))
                return false;

            int minX = bx * interval + 1;
            int maxX = (bx + 1) * interval - 1;
            if (maxX >= gridWidth) return false;

            for (int x = minX; x <= maxX; x++)
            {
                _skippedRoadCells.Add(new Vector2Int(x, roadZ));
            }

            blockMerged[bx, bz]     = true;
            blockMerged[bx, bz + 1] = true;
            mergedRowDividers.Add(dividerKey);

            int minZ = bz * interval + 1;
            int maxZ = (bz + 2) * interval - 1;

            bool isPark = random.NextDouble() < rectangularParkChance;
            _rectangularBlocks.Add(new RectangularBlock
            {
                minX = minX,
                maxX = maxX,
                minZ = minZ,
                maxZ = maxZ,
                isPark = isPark
            });

            return true;
        }

        private bool TryMergeHorizontal(int bx, int bz, int interval, int numBlocksX, int numBlocksZ,
                                        bool[,] blockMerged, HashSet<int> mergedColDividers)
        {
            if (bx + 1 >= numBlocksX) return false;
            if (blockMerged[bx, bz] || blockMerged[bx + 1, bz]) return false;

            int roadX = (bx + 1) * interval;
            if (roadX >= gridWidth) return false;

            int dividerKey = (roadX << 16) ^ bz;
            int downKey    = (roadX << 16) ^ (bz - 1);
            int upKey      = (roadX << 16) ^ (bz + 1);
            if (mergedColDividers.Contains(downKey) || mergedColDividers.Contains(upKey))
                return false;

            int minZ = bz * interval + 1;
            int maxZ = (bz + 1) * interval - 1;
            if (maxZ >= gridHeight) return false;

            for (int z = minZ; z <= maxZ; z++)
            {
                _skippedRoadCells.Add(new Vector2Int(roadX, z));
            }

            blockMerged[bx, bz]     = true;
            blockMerged[bx + 1, bz] = true;
            mergedColDividers.Add(dividerKey);

            int minX = bx * interval + 1;
            int maxX = (bx + 2) * interval - 1;

            bool isPark = random.NextDouble() < rectangularParkChance;
            _rectangularBlocks.Add(new RectangularBlock
            {
                minX = minX,
                maxX = maxX,
                minZ = minZ,
                maxZ = maxZ,
                isPark = isPark
            });

            return true;
        }

        private void GenerateParks()
        {
            // 1. Designated rectangular parks (span entire rectangular block)
            foreach (var rb in _rectangularBlocks)
            {
                if (rb.isPark)
                {
                    for (int x = rb.minX; x <= rb.maxX; x++)
                    {
                        for (int z = rb.minZ; z <= rb.maxZ; z++)
                        {
                            if (x >= 0 && x < gridWidth && z >= 0 && z < gridHeight)
                            {
                                if (cityGrid[x, z].type == CellType.Empty)
                                {
                                    cityGrid[x, z].type = CellType.Park;
                                }
                            }
                        }
                    }
                }
            }

            // 2. Standard park clusters from seeds in remaining empty blocks
            List<Vector2Int> parkSeeds = new List<Vector2Int>();

            for (int x = 1; x < gridWidth - 1; x++)
            {
                for (int z = 1; z < gridHeight - 1; z++)
                {
                    if (cityGrid[x, z].type == CellType.Empty && random.NextDouble() < parkChance)
                    {
                        parkSeeds.Add(new Vector2Int(x, z));
                    }
                }
            }

            // Grow parks from seeds
            foreach (var seed in parkSeeds)
            {
                int parkSize = random.Next(minParkSize, maxParkSize + 1);
                GrowPark(seed.x, seed.y, parkSize);
            }
        }

        private void GrowPark(int startX, int startZ, int size)
        {
            Queue<Vector2Int> toProcess = new Queue<Vector2Int>();
            HashSet<Vector2Int> processed = new HashSet<Vector2Int>();

            toProcess.Enqueue(new Vector2Int(startX, startZ));
            int cellsPlaced = 0;

            while (toProcess.Count > 0 && cellsPlaced < size)
            {
                Vector2Int current = toProcess.Dequeue();

                if (processed.Contains(current)) continue;
                processed.Add(current);

                int x = current.x;
                int z = current.y;

                // Check bounds and if cell is available
                if (x < 0 || x >= gridWidth || z < 0 || z >= gridHeight) continue;
                if (cityGrid[x, z].type != CellType.Empty) continue;

                // Mark as park
                cityGrid[x, z].type = CellType.Park;
                cellsPlaced++;

                // Add neighbors for expansion
                if (cellsPlaced < size && random.NextDouble() > 0.3f) // 70% chance to expand
                {
                    toProcess.Enqueue(new Vector2Int(x + 1, z));
                    toProcess.Enqueue(new Vector2Int(x - 1, z));
                    toProcess.Enqueue(new Vector2Int(x, z + 1));
                    toProcess.Enqueue(new Vector2Int(x, z - 1));
                }
            }
        }

        private void PlaceRoads()
        {
            Transform roadParent = new GameObject("Roads").transform;
            roadParent.SetParent(cityRoot);

            // Determine road connections
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    if (cityGrid[x, z].type == CellType.Road)
                    {
                        cityGrid[x, z].hasNorthRoad = z < gridHeight - 1 && cityGrid[x, z + 1].type == CellType.Road;
                        cityGrid[x, z].hasSouthRoad = z > 0 && cityGrid[x, z - 1].type == CellType.Road;
                        cityGrid[x, z].hasEastRoad = x < gridWidth - 1 && cityGrid[x + 1, z].type == CellType.Road;
                        cityGrid[x, z].hasWestRoad = x > 0 && cityGrid[x - 1, z].type == CellType.Road;
                    }
                }
            }

            // Place road prefabs
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    if (cityGrid[x, z].type == CellType.Road)
                    {
                        GameObject roadPrefab = SelectRoadPrefab(cityGrid[x, z]);
                        if (roadPrefab != null)
                        {
                            Vector3 position = cityGrid[x, z].position;
                            Quaternion rotation = GetRoadRotation(cityGrid[x, z]);

                            // Debug T-junctions only
                            if (roadPrefab == roadTJunction)
                            {
                                string roads = $"N:{cityGrid[x, z].hasNorthRoad} S:{cityGrid[x, z].hasSouthRoad} E:{cityGrid[x, z].hasEastRoad} W:{cityGrid[x, z].hasWestRoad}";
                                Debug.Log($"[T-Junction] Road_{x}_{z} - Connections: {roads} - Rotation: {rotation.eulerAngles.y}°");
                            }

                            GameObject road = Instantiate(roadPrefab, position, rotation, roadParent);
                            road.name = $"Road_{x}_{z}";
                            cityGrid[x, z].instance = road;
                        }
                    }
                }
            }
        }

        private GameObject SelectRoadPrefab(CityCell cell)
        {
            int connections = 0;
            if (cell.hasNorthRoad) connections++;
            if (cell.hasSouthRoad) connections++;
            if (cell.hasEastRoad) connections++;
            if (cell.hasWestRoad) connections++;

            switch (connections)
            {
                case 4:
                    return roadCross;
                case 3:
                    return roadTJunction;
                case 2:
                    // Check if it's a straight road or corner
                    bool isStraight = (cell.hasNorthRoad && cell.hasSouthRoad) ||
                                     (cell.hasEastRoad && cell.hasWestRoad);
                    return isStraight ? roadStraight : roadCorner;
                case 1:
                    return roadEnd;
                default:
                    return roadStraight;
            }
        }

        private Quaternion GetRoadRotation(CityCell cell)
        {
            // Count connections
            int connections = 0;
            if (cell.hasNorthRoad) connections++;
            if (cell.hasSouthRoad) connections++;
            if (cell.hasEastRoad) connections++;
            if (cell.hasWestRoad) connections++;

            // Straight roads (2 connections, opposite sides)
            if (connections == 2)
            {
                if (cell.hasNorthRoad && cell.hasSouthRoad)
                    return Quaternion.Euler(0, 90, 0); // Vertical straight (rotated)
                if (cell.hasEastRoad && cell.hasWestRoad)
                    return Quaternion.Euler(0, 0, 0); // Horizontal straight

                // Corners (2 connections, adjacent sides)
                if (cell.hasNorthRoad && cell.hasEastRoad)
                    return Quaternion.Euler(0, 0, 0);
                if (cell.hasEastRoad && cell.hasSouthRoad)
                    return Quaternion.Euler(0, 90, 0);
                if (cell.hasSouthRoad && cell.hasWestRoad)
                    return Quaternion.Euler(0, 180, 0);
                if (cell.hasWestRoad && cell.hasNorthRoad)
                    return Quaternion.Euler(0, 270, 0);
            }

            // T-junctions (3 connections) - rotation based on which direction is MISSING a road
            if (connections == 3)
            {
                if (!cell.hasNorthRoad)
                    return Quaternion.Euler(0, 180, 0); // Opening faces north (no road to north)
                if (!cell.hasSouthRoad)
                    return Quaternion.Euler(0, 0, 0);   // Opening faces south (no road to south)
                if (!cell.hasEastRoad)
                    return Quaternion.Euler(0, 270, 0); // Opening faces east (no road to east)
                if (!cell.hasWestRoad)
                    return Quaternion.Euler(0, 90, 0);  // Opening faces west (no road to west)
            }

            // Ends (1 connection)
            if (connections == 1)
            {
                if (cell.hasNorthRoad)
                    return Quaternion.Euler(0, 270, 0); // Road connects to North
                if (cell.hasSouthRoad)
                    return Quaternion.Euler(0, 90, 0);  // Road connects to South
                if (cell.hasEastRoad)
                    return Quaternion.Euler(0, 0, 0);   // Road connects to East
                if (cell.hasWestRoad)
                    return Quaternion.Euler(0, 180, 0); // Road connects to West
            }

            return Quaternion.identity;
        }

        private void PlaceBuildings()
        {
            Transform buildingParent = new GameObject("Buildings").transform;
            buildingParent.SetParent(cityRoot);

            // Pre-collect all prefabs for fallback searches
            var allPrefabs = residentialBuildings
                .Concat(commercialBuildings)
                .Concat(companyBuildings)
                .Concat(motelBuildings)
                .Where(p => p != null)
                .Distinct()
                .ToArray();

            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    if (cityGrid[x, z].type != CellType.Empty) continue;

                    if (random.NextDouble() >= buildingDensity) continue;

                    // 1. Determine available empty space around (x, z)
                    bool canExpandX = (x + 1 < gridWidth && cityGrid[x + 1, z].type == CellType.Empty);
                    bool canExpandZ = (z + 1 < gridHeight && cityGrid[x, z + 1].type == CellType.Empty);
                    bool canExpand2x2 = canExpandX && canExpandZ && (cityGrid[x + 1, z + 1].type == CellType.Empty);

                    // 2. Select initial candidate building based on user weight preferences
                    GameObject chosenPrefab = SelectBuildingPrefab();
                    Bounds chosenBounds = chosenPrefab != null ? GetPrefabBounds(chosenPrefab) : default;
                    Quaternion chosenRotation = Quaternion.identity;
                    int spanX = 1;
                    int spanZ = 1;
                    bool fits = false;

                    if (chosenPrefab != null)
                    {
                        // Check if chosen prefab fits in 1x1 cell
                        if (CanFitInArea(chosenBounds, cellSize, cellSize, out chosenRotation))
                        {
                            fits = true;
                            spanX = 1;
                            spanZ = 1;
                        }
                        // If not, check if it fits in 2x1 (horizontal) and space is available
                        else if (canExpandX && CanFitInArea(chosenBounds, cellSize * 2f, cellSize, out chosenRotation))
                        {
                            fits = true;
                            spanX = 2;
                            spanZ = 1;
                        }
                        // Check if it fits in 1x2 (vertical) and space is available
                        else if (canExpandZ && CanFitInArea(chosenBounds, cellSize, cellSize * 2f, out chosenRotation))
                        {
                            fits = true;
                            spanX = 1;
                            spanZ = 2;
                        }
                        // Check if it fits in 2x2 and space is available
                        else if (canExpand2x2 && CanFitInArea(chosenBounds, cellSize * 2f, cellSize * 2f, out chosenRotation))
                        {
                            fits = true;
                            spanX = 2;
                            spanZ = 2;
                        }
                    }

                    // 3. Fallback Replacement: If the chosen prefab does NOT fit, find one that DOES fit!
                    if (!fits && allPrefabs.Length > 0)
                    {
                        // Prioritize multi-cell if available to create variety in large lots
                        if (canExpand2x2 && random.NextDouble() < 0.35f && TryFindFittingBuilding(allPrefabs, cellSize * 2f, cellSize * 2f, out chosenPrefab, out chosenBounds, out chosenRotation))
                        {
                            fits = true;
                            spanX = 2;
                            spanZ = 2;
                        }
                        else if (canExpandX && random.NextDouble() < 0.5f && TryFindFittingBuilding(allPrefabs, cellSize * 2f, cellSize, out chosenPrefab, out chosenBounds, out chosenRotation))
                        {
                            fits = true;
                            spanX = 2;
                            spanZ = 1;
                        }
                        else if (canExpandZ && random.NextDouble() < 0.5f && TryFindFittingBuilding(allPrefabs, cellSize, cellSize * 2f, out chosenPrefab, out chosenBounds, out chosenRotation))
                        {
                            fits = true;
                            spanX = 1;
                            spanZ = 2;
                        }
                        // Otherwise, guarantee a building that fits in this single cell
                        else if (TryFindFittingBuilding(allPrefabs, cellSize, cellSize, out chosenPrefab, out chosenBounds, out chosenRotation))
                        {
                            fits = true;
                            spanX = 1;
                            spanZ = 1;
                        }
                    }

                    // 4. Instantiate and claim the spanned cells
                    if (fits && chosenPrefab != null)
                    {
                        Vector3 position;
                        if (spanX == 1 && spanZ == 1)
                        {
                            position = cityGrid[x, z].position;
                        }
                        else if (spanX == 2 && spanZ == 1)
                        {
                            position = (cityGrid[x, z].position + cityGrid[x + 1, z].position) * 0.5f;
                        }
                        else if (spanX == 1 && spanZ == 2)
                        {
                            position = (cityGrid[x, z].position + cityGrid[x, z + 1].position) * 0.5f;
                        }
                        else // spanX == 2 && spanZ == 2
                        {
                            position = (cityGrid[x, z].position + cityGrid[x + 1, z + 1].position) * 0.5f;
                        }

                        GameObject building = Instantiate(chosenPrefab, position, chosenRotation, buildingParent);
                        building.name = $"Building_{x}_{z}{(spanX > 1 || spanZ > 1 ? $"_{spanX}x{spanZ}" : "")}";

                        for (int ox = 0; ox < spanX; ox++)
                        {
                            for (int oz = 0; oz < spanZ; oz++)
                            {
                                cityGrid[x + ox, z + oz].type = CellType.Building;
                                cityGrid[x + ox, z + oz].instance = building;
                                cityGrid[x + ox, z + oz].bounds = chosenBounds;
                            }
                        }
                    }
                }
            }
        }

        private bool TryFindFittingBuilding(GameObject[] prefabs, float areaW, float areaD,
            out GameObject fittingPrefab, out Bounds fittingBounds, out Quaternion fittingRotation)
        {
            var candidates = new List<(GameObject prefab, Bounds bounds, Quaternion rot)>();

            foreach (var p in prefabs)
            {
                if (p == null) continue;
                Bounds b = GetPrefabBounds(p);
                if (CanFitInArea(b, areaW, areaD, out Quaternion rot))
                {
                    candidates.Add((p, b, rot));
                }
            }

            if (candidates.Count > 0)
            {
                var chosen = candidates[random.Next(candidates.Count)];
                fittingPrefab = chosen.prefab;
                fittingBounds = chosen.bounds;
                fittingRotation = chosen.rot;
                return true;
            }

            fittingPrefab = null;
            fittingBounds = default;
            fittingRotation = Quaternion.identity;
            return false;
        }

        private bool CanFitInArea(Bounds bounds, float areaW, float areaD, out Quaternion rotation)
        {
            float sx = bounds.size.x;
            float sz = bounds.size.z;

            // Check unrotated (0° or 180°)
            bool fitsUnrotated = (sx <= areaW + 0.1f && sz <= areaD + 0.1f);
            // Check rotated 90° (90° or 270°)
            bool fitsRotated   = (sz <= areaW + 0.1f && sx <= areaD + 0.1f);

            if (fitsUnrotated && fitsRotated)
            {
                rotation = Quaternion.Euler(0, random.Next(4) * 90, 0);
                return true;
            }
            if (fitsUnrotated)
            {
                rotation = Quaternion.Euler(0, random.Next(2) * 180, 0);
                return true;
            }
            if (fitsRotated)
            {
                rotation = Quaternion.Euler(0, 90 + random.Next(2) * 180, 0);
                return true;
            }

            rotation = Quaternion.identity;
            return false;
        }

        private GameObject SelectBuildingPrefab()
        {
            float total = residentialWeight + commercialWeight + companyWeight + motelWeight;
            float roll = (float)random.NextDouble() * total;

            if (roll < residentialWeight && residentialBuildings.Length > 0)
                return residentialBuildings[random.Next(residentialBuildings.Length)];

            roll -= residentialWeight;
            if (roll < commercialWeight && commercialBuildings.Length > 0)
                return commercialBuildings[random.Next(commercialBuildings.Length)];

            roll -= commercialWeight;
            if (roll < companyWeight && companyBuildings.Length > 0)
                return companyBuildings[random.Next(companyBuildings.Length)];

            if (motelBuildings.Length > 0)
                return motelBuildings[random.Next(motelBuildings.Length)];

            // Fallback to any available building
            var allBuildings = residentialBuildings.Concat(commercialBuildings)
                .Concat(companyBuildings).Concat(motelBuildings).ToArray();

            return allBuildings.Length > 0 ? allBuildings[random.Next(allBuildings.Length)] : null;
        }

        private void PlaceProps()
        {
            Transform propParent = new GameObject("Props").transform;
            propParent.SetParent(cityRoot);

            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    // Place dense trees in park areas
                    if (cityGrid[x, z].type == CellType.Park && trees.Length > 0)
                    {
                        // Place multiple trees per park cell for dense forest look
                        int treeCount = random.Next(3, 6); // 3-5 trees per park cell
                        for (int i = 0; i < treeCount; i++)
                        {
                            GameObject treePrefab = trees[random.Next(trees.Length)];
                            Vector3 offset = new Vector3(
                                ((float)random.NextDouble() - 0.5f) * cellSize * 0.9f,
                                0,
                                ((float)random.NextDouble() - 0.5f) * cellSize * 0.9f
                            );

                            GameObject tree = Instantiate(treePrefab, cityGrid[x, z].position + offset,
                                Quaternion.Euler(0, random.Next(360), 0), propParent);
                            tree.name = $"Tree_Park_{x}_{z}_{i}";
                        }
                    }
                    else if (cityGrid[x, z].type == CellType.Empty)
                    {
                        // Place occasional trees in empty spaces
                        if (random.NextDouble() < treeDensity && trees.Length > 0)
                        {
                            GameObject treePrefab = trees[random.Next(trees.Length)];
                            Vector3 offset = new Vector3(
                                ((float)random.NextDouble() - 0.5f) * cellSize * 0.8f,
                                0,
                                ((float)random.NextDouble() - 0.5f) * cellSize * 0.8f
                            );

                            GameObject tree = Instantiate(treePrefab, cityGrid[x, z].position + offset,
                                Quaternion.Euler(0, random.Next(360), 0), propParent);
                            tree.name = $"Tree_{x}_{z}";
                        }
                    }

                    // Place props ONLY on straight roads (not corners, T-junctions, or crosses)
                    if (cityGrid[x, z].type == CellType.Road && random.NextDouble() < propDensity)
                    {
                        // Count connections
                        int connections = 0;
                        if (cityGrid[x, z].hasNorthRoad) connections++;
                        if (cityGrid[x, z].hasSouthRoad) connections++;
                        if (cityGrid[x, z].hasEastRoad) connections++;
                        if (cityGrid[x, z].hasWestRoad) connections++;

                        // Check if it's a straight road (2 opposite connections)
                        bool isStraight = (connections == 2) &&
                                         ((cityGrid[x, z].hasNorthRoad && cityGrid[x, z].hasSouthRoad) ||
                                          (cityGrid[x, z].hasEastRoad && cityGrid[x, z].hasWestRoad));

                        // Skip if not a straight road
                        if (!isStraight) continue;

                        // Get the actual road GameObject rotation
                        GameObject roadInstance = cityGrid[x, z].instance;
                        if (roadInstance == null) continue;

                        GameObject[] availableProps = new[] { streetSigns, trashCans, plants }
                            .Where(arr => arr != null && arr.Length > 0)
                            .SelectMany(arr => arr)
                            .ToArray();

                        if (availableProps.Length > 0)
                        {
                            GameObject propPrefab = availableProps[random.Next(availableProps.Length)];

                            float roadRotationY = roadInstance.transform.rotation.eulerAngles.y;

                            // Determine placement based on road rotation
                            Vector3 offset;
                            Quaternion propRotation;

                            if (Mathf.Abs(roadRotationY - 0f) < 5f || Mathf.Abs(roadRotationY - 180f) < 5f)
                            {
                                // Horizontal road (0° or 180°) - place on North or South (±4 on Z axis)
                                float side = random.Next(2) == 0 ? 1f : -1f;
                                offset = new Vector3(0, 0, side * 4f);
                                propRotation = Quaternion.Euler(0, side > 0 ? 90 : 270, 0);
                            }
                            else // roadRotationY ≈ 90° or 270°
                            {
                                // Vertical road (90° or 270°) - place on East or West (±4 on X axis)
                                float side = random.Next(2) == 0 ? 1f : -1f;
                                offset = new Vector3(side * 4f, 0, 0);
                                propRotation = Quaternion.Euler(0, side > 0 ? 0 : 180, 0);
                            }

                            GameObject prop = Instantiate(propPrefab, cityGrid[x, z].position + offset,
                                propRotation, propParent);
                            prop.name = $"Prop_{x}_{z}";
                        }
                    }
                }
            }
        }

        // ── City Life integration ──────────────────────────────────────────────

        /// <summary>
        /// Collects connectivity data for every road cell and returns it
        /// as a flat list consumed by CityLifeManager / CityWaypointGraph.
        /// </summary>
        private List<RoadNodeData> BuildRoadData()
        {
            var data = new List<RoadNodeData>();
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    var cell = cityGrid[x, z];
                    if (cell.type != CellType.Road) continue;

                    data.Add(new RoadNodeData
                    {
                        x            = x,
                        z            = z,
                        worldPosition = cell.position,
                        hasNorthRoad = cell.hasNorthRoad,
                        hasSouthRoad = cell.hasSouthRoad,
                        hasEastRoad  = cell.hasEastRoad,
                        hasWestRoad  = cell.hasWestRoad,
                    });
                }
            }
            return data;
        }

        /// <summary>
        /// Reconstructs road connectivity data directly from existing scene Road_x_z GameObjects.
        /// Used when entering Play Mode if cityGrid was cleared by domain reload.
        /// </summary>
        private List<RoadNodeData> RebuildRoadDataFromScene()
        {
            var data = new List<RoadNodeData>();
            var roadTransforms = new Dictionary<(int, int), Transform>();

            var allTransforms = FindObjectsByType<Transform>();
            foreach (var t in allTransforms)
            {
                if (t.name.StartsWith("Road_"))
                {
                    var parts = t.name.Split('_');
                    if (parts.Length >= 3 && int.TryParse(parts[1], out int rx) && int.TryParse(parts[2], out int rz))
                    {
                        roadTransforms[(rx, rz)] = t;
                    }
                }
            }

            foreach (var kvp in roadTransforms)
            {
                int rx = kvp.Key.Item1;
                int rz = kvp.Key.Item2;
                data.Add(new RoadNodeData
                {
                    x            = rx,
                    z            = rz,
                    worldPosition = kvp.Value.position,
                    hasNorthRoad = roadTransforms.ContainsKey((rx, rz + 1)),
                    hasSouthRoad = roadTransforms.ContainsKey((rx, rz - 1)),
                    hasEastRoad  = roadTransforms.ContainsKey((rx + 1, rz)),
                    hasWestRoad  = roadTransforms.ContainsKey((rx - 1, rz))
                });
            }

            serializedRoadData = data;
            return data;
        }

        private Bounds GetPrefabBounds(GameObject prefab)
        {
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(Vector3.zero, Vector3.one * 5f);

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos || cityGrid == null)
                return;

            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    Vector3 center = new Vector3(x * cellSize, 0, z * cellSize);

                    switch (cityGrid[x, z].type)
                    {
                        case CellType.Road:
                            Gizmos.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
                            break;
                        case CellType.Building:
                            Gizmos.color = new Color(0.8f, 0.8f, 0.2f, 0.3f);
                            break;
                        case CellType.Empty:
                            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.1f);
                            break;
                        case CellType.Park:
                            Gizmos.color = new Color(0.2f, 0.6f, 0.2f, 0.3f);
                            break;
                    }

                    Gizmos.DrawCube(center, new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f));
                }
            }
        }
    }
}
