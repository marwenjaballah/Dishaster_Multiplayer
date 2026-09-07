# CityLife System: Procedural Urban Generation & Traffic Simulation

A modular, self-contained Unity package providing procedural city generation, arterial road network synthesis, and an urban life simulation system featuring vehicular traffic and procedurally animated pedestrians.

> **Asset Notice**:
> All buildings, roads, trees, and props used by the generator are from the **Pandazole City Town Pack**:
> Unity Asset Store link: [https://assetstore.unity.com/packages/package/205787](https://assetstore.unity.com/packages/package/205787)

---

## 1. System Overview

The CityLife package consolidates procedural environment generation and multi-agent urban simulation into a decoupled, zero-dependency directory (`Assets/CityLife/`). The system functions in both the Unity Editor and runtime Play Mode, handling layout construction, mesh bounds analysis, asset assignment, graph synthesis, and multi-agent navigation without external dependencies.

---

## 2. Architecture & Technical Breakdown

### 2.1 City Generator (`CityGenerator.cs`)

The city generator constructs the physical urban environment on a discrete 2D grid matrix (`cityGrid[x, z]`) where each unit cell represents a 10m x 10m world-space area (`cellSize = 10f`).

Each cell has a designated state:
- `CellType.Road`: Dedicated to vehicular lanes and pedestrian sidewalks.
- `CellType.Building`: Occupied by single-cell or multi-cell architectural structures.
- `CellType.Park`: Dense public green spaces containing multi-tree foliage clusters.
- `CellType.Empty`: Open yard or plaza space evaluated for building placement or single decorative trees.

#### Road Network & Topological Analysis
- Roads are planned along regular intervals determined by `mainRoadInterval` (default: every 3 blocks).
- Every road cell evaluates its 4 cardinal neighbors (North, South, East, West).
- Based on connection counts and neighbor orientations, the system selects and rotates the appropriate road prefab:
  - 4 connections: 4-way intersection (`roadCross`).
  - 3 connections: 3-way T-junction (`roadTJunction`), oriented toward the omitted road direction.
  - 2 connections (opposite): 2-lane straight road (`roadStraight`), aligned horizontally (0 deg) or vertically (90 deg).
  - 2 connections (adjacent): 90-degree corner turn (`roadCorner`), rotated to join adjacent streets.
  - 1 connection: Dead-end terminal road (`roadEnd`), rotated toward its single feeder road.

#### Block Merging (Square & Rectangular Parcels)
- Standard blocks are 2x2 cell square blocks between grid avenues.
- The block planner (`PlanRoadsAndBlocks`) evaluates adjacent block pairs:
  - `rectangularBlockChance` (0.0 to 0.6): Probability of merging two adjacent blocks into a continuous rectangle (either 2x5 or 5x2 cells) by omitting the dividing road segment.
  - Boundary guards prevent omitting outer perimeter roads or cross-street intersections.
  - Spacing constraints prevent consecutive parallel merges on adjacent rows or columns.
- Merged rectangular parcels are designated as either:
  - Large rectangular parks (`rectangularParkChance`, default 0.40): Populated with continuous tree groves.
  - Large building development parcels: Available for expansive multi-cell commercial or residential buildings.

#### Multi-Cell Building Placement & Fallback System
The building placement algorithm evaluates empty lots and supports varying structure footprints:
1. Footprint Detection:
   - Evaluates whether contiguous empty cells are available starting at `(x, z)`:
     - Single-cell (1x1, 10m x 10m).
     - Horizontal two-cell (2x1, 20m x 10m) if cell `(x+1, z)` is empty.
     - Vertical two-cell (1x2, 10m x 20m) if cell `(x, z+1)` is empty.
     - Quad-cell (2x2, 20m x 20m) if cells `(x+1, z)`, `(x, z+1)`, and `(x+1, z+1)` are empty.
2. Candidate Selection & Orientation:
   - Selects a building from category weights (`residentialWeight`, `commercialWeight`, `companyWeight`, `motelWeight`).
   - Calculates renderer bounding boxes (`GetPrefabBounds`) and checks whether the candidate fits in available dimensions at unrotated (0 deg / 180 deg) or rotated (90 deg / 270 deg) orientations.
3. Fallback Replacement (Zero Voids):
   - If a candidate building does not fit the available space (e.g., a 20m building chosen for an isolated 10m lot), the system does not discard the lot.
   - It searches all assigned building prefabs for a model that fits the exact dimensions of the lot and instantiates it.
4. Cell Reservation:
   - When a multi-cell structure is placed, it centers itself across the combined bounds, and marks all spanned cells as `CellType.Building`, preventing overlapping placements.

#### Foliage & Prop Placement
- Park Cells: Receives 3 to 5 randomized trees per cell with local position jitter and full 360-degree rotation variance.
- Open Lots: Controlled by `treeDensity` (default 0.3) to scatter standalone trees across unbuilt spaces.
- Sidewalk Props: Straight road tiles receive street signs, trash cans, or planter boxes aligned strictly along the curb (offset +/- 4.0m from road center, parallel to road heading).

---

### 2.2 Vehicle Traffic Simulation (`CarController.cs`)

`CarController` governs vehicular movement and lane navigation:
- Right-Hand Lane Geometry: Vehicles navigate lane waypoints offset +1.5m to the right of the road center line relative to their driving vector.
- Intersection Turns:
  - Right Turns: Follow tight inner radius curves.
  - Left Turns: Execute wide tangent arcs across the intersection box, avoiding the center pivot and entering the correct destination lane.
- Obstacle Avoidance & Braking: A forward raycast detects leading vehicles. If an obstacle is detected within `followDistance`, the vehicle decelerates smoothly; if the obstacle is within stopping threshold, the vehicle stops completely.
- Procedural Wheel Spin: Rotates four wheel transforms around their local pitch axes proportional to linear travel distance.
- Despawn & Terminal Respawning: When a vehicle reaches a dead-end road (`roadEnd`), it triggers a despawn back to `CityObjectPool`. The manager initiates a staggered respawn timer from another peripheral terminal to preserve continuous traffic flow.

---

### 2.3 Pedestrian Simulation (`PedestrianController.cs`)

`PedestrianController` manages bipedal pedestrian traversal along sidewalks:
- Sidewalk Geometry: Navigates waypoint chains offset +/- 4.0m from road center lines on both edges of street tiles.
- Procedural Movement Animation:
  - Operates on a hierarchical bone/pivot rig (hips, legs, shoulders, arms, torso, head).
  - While walking, applies synchronized sinusoidal swings to left/right limbs, a vertical torso bounce at twice the gait frequency, and a slight lateral sway.
  - While idle, limb positions smoothly reset to rest poses.
- Behavioral State Machine:
  - `State.Walking`: Traverses toward the active sidewalk waypoint.
  - `State.Arriving`: Decelerates upon reaching the target node.
  - `State.Idle`: Pauses for 1 to 4 seconds, during which the agent executes procedural head rotations to look across intersections before selecting the next destination.
- Visual Variation: On spawn, randomizes shirt and pants colors using `MaterialPropertyBlock` instances to maintain batching efficiency without material leaks.

---

### 2.4 Waypoint Graphs & Object Pooling

- `CityWaypointGraph.cs`: Constructs two independent navigation networks from road data:
  - Lane Graph: Directed roadway node chains with turn connectors.
  - Sidewalk Graph: Bidirectional perimeter node chains with intersection crosswalk links.
- `CityObjectPool.cs`: Implements a generic, pre-warmed object pool for vehicular and pedestrian agents, eliminating runtime GC allocations during agent respawning.
- `RoadNodeData.cs`: A serializable data contract capturing road grid coordinates, world positions, and cardinal connectivity flags.

---

### 2.5 Editor Integration (`CityGeneratorEditor.cs`, `CityLifeBuilderEditor.cs`)

- Dynamic Asset Discovery (`FindPrefabsPath`): Resolves prefab directories dynamically through `AssetDatabase`, functioning regardless of root folder names or package relocations.
- One-Click Builder (`Tools > CityLife > Full Setup`): Automatically verifies materials and prefabs, attaches missing components, auto-assigns Pandazole assets, and generates the city in a single operation.

---

## 3. Package Folder Structure

```
Assets/CityLife/
├── CityElements/              Comprehensive Pandazole asset pack
│   ├── Materials/             Building and environment materials
│   ├── Models/                3D meshes for buildings, roads, props, trees
│   ├── Prefabs/               Categorized prefabs for all road types and structures
│   ├── Scenes/                Reference demo scenes
│   └── Textures/              Texture atlases
├── Editor/                    Editor utilities and custom inspectors
│   ├── CityGeneratorEditor.cs Inspector with 1-click action buttons and auto-assign
│   └── CityLifeBuilderEditor.cs Top-level menu tools (Tools > CityLife)
├── Materials/                 URP Lit materials for vehicles and pedestrians
├── Prefabs/                   Procedural Car and Pedestrian agent prefabs
├── Scripts/                   Core simulation runtime scripts
│   ├── CarController.cs       Vehicular kinematics and obstacle avoidance
│   ├── CityGenerator.cs       Procedural urban grid synthesis and lot planning
│   ├── CityLifeManager.cs     Simulation coordinator, spawner, and pool host
│   ├── CityObjectPool.cs      Zero-allocation pooled agent lifecycle host
│   ├── CityWaypointGraph.cs   Dual lane and sidewalk navigation topology
│   ├── PedestrianController.cs Pedestrian movement, animation, and state machine
│   └── RoadNodeData.cs        Lightweight road node data model
└── README.md                  Package documentation and configuration reference
```

---

## 4. Usage Guide for Any Unity Project

### Step 1: Import Package
Copy the `CityLife/` directory directly into your target project's `Assets/` directory.

### Step 2: One-Click Scene Setup
1. Open any scene (or create a new empty scene).
2. In the Unity menu bar, select:
   `Tools` > `CityLife` > `Full Setup (Build Prefabs & Wire Scene)`
3. The setup tool will:
   - Validate and build all car and pedestrian agent materials and prefabs.
   - Locate or create a `CitySystem` GameObject in the active scene.
   - Attach `CityGenerator` and `CityLifeManager` components.
   - Scan and auto-assign all road, building, and foliage prefabs from `CityElements/Prefabs/`.
   - Wire dependencies and trigger city generation.

### Step 3: Run Simulation
Press **Play** in the Unity Editor:
- Cars spawn at peripheral terminal roads, drive in the right lane, yield to lead vehicles, and execute wide left turns.
- Pedestrians spawn along sidewalks, traversing paths with procedural arm and leg swings and pausing at intersections.

---

## 5. Configuration Reference

### CityGenerator Component

| Property | Default | Description |
| :--- | :--- | :--- |
| `gridWidth` | `30` | Width of the city in 10m grid cells. |
| `gridHeight` | `30` | Height of the city in 10m grid cells. |
| `cellSize` | `10.0` | Spatial dimension of each cell (10m matches Pandazole road tiles). |
| `seed` | `12345` | Random seed for reproducible city generation. |
| `generateOnStart` | `false` | When true, regenerates the city automatically on Start in Play Mode. |
| `mainRoadInterval` | `3` | Distance between major road avenues (interval = 3 yields 2x2 building blocks). |
| `rectangularBlockChance`| `0.35` | Probability of merging adjacent square blocks into a continuous rectangle. |
| `rectangularParkChance` | `0.40` | Probability that a merged rectangular block becomes a public park. |
| `buildingDensity` | `1.0` | Proportion of available empty lots that receive buildings (1.0 = 100%). |
| `treeDensity` | `0.3` | Probability of placing a tree on leftover empty spaces outside parks. |
| `propDensity` | `0.2` | Probability of placing sidewalk props along straight road segments. |
| `residentialWeight` | `0.50` | Relative probability of selecting residential buildings. |
| `commercialWeight` | `0.30` | Relative probability of selecting commercial buildings. |
| `companyWeight` | `0.15` | Relative probability of selecting office/company buildings. |
| `motelWeight` | `0.05` | Relative probability of selecting motel buildings. |

### CityLifeManager Component

| Property | Default | Description |
| :--- | :--- | :--- |
| `carPrefab` | `Car.prefab` | Default prefab instantiated for vehicular agents. |
| `carPrefabs` | `[]` | Optional array of multiple vehicle variations (Sedans, SUVs, Taxis, Trucks). |
| `carSpawnMode` | `Hybrid` | `DeadEndTerminalsOnly` (spawn strictly at outer road ends), `RandomOnRoads` (spawn across any inner lane), or `Hybrid` (initial spread on roads + outer terminal respawns). |
| `pedestrianPrefab` | `Pedestrian.prefab` | Prefab instantiated for pedestrian agents. |
| `targetCars` | `15` | Maximum active vehicle count maintained by the spawner. |
| `targetPedestrians` | `20` | Maximum active pedestrian count maintained by the spawner. |
| `carPoolSize` | `25` | Capacity of the pre-warmed vehicle pool. |
| `pedestrianPoolSize`| `30` | Capacity of the pre-warmed pedestrian pool. |
| `carSpeedRange` | `(6.0, 10.0)` | Min and max cruise speed (m/s) assigned to vehicles. |
| `pedestrianSpeedRange` | `(1.2, 2.0)` | Min and max walking speed (m/s) assigned to pedestrians. |
| `carFollowDistance` | `7.0` | Distance (m) at which vehicles begin decelerating behind obstacles. |
| `carStopDistance` | `3.5` | Distance (m) at which vehicles come to a complete stop behind obstacles. |

---

## 6. How to Use

### Method A: Automated One-Click Setup (Recommended)
1. Copy the `CityLife/` folder into your Unity project's `Assets/` directory.
2. In the top Unity menu, click:
   `Tools` > `CityLife` > `Full Setup (Build Prefabs & Wire Scene)`.
3. This command will automatically:
   - Create or update the `Car.prefab` and `Pedestrian.prefab` with their materials in `Assets/CityLife/Prefabs/`.
   - Locate or create a `CitySystem` GameObject in the current scene.
   - Attach `CityGenerator` and `CityLifeManager` to the GameObject.
   - Scan and automatically assign all road, building, and foliage prefabs from `CityElements/Prefabs/`.
   - Wire references between the generator and the simulation manager.
   - Generate the complete procedural city immediately in the Editor.
4. Press Play in the Unity Editor. Cars will spawn and navigate in the right lane, and pedestrians will walk on sidewalks with procedural animations.

### Method B: Manual Inspector Workflow
If you prefer configuring components manually or integrating into an existing scene:
1. Create an empty GameObject in your scene hierarchy and name it `CitySystem`.
2. Add the `CityGenerator` component (`SevenDays.World.CityGenerator`) to it.
3. Add the `CityLifeManager` component (`CityLife.CityLifeManager`) to the same GameObject.
4. On the `CityGenerator` component inspector:
   - Click the `Auto-Assign Prefabs` button to automatically populate all residential, commercial, company, motel, road, and prop arrays from `CityElements/Prefabs/`.
5. On the `CityLifeManager` component inspector:
   - Drag and drop `Assets/CityLife/Prefabs/Car.prefab` into the `Car Prefab` field.
   - Drag and drop `Assets/CityLife/Prefabs/Pedestrian.prefab` into the `Pedestrian Prefab` field.
6. Click the `Generate City` button on the `CityGenerator` inspector.

### Customizing and Iterating Layouts
- Changing City Size: Adjust `Grid Width` and `Grid Height` (e.g. 20x20 for small towns, 40x40 for larger cities). Pandazole road spacing requires `Cell Size = 10`.
- Generating New Layouts: Click `Randomize Seed` in the inspector, then click `Generate City`.
- Modifying Block Variety:
  - Increase `Rectangular Block Chance` (up to 0.6) to create longer, merged avenues and rectangular lots.
  - Increase `Rectangular Park Chance` to transform merged blocks into expansive city parks.
- Adjusting Traffic & Pedestrian Crowds:
  - In `CityLifeManager`, adjust `Target Cars` and `Target Pedestrians` to increase or decrease urban density.
  - Adjust `Car Speed Range` and `Pedestrian Speed Range` to control traversal speeds.
- Clearing the Scene: Click `Clear City` in the inspector to remove all generated roads, buildings, and props before saving or modifying settings.
