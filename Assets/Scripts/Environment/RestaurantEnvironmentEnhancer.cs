using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Environment
{
    /// <summary>
    /// Master Architectural & Environmental Redesign for the Restaurant interior & exterior.
    /// Transforms the game space into "The Gilded Spoon Diner & Bistro" — a vibrant, 
    /// high-end retro-modern commercial restaurant with:
    /// 1. Zoned Flooring: Herringbone hardwood parquet, plush velvet dining rugs, and checkerboard kitchen tile.
    /// 2. Architectural Envelope: Exposed red brick accent wall, mahogany wainscoting, and panoramic city-view windows.
    /// 3. Luxury Dining Booths: Handcrafted crimson/emerald tufted leather banquettes, walnut tables, brass lamps & condiments.
    /// 4. Host Station & Waiting Lounge: Maître d' podium, golden velvet bench, coat rack, and lush 3D ficus trees.
    /// 5. Diner Service Bar & Coffee Station: Carrera marble bar counter, chrome swivel stools, espresso machine & pastry dome.
    /// 6. Exhibition Kitchen & Pass-Through: Service window with heat lamps, ticket rail, saloon doors, and stainless canopy hood.
    /// 7. Atmospheric Theming: 1950s illuminated jukebox, framed vintage art, chalkboard specials, and radiant neon diner signage.
    /// 8. Exterior Takeout Bay: Striped canvas awning, courier staging counter, and neon pickup signage.
    /// </summary>
    [ExecuteAlways]
    public class RestaurantEnvironmentEnhancer : MonoBehaviour
    {
        public static RestaurantEnvironmentEnhancer Instance { get; private set; }

        public static void ResetStaticData()
        {
            Instance = null;
        }

        private const string ROOT_CONTAINER_NAME = "Restaurant_EnhancedDesign";

        [Header("Material References")]
        [SerializeField] private Material hardwoodFloorMat;
        [SerializeField] private Material kitchenTileMat;
        [SerializeField] private Material checkerboardDarkMat;
        [SerializeField] private Material checkerboardLightMat;
        [SerializeField] private Material subwayTileMat;
        [SerializeField] private Material exposedBrickMat;
        [SerializeField] private Material wainscotingMat;
        [SerializeField] private Material marbleBarMat;
        [SerializeField] private Material boothBurgundyMat;
        [SerializeField] private Material boothEmeraldMat;
        [SerializeField] private Material velvetPlushMat;
        [SerializeField] private Material wallAccentMat;
        [SerializeField] private Material stainlessSteelMat;
        [SerializeField] private Material brassGoldMat;
        [SerializeField] private Material neonCyanMat;
        [SerializeField] private Material neonAmberMat;
        [SerializeField] private Material windowGlassMat;
        [SerializeField] private Material windowFrameMat;
        [SerializeField] private Material ceilingBeamMat;
        [SerializeField] private Material chalkboardMat;
        [SerializeField] private Material jukeboxNeonMat;
        [SerializeField] private Material awningRedMat;
        [SerializeField] private Material awningWhiteMat;
        [SerializeField] private Material ketchupRedMat;
        [SerializeField] private Material mustardYellowMat;
        [SerializeField] private Material plantGreenMat;
        [SerializeField] private Material rubberFloorMat;
        [SerializeField] private Material ansulPipingMat;
        [SerializeField] private Material chromePolishedMat;
        [SerializeField] private Material plasticHotelPanMat;
        [SerializeField] private Material sanitizerBlueMat;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            LoadMaterialsIfMissing();
        }

        private void Start()
        {
            GameObject existing = GameObject.Find(ROOT_CONTAINER_NAME);
            if (existing == null)
            {
                BakeRestaurantEnvironment();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void LoadMaterialsIfMissing()
        {
            if (hardwoodFloorMat == null) hardwoodFloorMat = LoadAssetMat("Restaurant_HardwoodFloor");
            if (kitchenTileMat == null) kitchenTileMat = LoadAssetMat("Restaurant_KitchenTile");
            if (checkerboardDarkMat == null) checkerboardDarkMat = LoadAssetMat("Restaurant_CheckerboardDark");
            if (checkerboardLightMat == null) checkerboardLightMat = LoadAssetMat("Restaurant_CheckerboardLight");
            if (subwayTileMat == null) subwayTileMat = LoadAssetMat("Restaurant_SubwayTile");
            if (exposedBrickMat == null) exposedBrickMat = LoadAssetMat("Restaurant_ExposedBrick");
            if (wainscotingMat == null) wainscotingMat = LoadAssetMat("Restaurant_Wainscoting");
            if (marbleBarMat == null) marbleBarMat = LoadAssetMat("Restaurant_MarbleBarTop");
            if (boothBurgundyMat == null) boothBurgundyMat = LoadAssetMat("Restaurant_BoothLeatherBurgundy");
            if (boothEmeraldMat == null) boothEmeraldMat = LoadAssetMat("Restaurant_BoothLeatherEmerald");
            if (velvetPlushMat == null) velvetPlushMat = LoadAssetMat("Restaurant_VelvetPlush");
            if (wallAccentMat == null) wallAccentMat = LoadAssetMat("Restaurant_WallAccent");
            if (stainlessSteelMat == null) stainlessSteelMat = LoadAssetMat("Restaurant_StainlessSteel");
            if (brassGoldMat == null) brassGoldMat = LoadAssetMat("Restaurant_BrassGold");
            if (neonCyanMat == null) neonCyanMat = LoadAssetMat("Restaurant_NeonCyan");
            if (neonAmberMat == null) neonAmberMat = LoadAssetMat("Restaurant_NeonAmber");
            if (windowGlassMat == null) windowGlassMat = LoadAssetMat("Restaurant_WindowGlass");
            if (windowFrameMat == null) windowFrameMat = LoadAssetMat("Restaurant_WindowFrame");
            if (ceilingBeamMat == null) ceilingBeamMat = LoadAssetMat("Restaurant_CeilingBeam");
            if (chalkboardMat == null) chalkboardMat = LoadAssetMat("Restaurant_ChalkboardSlate");
            if (jukeboxNeonMat == null) jukeboxNeonMat = LoadAssetMat("Restaurant_JukeboxNeon");
            if (awningRedMat == null) awningRedMat = LoadAssetMat("Restaurant_AwningRed");
            if (awningWhiteMat == null) awningWhiteMat = LoadAssetMat("Restaurant_AwningWhite");
            if (ketchupRedMat == null) ketchupRedMat = LoadAssetMat("Restaurant_KetchupRed");
            if (mustardYellowMat == null) mustardYellowMat = LoadAssetMat("Restaurant_MustardYellow");
            if (plantGreenMat == null) plantGreenMat = LoadAssetMat("Restaurant_PlantGreen");
            if (rubberFloorMat == null) rubberFloorMat = LoadAssetMat("Restaurant_RubberFloorMat");
            if (ansulPipingMat == null) ansulPipingMat = LoadAssetMat("Restaurant_AnsulFirePiping");
            if (chromePolishedMat == null) chromePolishedMat = LoadAssetMat("Restaurant_ChromePolished");
            if (plasticHotelPanMat == null) plasticHotelPanMat = LoadAssetMat("Restaurant_PlasticHotelPan");
            if (sanitizerBlueMat == null) sanitizerBlueMat = LoadAssetMat("Restaurant_SanitizerBlue");
        }

        private Material LoadAssetMat(string matName)
        {
            #if UNITY_EDITOR
            string path = $"Assets/_Assets/Materials/Restaurant/{matName}.mat";
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            #else
            return null;
            #endif
        }

        [ContextMenu("Bake Restaurant Environment")]
        public void BakeRestaurantEnvironment()
        {
            LoadMaterialsIfMissing();

            GameObject restObj = GameObject.Find("Restaurant");
            if (restObj == null)
            {
                Debug.LogError("[RestaurantEnvironmentEnhancer] Restaurant root object not found!");
                return;
            }

            // Hide old generic single-plane floor
            Transform oldFloor = restObj.transform.Find("walls/Floor (1)");
            if (oldFloor != null)
            {
                MeshRenderer mr = oldFloor.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
            }

            // Hide old prototype egg table tops & legs (replaced by luxury booths/bistros)
            foreach (var table in restObj.GetComponentsInChildren<CustomerTable>(true))
            {
                Transform tt = table.transform.Find("TableTop");
                if (tt != null) { var mr = tt.GetComponent<MeshRenderer>(); if (mr != null) mr.enabled = false; }
                Transform tl = table.transform.Find("TableLegs");
                if (tl != null) { var mr = tl.GetComponent<MeshRenderer>(); if (mr != null) mr.enabled = false; }
            }

            // Hide old prototype egg stool meshes & colliders, reposition to new booth cushions
            Transform diningArea = restObj.transform.Find("DiningArea");
            if (diningArea != null)
            {
                foreach (var mr in diningArea.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (mr.name.Contains("EggStool")) mr.enabled = false;
                }
                foreach (var col in diningArea.GetComponentsInChildren<Collider>(true))
                {
                    if (col.name.Contains("EggStool")) DestroyImmediate(col);
                }
            }

            // Clean up existing enhancement container
            Transform oldContainer = restObj.transform.Find(ROOT_CONTAINER_NAME);
            if (oldContainer != null)
            {
                if (Application.isPlaying) Destroy(oldContainer.gameObject);
                else DestroyImmediate(oldContainer.gameObject);
            }

            GameObject root = new GameObject(ROOT_CONTAINER_NAME);
            root.transform.SetParent(restObj.transform, false);

            // 1. Zoned Flooring with Rich Contrast & Carpet Insets
            BuildZonedFlooring(root.transform);

            // 2. Architectural Walls, Brick Texturing & Panoramic City Windows
            BuildArchitecturalWallsAndWindows(root.transform);

            // 3. Luxurious Diner Booths & Bistro Seating Sets
            BuildDinerBoothsAndDiningSets(root.transform);

            // 4. Host Station, Waiting Lounge & Greenery Vestibule
            BuildHostStandAndWaitingLounge(root.transform);

            // 5. Diner Service Bar, Swivel Stools, Espresso & Pastry Dome
            BuildDinerServiceBarAndCoffeeStation(root.transform);

            // 6. Exhibition Kitchen, Pass-Through Window & Stainless Range Hood
            BuildExhibitionKitchenAndPassThrough(root.transform);

            // 7. Atmosphere, Vintage Art, Chalkboard & Illuminated Retro Jukebox
            BuildAtmosphereArtAndJukebox(root.transform);

            // 8. Exterior Takeout Canopy & Courier Drive-Up Window
            BuildTakeoutAndCourierBay(root.transform);

            Debug.Log("[RestaurantEnvironmentEnhancer] ✨ Complete restaurant redesign successfully baked!");
        }

        // ── 1. Zoned Flooring ─────────────────────────────────────────────────

        private void BuildZonedFlooring(Transform parent)
        {
            GameObject floorGroup = new GameObject("01_ZonedFloors");
            floorGroup.transform.SetParent(parent, false);

            // Dining Hall Hardwood Parquet: X ~ 88.5 to 110.0, Z ~ -5.2 to -36.4
            CreatePrimitiveCube(floorGroup.transform, "Floor_DiningHardwood",
                new Vector3(99.25f, 0.012f, -20.8f),
                new Vector3(21.5f, 0.02f, 31.2f),
                hardwoodFloorMat);

            // Plush Burgundy Carpet Insets Under Dining Rows
            float[] rowZ = new float[] { -14.84f, -22.34f, -29.84f };
            for (int r = 0; r < rowZ.Length; r++)
            {
                CreatePrimitiveCube(floorGroup.transform, $"Carpet_BoothRow_{r + 1}",
                    new Vector3(96.74f, 0.016f, rowZ[r]),
                    new Vector3(15.2f, 0.022f, 4.6f),
                    boothBurgundyMat);

                // Brass border edging around carpet
                CreatePrimitiveCube(floorGroup.transform, $"Carpet_Border_{r + 1}",
                    new Vector3(96.74f, 0.020f, rowZ[r]),
                    new Vector3(15.4f, 0.025f, 4.8f),
                    brassGoldMat);
            }

            // Exhibition Kitchen Commercial Tile: X ~ 110.0 to 122.5, Z ~ -14.0 to -27.5
            CreatePrimitiveCube(floorGroup.transform, "Floor_KitchenTile",
                new Vector3(116.25f, 0.015f, -20.89f),
                new Vector3(12.5f, 0.02f, 13.5f),
                kitchenTileMat);

            // Kitchen Checkerboard Tile Accent Run (Along Central Working Aisle)
            CreatePrimitiveCube(floorGroup.transform, "Floor_KitchenCheckerRunner",
                new Vector3(116.25f, 0.018f, -20.89f),
                new Vector3(10.5f, 0.022f, 2.6f),
                checkerboardDarkMat);

            // Heavy-Duty Slotted Anti-Fatigue Rubber Drainage Mats (Commercial Ergonomic Standards)
            // Mat 1: Hot Cook Line (in front of Stoves, Sink, Plates)
            CreatePrimitiveCube(floorGroup.transform, "KitchenMat_CookLine",
                new Vector3(119.85f, 0.026f, -21.14f),
                new Vector3(1.10f, 0.012f, 8.80f),
                rubberFloorMat);
            // Mat 2: North Cold Prep Line (in front of Bread, Cabbage, Cheese, Meat, Tomato)
            CreatePrimitiveCube(floorGroup.transform, "KitchenMat_NorthPrep",
                new Vector3(115.89f, 0.026f, -17.20f),
                new Vector3(8.50f, 0.012f, 1.10f),
                rubberFloorMat);
            // Mat 3: South Fryer & Beverage Station
            CreatePrimitiveCube(floorGroup.transform, "KitchenMat_SouthLine",
                new Vector3(115.89f, 0.026f, -24.55f),
                new Vector3(8.50f, 0.012f, 1.10f),
                rubberFloorMat);
            // Mat 4: Center Island Work Pad
            CreatePrimitiveCube(floorGroup.transform, "KitchenMat_IslandPad",
                new Vector3(115.89f, 0.027f, -20.89f),
                new Vector3(3.80f, 0.012f, 1.15f),
                rubberFloorMat);

            // Entrance Vestibule Hexagonal Stone Entry Mat: Southwest entrance area
            CreatePrimitiveCube(floorGroup.transform, "Floor_EntranceVestibule",
                new Vector3(92.0f, 0.018f, -34.5f),
                new Vector3(6.5f, 0.025f, 3.6f),
                checkerboardLightMat);

            // Brass Threshold Transition Strip between Dining and Kitchen
            CreatePrimitiveCube(floorGroup.transform, "Floor_BrassThreshold",
                new Vector3(110.0f, 0.024f, -20.89f),
                new Vector3(0.22f, 0.035f, 13.5f),
                brassGoldMat);
        }

        // ── 2. Architectural Walls, Brick & Windows ───────────────────────────

        private void BuildArchitecturalWallsAndWindows(Transform parent)
        {
            GameObject wallGroup = new GameObject("02_ArchitecturalWalls");
            wallGroup.transform.SetParent(parent, false);

            // West Wall Wainscoting (Lower 1.1m dark mahogany wood)
            CreatePrimitiveCube(wallGroup.transform, "Wainscot_WestWall",
                new Vector3(88.55f, 0.55f, -20.8f),
                new Vector3(0.12f, 1.1f, 31.2f),
                wainscotingMat);

            // West Wall Chair Rail Molding Strip
            CreatePrimitiveCube(wallGroup.transform, "Molding_WestWall",
                new Vector3(88.58f, 1.12f, -20.8f),
                new Vector3(0.16f, 0.08f, 31.2f),
                brassGoldMat);

            // West Wall Upper Exposed Red Brick Accent Wall (Upper 1.9m)
            CreatePrimitiveCube(wallGroup.transform, "Brick_WestWall",
                new Vector3(88.54f, 2.15f, -20.8f),
                new Vector3(0.10f, 1.95f, 31.2f),
                exposedBrickMat);

            // 3 Large Panoramic Arched City-View Windows along West Wall
            float[] windowZ = new float[] { -14.84f, -22.34f, -29.84f };
            for (int w = 0; w < windowZ.Length; w++)
            {
                float z = windowZ[w];
                GameObject winObj = new GameObject($"Window_Panoramic_{w + 1}");
                winObj.transform.SetParent(wallGroup.transform, false);

                // Window Frame Outer
                CreatePrimitiveCube(winObj.transform, "Frame_Outer",
                    new Vector3(88.56f, 2.15f, z),
                    new Vector3(0.14f, 1.60f, 2.40f),
                    windowFrameMat);

                // Window Glass Pane (Translucent with sky reflections)
                CreatePrimitiveCube(winObj.transform, "Glass_Pane",
                    new Vector3(88.54f, 2.15f, z),
                    new Vector3(0.04f, 1.48f, 2.26f),
                    windowGlassMat);

                // Horizontal & Vertical Muntin Dividers (Classic French 4-lite grid)
                CreatePrimitiveCube(winObj.transform, "Muntin_H",
                    new Vector3(88.58f, 2.15f, z),
                    new Vector3(0.08f, 0.04f, 2.26f),
                    brassGoldMat);
                CreatePrimitiveCube(winObj.transform, "Muntin_V",
                    new Vector3(88.58f, 2.15f, z),
                    new Vector3(0.08f, 1.48f, 0.04f),
                    brassGoldMat);

                // Polished Walnut Interior Window Sill
                CreatePrimitiveCube(winObj.transform, "Window_Sill",
                    new Vector3(88.66f, 1.34f, z),
                    new Vector3(0.26f, 0.05f, 2.50f),
                    wainscotingMat);

                // Warm Exterior Sunlight Spill Light
                CreatePointLight(winObj.transform, "Sunlight_Spill",
                    new Vector3(89.20f, 2.20f, z),
                    new Color(1.0f, 0.95f, 0.85f),
                    2.0f, 4.5f);
            }

            // North Wall Wainscoting & Plaster
            CreatePrimitiveCube(wallGroup.transform, "Wainscot_NorthWall",
                new Vector3(99.25f, 0.55f, -5.25f),
                new Vector3(21.5f, 1.1f, 0.12f),
                wainscotingMat);
            CreatePrimitiveCube(wallGroup.transform, "Plaster_NorthWall",
                new Vector3(99.25f, 2.15f, -5.25f),
                new Vector3(21.5f, 1.95f, 0.10f),
                wallAccentMat);

            // Grand Arched Brass Wall Mirror on North Wall (Creates deep visual reflection)
            CreatePrimitiveCube(wallGroup.transform, "Mirror_Frame",
                new Vector3(96.74f, 2.10f, -5.22f),
                new Vector3(4.20f, 1.60f, 0.06f),
                brassGoldMat);
            CreatePrimitiveCube(wallGroup.transform, "Mirror_Glass",
                new Vector3(96.74f, 2.10f, -5.20f),
                new Vector3(4.00f, 1.44f, 0.03f),
                windowGlassMat);

            // South Wall Wainscoting
            CreatePrimitiveCube(wallGroup.transform, "Wainscot_SouthWall",
                new Vector3(99.25f, 0.55f, -36.35f),
                new Vector3(21.5f, 1.1f, 0.12f),
                wainscotingMat);

            // Double Mahogany Entrance Doors with Brass Push Bars
            CreatePrimitiveCube(wallGroup.transform, "Door_Frame",
                new Vector3(92.0f, 1.45f, -36.32f),
                new Vector3(2.6f, 2.7f, 0.14f),
                wainscotingMat);
            CreatePrimitiveCube(wallGroup.transform, "Door_Leaf_L",
                new Vector3(91.35f, 1.35f, -36.30f),
                new Vector3(1.15f, 2.45f, 0.08f),
                wainscotingMat);
            CreatePrimitiveCube(wallGroup.transform, "Door_Leaf_R",
                new Vector3(92.65f, 1.35f, -36.30f),
                new Vector3(1.15f, 2.45f, 0.08f),
                wainscotingMat);
            CreatePrimitiveCube(wallGroup.transform, "Door_Glass_L",
                new Vector3(91.35f, 1.65f, -36.28f),
                new Vector3(0.75f, 1.10f, 0.03f),
                windowGlassMat);
            CreatePrimitiveCube(wallGroup.transform, "Door_Glass_R",
                new Vector3(92.65f, 1.65f, -36.28f),
                new Vector3(0.75f, 1.10f, 0.03f),
                windowGlassMat);
            CreatePrimitiveCube(wallGroup.transform, "Door_PushBar",
                new Vector3(92.0f, 1.05f, -36.26f),
                new Vector3(2.3f, 0.06f, 0.06f),
                brassGoldMat);

            // Kitchen Subway Tile Splashback (Full 3-Wall Wrap: East, North, South)
            // East Wall flush behind Cook Line counters
            CreatePrimitiveCube(wallGroup.transform, "Splashback_KitchenSubwayTile_East",
                new Vector3(121.90f, 1.45f, -20.89f),
                new Vector3(0.04f, 2.90f, 13.5f),
                subwayTileMat);

            // North Wall flush behind Ingredient Container counters (Z = -14.85)
            CreatePrimitiveCube(wallGroup.transform, "Splashback_KitchenSubwayTile_North",
                new Vector3(116.25f, 1.45f, -14.85f),
                new Vector3(11.8f, 2.90f, 0.04f),
                subwayTileMat);

            // South Wall flush behind Deep Fryer & Beverage counters (Z = -27.05)
            CreatePrimitiveCube(wallGroup.transform, "Splashback_KitchenSubwayTile_South",
                new Vector3(116.25f, 1.45f, -27.05f),
                new Vector3(11.8f, 2.90f, 0.04f),
                subwayTileMat);

            // Sanitary Stainless Steel Cove Baseboards (0.12m high hygiene code trim)
            CreatePrimitiveCube(wallGroup.transform, "KitchenCoving_East",
                new Vector3(121.88f, 0.06f, -20.89f),
                new Vector3(0.06f, 0.12f, 13.5f),
                stainlessSteelMat);
            CreatePrimitiveCube(wallGroup.transform, "KitchenCoving_North",
                new Vector3(116.25f, 0.06f, -14.83f),
                new Vector3(11.8f, 0.12f, 0.06f),
                stainlessSteelMat);
            CreatePrimitiveCube(wallGroup.transform, "KitchenCoving_South",
                new Vector3(116.25f, 0.06f, -27.03f),
                new Vector3(11.8f, 0.12f, 0.06f),
                stainlessSteelMat);

            // Heavy-Duty Stainless Steel Corner Guards (External Wall Corners)
            CreatePrimitiveCube(wallGroup.transform, "CornerGuard_NE",
                new Vector3(121.88f, 1.45f, -14.83f),
                new Vector3(0.08f, 2.90f, 0.08f),
                stainlessSteelMat);
            CreatePrimitiveCube(wallGroup.transform, "CornerGuard_SE",
                new Vector3(121.88f, 1.45f, -27.03f),
                new Vector3(0.08f, 2.90f, 0.08f),
                stainlessSteelMat);

            // Official Framed Department of Public Health Certificate of Inspection ("GRADE A")
            Vector3 certPos = new Vector3(110.60f, 1.85f, -16.20f);
            CreatePrimitiveCube(wallGroup.transform, "HealthCert_Frame",
                certPos,
                new Vector3(0.04f, 0.48f, 0.38f),
                brassGoldMat);
            CreatePrimitiveCube(wallGroup.transform, "HealthCert_Paper",
                certPos + new Vector3(0.02f, 0f, 0f),
                new Vector3(0.02f, 0.42f, 0.32f),
                awningWhiteMat);
            CreateWallSignUI(wallGroup.transform,
                certPos + new Vector3(0.035f, 0.08f, 0f),
                Quaternion.Euler(0f, 90f, 0f),
                new Vector2(200f, 60f), 0.0050f, "GRADE A", 28f, new Color(0.1f, 0.5f, 0.15f));
            CreateWallSignUI(wallGroup.transform,
                certPos + new Vector3(0.035f, -0.08f, 0f),
                Quaternion.Euler(0f, 90f, 0f),
                new Vector2(240f, 50f), 0.0035f, "SANITARY INSPECTION\nSCORE 100", 18f, new Color(0.2f, 0.2f, 0.2f));

            // OSHA / Health Code Sign: "EMPLOYEES MUST WASH HANDS"
            Vector3 washSignPos = new Vector3(121.86f, 1.95f, -21.40f);
            CreatePrimitiveCube(wallGroup.transform, "WashHands_SignBack",
                washSignPos,
                new Vector3(0.03f, 0.22f, 0.48f),
                neonCyanMat);
            CreateWallSignUI(wallGroup.transform,
                washSignPos + new Vector3(-0.02f, 0f, 0f),
                Quaternion.Euler(0f, 270f, 0f),
                new Vector2(280f, 60f), 0.0038f, "EMPLOYEES MUST\nWASH HANDS", 18f, Color.white);
        }

        // ── 3. Luxurious Diner Booths & Bistro Seating ─────────────────────────

        private void BuildDinerBoothsAndDiningSets(Transform parent)
        {
            GameObject diningGroup = new GameObject("03_LuxuryDinerBooths");
            diningGroup.transform.SetParent(parent, false);

            // Table Coordinates:
            // West Column (Booths): Tables 2, 4, 6 at X = 92.74
            // Center Column (Bistros): Tables 1, 3, 5 at X = 100.74
            int[] westTableNums = new int[] { 2, 4, 6 };
            float[] westTableZ = new float[] { -14.84f, -22.34f, -29.84f };

            for (int i = 0; i < westTableNums.Length; i++)
            {
                int num = westTableNums[i];
                float z = westTableZ[i];
                Vector3 tablePos = new Vector3(92.74f, 0.0f, z);

                GameObject boothRoot = new GameObject($"DinerBooth_Table_{num}");
                boothRoot.transform.SetParent(diningGroup.transform, false);

                // Polished Walnut Dining Tabletop with Brass Edge Trim
                CreatePrimitiveCube(boothRoot.transform, "TableTop_Walnut",
                    tablePos + new Vector3(0f, 0.76f, 0f),
                    new Vector3(2.40f, 0.06f, 1.30f),
                    wainscotingMat);
                CreatePrimitiveCube(boothRoot.transform, "TableTop_BrassRim",
                    tablePos + new Vector3(0f, 0.758f, 0f),
                    new Vector3(2.44f, 0.05f, 1.34f),
                    brassGoldMat);
                // Center Cast-Iron Pedestal
                CreatePrimitiveCylinder(boothRoot.transform, "Table_Pedestal",
                    tablePos + new Vector3(0f, 0.38f, 0f),
                    new Vector3(0.24f, 0.38f, 0.24f),
                    windowFrameMat);

                // North & South Double-Sided High-Back Fluted Banquette Benches
                float[] benchOffsets = new float[] { 0.95f, -0.95f };
                string[] benchLabels = new string[] { "North", "South" };

                for (int b = 0; b < 2; b++)
                {
                    float bZ = z + benchOffsets[b];
                    Vector3 benchPos = new Vector3(92.74f, 0f, bZ);

                    // Wooden Banquette Plinth Base
                    CreatePrimitiveCube(boothRoot.transform, $"BenchBase_{benchLabels[b]}",
                        benchPos + new Vector3(0f, 0.22f, 0f),
                        new Vector3(2.35f, 0.44f, 0.55f),
                        wainscotingMat);

                    // Plush Tufted Burgundy Leather Seat Cushion
                    CreatePrimitiveCube(boothRoot.transform, $"SeatCushion_{benchLabels[b]}",
                        benchPos + new Vector3(0f, 0.47f, 0f),
                        new Vector3(2.30f, 0.12f, 0.50f),
                        boothBurgundyMat);

                    // High Fluted Leather Backrest
                    float backOffset = (b == 0) ? 0.22f : -0.22f;
                    CreatePrimitiveCube(boothRoot.transform, $"Backrest_{benchLabels[b]}",
                        benchPos + new Vector3(0f, 0.88f, backOffset),
                        new Vector3(2.30f, 0.70f, 0.12f),
                        boothBurgundyMat);

                    // Mahogany End Cap Panels
                    CreatePrimitiveCube(boothRoot.transform, $"EndCap_L_{benchLabels[b]}",
                        benchPos + new Vector3(-1.18f, 0.60f, backOffset * 0.5f),
                        new Vector3(0.06f, 0.95f, 0.60f),
                        wainscotingMat);
                    CreatePrimitiveCube(boothRoot.transform, $"EndCap_R_{benchLabels[b]}",
                        benchPos + new Vector3(1.18f, 0.60f, backOffset * 0.5f),
                        new Vector3(0.06f, 0.95f, 0.60f),
                        wainscotingMat);
                }

                // Align existing AI Customer Seating Anchors for DineInCustomerController
                GameObject seatL = GameObject.Find($"EggStool_{num}_Left");
                if (seatL != null) seatL.transform.position = tablePos + new Vector3(-0.45f, 0.48f, 0.85f);

                GameObject seatR = GameObject.Find($"EggStool_{num}_Right");
                if (seatR != null) seatR.transform.position = tablePos + new Vector3(0.45f, 0.48f, -0.85f);

                // Tabletop Dressings & Ambience
                BuildTabletopSetDressings(boothRoot.transform, tablePos, num);
            }

            // Center Column Bistro Tables: Tables 1, 3, 5 at X = 100.74
            int[] eastTableNums = new int[] { 1, 3, 5 };
            float[] eastTableZ = new float[] { -14.84f, -22.34f, -29.84f };

            for (int i = 0; i < eastTableNums.Length; i++)
            {
                int num = eastTableNums[i];
                float z = eastTableZ[i];
                Vector3 tablePos = new Vector3(100.74f, 0.0f, z);

                GameObject bistroRoot = new GameObject($"BistroSet_Table_{num}");
                bistroRoot.transform.SetParent(diningGroup.transform, false);

                // Round Walnut Bistro Tabletop
                CreatePrimitiveCylinder(bistroRoot.transform, "TableTop_Round",
                    tablePos + new Vector3(0f, 0.76f, 0f),
                    new Vector3(1.60f, 0.04f, 1.60f),
                    wainscotingMat);
                CreatePrimitiveCylinder(bistroRoot.transform, "TableRim_Brass",
                    tablePos + new Vector3(0f, 0.758f, 0f),
                    new Vector3(1.64f, 0.035f, 1.64f),
                    brassGoldMat);
                CreatePrimitiveCylinder(bistroRoot.transform, "Pedestal_Base",
                    tablePos + new Vector3(0f, 0.38f, 0f),
                    new Vector3(0.20f, 0.38f, 0.20f),
                    windowFrameMat);

                // Pair of Bentwood Bistro Chairs with Emerald Leather Seats
                float[] chairZOffsets = new float[] { 0.85f, -0.85f };
                string[] chairSides = new string[] { "Left", "Right" };

                for (int c = 0; c < 2; c++)
                {
                    Vector3 chairPos = tablePos + new Vector3(0f, 0f, chairZOffsets[c]);
                    GameObject chair = GameObject.Find($"EggStool_{num}_{chairSides[c]}");
                    if (chair != null) chair.transform.position = chairPos + new Vector3(0f, 0.46f, 0f);

                    // Chair Seat Cushion
                    CreatePrimitiveCylinder(bistroRoot.transform, $"ChairSeat_{c + 1}",
                        chairPos + new Vector3(0f, 0.46f, 0f),
                        new Vector3(0.48f, 0.05f, 0.48f),
                        boothEmeraldMat);

                    // Chair Wooden Legs & Frame
                    CreatePrimitiveCylinder(bistroRoot.transform, $"ChairLegs_{c + 1}",
                        chairPos + new Vector3(0f, 0.23f, 0f),
                        new Vector3(0.38f, 0.23f, 0.38f),
                        wainscotingMat);

                    // Curved Wooden Spindle Backrest
                    float backZ = chairZOffsets[c] > 0 ? 0.22f : -0.22f;
                    CreatePrimitiveCube(bistroRoot.transform, $"ChairBack_{c + 1}",
                        chairPos + new Vector3(0f, 0.72f, backZ),
                        new Vector3(0.46f, 0.36f, 0.04f),
                        wainscotingMat);
                }

                // Tabletop Dressings
                BuildTabletopSetDressings(bistroRoot.transform, tablePos, num);
            }
        }

        private void BuildTabletopSetDressings(Transform parent, Vector3 tablePos, int tableNum)
        {
            Vector3 center = tablePos + new Vector3(0f, 0.79f, 0f);

            // Table Linen Runner
            CreatePrimitiveCube(parent, "LinenRunner",
                center + new Vector3(0f, 0.005f, 0f),
                new Vector3(0.45f, 0.005f, 0.85f),
                awningWhiteMat);

            // Classic Glass Ketchup Bottle (Red cap)
            CreatePrimitiveCylinder(parent, "KetchupBottle",
                center + new Vector3(-0.16f, 0.08f, -0.12f),
                new Vector3(0.065f, 0.16f, 0.065f),
                ketchupRedMat);

            // Classic Mustard Squeeze Bottle (Yellow cap)
            CreatePrimitiveCylinder(parent, "MustardBottle",
                center + new Vector3(-0.07f, 0.08f, -0.12f),
                new Vector3(0.065f, 0.15f, 0.065f),
                mustardYellowMat);

            // Glass Salt & Pepper Shakers with Chrome Tops
            CreatePrimitiveCylinder(parent, "SaltShaker",
                center + new Vector3(-0.15f, 0.05f, 0.08f),
                new Vector3(0.045f, 0.10f, 0.045f),
                awningWhiteMat);
            CreatePrimitiveCylinder(parent, "PepperShaker",
                center + new Vector3(-0.07f, 0.05f, 0.08f),
                new Vector3(0.045f, 0.10f, 0.045f),
                checkerboardDarkMat);

            // Brass Mini Tabletop Candle Jar / Mood Lamp with Golden Flame Glow
            CreatePrimitiveCylinder(parent, "CandleJar",
                center + new Vector3(0.12f, 0.06f, 0.00f),
                new Vector3(0.07f, 0.12f, 0.07f),
                brassGoldMat);
            CreatePointLight(parent, "CandleLight",
                center + new Vector3(0.12f, 0.16f, 0.00f),
                new Color(1.0f, 0.88f, 0.65f),
                1.4f, 2.2f);

            // Diegetic Wooden Table Number Tent Sign (Two-sided, clean TextMeshPro)
            Vector3 signPos = center + new Vector3(0.18f, 0.08f, -0.16f);
            CreatePrimitiveCube(parent, "TableTent_Wood",
                signPos,
                new Vector3(0.18f, 0.14f, 0.10f),
                wainscotingMat);

            // Front & Back clean labels
            CreateWallSignUI(parent, signPos + new Vector3(0f, 0.01f, 0.055f),
                Quaternion.Euler(0f, 0f, 0f),
                new Vector2(100f, 80f), 0.0016f, $"#{tableNum}", 40f, new Color(0.95f, 0.85f, 0.55f));
            CreateWallSignUI(parent, signPos + new Vector3(0f, 0.01f, -0.055f),
                Quaternion.Euler(0f, 180f, 0f),
                new Vector2(100f, 80f), 0.0016f, $"#{tableNum}", 40f, new Color(0.95f, 0.85f, 0.55f));
        }

        // ── 4. Host Stand, Waiting Lounge & Greenery ──────────────────────────

        private void BuildHostStandAndWaitingLounge(Transform parent)
        {
            GameObject loungeGroup = new GameObject("04_HostStandAndLounge");
            loungeGroup.transform.SetParent(parent, false);

            Vector3 hostPos = new Vector3(92.0f, 0f, -33.2f);

            // Maître D' / Host Podium
            CreatePrimitiveCube(loungeGroup.transform, "Host_PodiumBody",
                hostPos + new Vector3(0f, 0.60f, 0f),
                new Vector3(0.70f, 1.20f, 0.55f),
                wainscotingMat);
            CreatePrimitiveCube(loungeGroup.transform, "Host_SlantedTop",
                hostPos + new Vector3(0f, 1.22f, 0f),
                new Vector3(0.75f, 0.06f, 0.60f),
                marbleBarMat);

            // Reservation Ledger Book & Brass Desk Lamp
            CreatePrimitiveCube(loungeGroup.transform, "GuestBook",
                hostPos + new Vector3(-0.12f, 1.26f, 0f),
                new Vector3(0.28f, 0.03f, 0.36f),
                boothBurgundyMat);
            CreatePrimitiveCylinder(loungeGroup.transform, "DeskLamp_Base",
                hostPos + new Vector3(0.22f, 1.28f, 0.12f),
                new Vector3(0.12f, 0.14f, 0.12f),
                brassGoldMat);
            CreatePointLight(loungeGroup.transform, "HostLampLight",
                hostPos + new Vector3(0.22f, 1.45f, 0.12f),
                new Color(1.0f, 0.90f, 0.70f),
                2.0f, 3.2f);

            // 2.6m Golden Velvet Guest Waiting Bench
            Vector3 benchPos = new Vector3(95.6f, 0f, -34.8f);
            CreatePrimitiveCube(loungeGroup.transform, "WaitingBench_WoodFrame",
                benchPos + new Vector3(0f, 0.22f, 0f),
                new Vector3(2.60f, 0.44f, 0.65f),
                wainscotingMat);
            CreatePrimitiveCube(loungeGroup.transform, "WaitingBench_VelvetCushion",
                benchPos + new Vector3(0f, 0.48f, 0f),
                new Vector3(2.55f, 0.14f, 0.60f),
                velvetPlushMat);

            // Vintage Brass Standing Coat & Umbrella Rack
            Vector3 coatPos = new Vector3(89.8f, 0f, -34.8f);
            CreatePrimitiveCylinder(loungeGroup.transform, "CoatRack_Pole",
                coatPos + new Vector3(0f, 1.05f, 0f),
                new Vector3(0.08f, 2.10f, 0.08f),
                brassGoldMat);
            CreatePrimitiveCylinder(loungeGroup.transform, "CoatRack_Base",
                coatPos + new Vector3(0f, 0.05f, 0f),
                new Vector3(0.48f, 0.10f, 0.48f),
                brassGoldMat);
            CreatePrimitiveCylinder(loungeGroup.transform, "Umbrella_Ring",
                coatPos + new Vector3(0f, 0.65f, 0f),
                new Vector3(0.36f, 0.04f, 0.36f),
                brassGoldMat);

            // Real 3D Indoor Potted Ficus Trees (from CityLife library)
            InstantiatePlant(loungeGroup.transform, "Plant_Entrance_L", new Vector3(89.6f, 0f, -33.2f), "Prop_Plant_01");
            InstantiatePlant(loungeGroup.transform, "Plant_Entrance_R", new Vector3(94.8f, 0f, -33.2f), "Prop_Plant_02");
        }

        // ── 5. Diner Service Bar & Coffee Station ─────────────────────────────

        private void BuildDinerServiceBarAndCoffeeStation(Transform parent)
        {
            GameObject barGroup = new GameObject("05_ServiceBarAndCoffee");
            barGroup.transform.SetParent(parent, false);

            Vector3 barPos = new Vector3(104.5f, 0f, -11.0f);

            // Polished White Carrera Marble Bar Counter (5.5m long)
            CreatePrimitiveCube(barGroup.transform, "Bar_BaseCabinet",
                barPos + new Vector3(0f, 0.52f, 0f),
                new Vector3(5.50f, 1.04f, 0.85f),
                wainscotingMat);
            CreatePrimitiveCube(barGroup.transform, "Bar_MarbleTop",
                barPos + new Vector3(0f, 1.06f, 0f),
                new Vector3(5.70f, 0.08f, 1.00f),
                marbleBarMat);

            // Continuous Brass Footrail
            CreatePrimitiveCube(barGroup.transform, "Bar_BrassFootrail",
                barPos + new Vector3(0f, 0.16f, -0.48f),
                new Vector3(5.60f, 0.05f, 0.05f),
                brassGoldMat);

            // 4 Classic Chrome Swivel Diner Barstools with Red Padded Seats
            float[] stoolOffsetsX = new float[] { -1.9f, -0.6f, 0.6f, 1.9f };
            for (int s = 0; s < stoolOffsetsX.Length; s++)
            {
                Vector3 sPos = barPos + new Vector3(stoolOffsetsX[s], 0f, -0.85f);
                CreatePrimitiveCylinder(barGroup.transform, $"StoolPedestal_{s + 1}",
                    sPos + new Vector3(0f, 0.34f, 0f),
                    new Vector3(0.08f, 0.68f, 0.08f),
                    stainlessSteelMat);
                CreatePrimitiveCylinder(barGroup.transform, $"StoolBase_{s + 1}",
                    sPos + new Vector3(0f, 0.03f, 0f),
                    new Vector3(0.42f, 0.06f, 0.42f),
                    stainlessSteelMat);
                CreatePrimitiveCylinder(barGroup.transform, $"StoolSeat_{s + 1}",
                    sPos + new Vector3(0f, 0.69f, 0f),
                    new Vector3(0.40f, 0.08f, 0.40f),
                    awningRedMat);
            }

            // Commercial Stainless Steel Dual-Group Espresso Machine
            Vector3 espressoPos = barPos + new Vector3(-1.8f, 1.10f, 0.10f);
            CreatePrimitiveCube(barGroup.transform, "Espresso_Body",
                espressoPos + new Vector3(0f, 0.28f, 0f),
                new Vector3(0.85f, 0.56f, 0.55f),
                stainlessSteelMat);
            CreatePrimitiveCylinder(barGroup.transform, "Portafilter_1",
                espressoPos + new Vector3(-0.20f, 0.22f, -0.28f),
                new Vector3(0.06f, 0.04f, 0.18f),
                brassGoldMat);
            CreatePrimitiveCylinder(barGroup.transform, "Portafilter_2",
                espressoPos + new Vector3(0.20f, 0.22f, -0.28f),
                new Vector3(0.06f, 0.04f, 0.18f),
                brassGoldMat);

            // Glass Drip Coffee Carafes (Regular & Decaf) on heating pad
            Vector3 carafePos = barPos + new Vector3(-0.75f, 1.10f, 0.05f);
            CreatePrimitiveCube(barGroup.transform, "CoffeeHeatingPad",
                carafePos + new Vector3(0f, 0.02f, 0f),
                new Vector3(0.65f, 0.04f, 0.35f),
                checkerboardDarkMat);
            CreatePrimitiveCylinder(barGroup.transform, "Carafe_Regular",
                carafePos + new Vector3(-0.16f, 0.16f, 0f),
                new Vector3(0.18f, 0.26f, 0.18f),
                windowGlassMat);
            CreatePrimitiveCylinder(barGroup.transform, "Carafe_Decaf",
                carafePos + new Vector3(0.16f, 0.16f, 0f),
                new Vector3(0.18f, 0.26f, 0.18f),
                windowGlassMat);

            // Tiered Glass Pastry Cake Dome with Chocolate Cherry Layer Cake
            Vector3 cakePos = barPos + new Vector3(0.85f, 1.10f, 0.05f);
            CreatePrimitiveCylinder(barGroup.transform, "CakePedestal",
                cakePos + new Vector3(0f, 0.06f, 0f),
                new Vector3(0.42f, 0.12f, 0.42f),
                marbleBarMat);
            CreatePrimitiveCylinder(barGroup.transform, "LayerCake",
                cakePos + new Vector3(0f, 0.16f, 0f),
                new Vector3(0.32f, 0.14f, 0.32f),
                wainscotingMat);
            CreatePrimitiveCylinder(barGroup.transform, "GlassDome",
                cakePos + new Vector3(0f, 0.26f, 0f),
                new Vector3(0.38f, 0.28f, 0.38f),
                windowGlassMat);

            // Chrome Napkin Dispenser & Straw Holder
            CreatePrimitiveCube(barGroup.transform, "NapkinDispenser",
                barPos + new Vector3(2.1f, 1.18f, 0f),
                new Vector3(0.18f, 0.20f, 0.14f),
                stainlessSteelMat);
            CreatePrimitiveCylinder(barGroup.transform, "StrawHolder",
                barPos + new Vector3(2.4f, 1.20f, 0f),
                new Vector3(0.10f, 0.24f, 0.10f),
                stainlessSteelMat);
        }

        // ── 6. Exhibition Kitchen & Pass-Through ───────────────────────────────

        private void BuildExhibitionKitchenAndPassThrough(Transform parent)
        {
            GameObject kitchenGroup = new GameObject("06_ExhibitionKitchen");
            kitchenGroup.transform.SetParent(parent, false);

            // 1. Pass-Through Partition Counter Wall at X = 110.64
            Vector3 passCenter = new Vector3(110.64f, 0f, -20.69f);

            // Polished Carrera Marble Serving Pass Shelf
            CreatePrimitiveCube(kitchenGroup.transform, "PassThrough_MarbleShelf",
                passCenter + new Vector3(0f, 1.28f, 0f),
                new Vector3(0.65f, 0.08f, 3.80f),
                marbleBarMat);

            // Overhead Valance & Dual Quartz Warming Heat Lamps
            CreatePrimitiveCube(kitchenGroup.transform, "PassThrough_ValanceHeader",
                passCenter + new Vector3(0f, 2.10f, 0f),
                new Vector3(0.40f, 0.16f, 3.80f),
                wainscotingMat);
            CreatePrimitiveCube(kitchenGroup.transform, "HeatLamp_Housing",
                passCenter + new Vector3(0f, 1.98f, 0f),
                new Vector3(0.18f, 0.08f, 3.20f),
                stainlessSteelMat);

            // Food Holding Amber Light Glow (Keeps plated food warm at 145°F)
            CreatePointLight(kitchenGroup.transform, "PassThrough_HeatLight",
                passCenter + new Vector3(0f, 1.75f, 0f),
                new Color(1.0f, 0.72f, 0.22f),
                3.0f, 4.0f);

            // Stainless Steel Order Ticket Clip Rail with Printed Tickets
            CreatePrimitiveCube(kitchenGroup.transform, "TicketRail",
                passCenter + new Vector3(-0.18f, 1.92f, 0f),
                new Vector3(0.04f, 0.04f, 3.40f),
                stainlessSteelMat);

            float[] ticketZ = new float[] { -21.8f, -21.2f, -20.7f, -20.2f, -19.6f };
            for (int t = 0; t < ticketZ.Length; t++)
            {
                CreatePrimitiveCube(kitchenGroup.transform, $"Ticket_{t + 1}",
                    new Vector3(110.45f, 1.84f, ticketZ[t]),
                    new Vector3(0.01f, 0.16f, 0.18f),
                    awningWhiteMat);
            }

            // Rotating Ticket Order Carousel Tree on pass shelf
            Vector3 carouselPos = passCenter + new Vector3(0.15f, 1.34f, -1.20f);
            CreatePrimitiveCylinder(kitchenGroup.transform, "TicketCarousel_Base",
                carouselPos,
                new Vector3(0.14f, 0.04f, 0.14f),
                stainlessSteelMat);
            CreatePrimitiveCylinder(kitchenGroup.transform, "TicketCarousel_Spindle",
                carouselPos + new Vector3(0f, 0.18f, 0f),
                new Vector3(0.03f, 0.32f, 0.03f),
                chromePolishedMat);
            CreatePrimitiveCylinder(kitchenGroup.transform, "TicketCarousel_Wheel",
                carouselPos + new Vector3(0f, 0.32f, 0f),
                new Vector3(0.26f, 0.03f, 0.26f),
                stainlessSteelMat);

            // Brass Service Call Bell ("Ding! Order up!")
            CreatePrimitiveCylinder(kitchenGroup.transform, "ServiceBell",
                passCenter + new Vector3(0.18f, 1.34f, 0.50f),
                new Vector3(0.12f, 0.08f, 0.12f),
                brassGoldMat);

            // Expeditor Dry-Erase Order Board mounted on partition wall
            Vector3 memoPos = new Vector3(110.60f, 1.80f, -18.70f);
            CreatePrimitiveCube(kitchenGroup.transform, "ExpeditorBoard_Frame",
                memoPos,
                new Vector3(0.04f, 0.45f, 0.65f),
                stainlessSteelMat);
            CreatePrimitiveCube(kitchenGroup.transform, "ExpeditorBoard_Surface",
                memoPos + new Vector3(0.02f, 0f, 0f),
                new Vector3(0.02f, 0.39f, 0.59f),
                awningWhiteMat);
            CreateWallSignUI(kitchenGroup.transform,
                memoPos + new Vector3(0.035f, 0.08f, 0f),
                Quaternion.Euler(0f, 90f, 0f),
                new Vector2(250f, 40f), 0.0035f, "ORDER EXPEDITOR", 18f, new Color(0.1f, 0.1f, 0.1f));
            CreateWallSignUI(kitchenGroup.transform,
                memoPos + new Vector3(0.035f, -0.06f, 0f),
                Quaternion.Euler(0f, 90f, 0f),
                new Vector2(250f, 40f), 0.0030f, "RUSH TABLE 2 -- 86 TOMATOES", 15f, new Color(0.7f, 0.1f, 0.1f));

            // Double Swinging Saloon Doors leading to dining room walkway (Z = -17.5)
            Vector3 doorWalkway = new Vector3(110.64f, 0f, -17.50f);
            CreatePrimitiveCube(kitchenGroup.transform, "SaloonDoor_North",
                doorWalkway + new Vector3(0f, 0.95f, 0.45f),
                new Vector3(0.05f, 1.10f, 0.75f),
                wainscotingMat);
            CreatePrimitiveCube(kitchenGroup.transform, "SaloonDoor_South",
                doorWalkway + new Vector3(0f, 0.95f, -0.45f),
                new Vector3(0.05f, 1.10f, 0.75f),
                wainscotingMat);
            CreatePrimitiveCylinder(kitchenGroup.transform, "SaloonPorthole_N",
                doorWalkway + new Vector3(0f, 1.15f, 0.45f),
                new Vector3(0.24f, 0.06f, 0.24f),
                brassGoldMat, Quaternion.Euler(0f, 0f, 90f));
            CreatePrimitiveCylinder(kitchenGroup.transform, "SaloonPorthole_S",
                doorWalkway + new Vector3(0f, 1.15f, -0.45f),
                new Vector3(0.24f, 0.06f, 0.24f),
                brassGoldMat, Quaternion.Euler(0f, 0f, 90f));

            // 2. Commercial Stainless Range Hood Canopy & ANSUL Fire Suppression System
            // Hood Canopy over Cook Line (X = 121.0, Z = -18.75)
            Vector3 hoodCenter = new Vector3(121.0f, 2.50f, -18.75f);
            CreatePrimitiveCube(kitchenGroup.transform, "ExhaustCanopy_Body",
                hoodCenter,
                new Vector3(1.35f, 0.60f, 4.80f),
                stainlessSteelMat);
            CreatePrimitiveCube(kitchenGroup.transform, "ExhaustCanopy_BaffleLip",
                hoodCenter + new Vector3(-0.68f, -0.28f, 0f),
                new Vector3(0.10f, 0.08f, 4.80f),
                stainlessSteelMat);

            // Stainless Steel Angled Grease Baffle Filters (Removable Commercial Filters)
            for (int f = 0; f < 5; f++)
            {
                float fz = -20.60f + (f * 0.92f);
                CreatePrimitiveCube(kitchenGroup.transform, $"BaffleFilter_{f + 1}",
                    new Vector3(121.10f, 2.32f, fz),
                    new Vector3(0.75f, 0.04f, 0.75f),
                    stainlessSteelMat, Quaternion.Euler(30f, 0f, 0f));
            }

            // Twin Vertical Rooftop Exhaust Ducts
            CreatePrimitiveCube(kitchenGroup.transform, "RoofDuct_1",
                new Vector3(121.0f, 3.20f, -17.50f),
                new Vector3(0.48f, 0.85f, 0.48f),
                stainlessSteelMat);
            CreatePrimitiveCube(kitchenGroup.transform, "RoofDuct_2",
                new Vector3(121.0f, 3.20f, -20.00f),
                new Vector3(0.48f, 0.85f, 0.48f),
                stainlessSteelMat);

            // Extended Fryer Exhaust Canopy over Deep Fryer (Z = -26.14)
            Vector3 fryerHoodCenter = new Vector3(118.14f, 2.50f, -26.14f);
            CreatePrimitiveCube(kitchenGroup.transform, "FryerExhaust_Body",
                fryerHoodCenter,
                new Vector3(2.40f, 0.55f, 1.30f),
                stainlessSteelMat);
            CreatePrimitiveCube(kitchenGroup.transform, "FryerExhaust_Duct",
                fryerHoodCenter + new Vector3(0f, 0.65f, 0f),
                new Vector3(0.45f, 0.85f, 0.45f),
                stainlessSteelMat);

            // Recessed Cook Line Halogen Downlights
            CreatePointLight(kitchenGroup.transform, "HoodLight_Stoves",
                new Vector3(120.9f, 2.20f, -17.80f),
                new Color(1.0f, 0.94f, 0.80f),
                3.2f, 4.0f);
            CreatePointLight(kitchenGroup.transform, "HoodLight_Fryer",
                new Vector3(118.14f, 2.20f, -25.90f),
                new Color(1.0f, 0.94f, 0.80f),
                3.0f, 4.0f);

            // ANSUL R-102 Industrial Fire Suppression System
            // Red fire suppression agent storage tank mounted to cook line wall
            Vector3 ansulTankPos = new Vector3(121.84f, 2.20f, -15.90f);
            CreatePrimitiveCylinder(kitchenGroup.transform, "Ansul_Tank",
                ansulTankPos,
                new Vector3(0.24f, 0.65f, 0.24f),
                ansulPipingMat);
            CreatePrimitiveCylinder(kitchenGroup.transform, "Ansul_TankCap",
                ansulTankPos + new Vector3(0f, 0.35f, 0f),
                new Vector3(0.18f, 0.08f, 0.18f),
                brassGoldMat);
            CreatePrimitiveCylinder(kitchenGroup.transform, "Ansul_PressureGauge",
                ansulTankPos + new Vector3(-0.14f, 0.20f, 0f),
                new Vector3(0.08f, 0.04f, 0.08f),
                awningWhiteMat, Quaternion.Euler(0f, 0f, 90f));

            // Emergency Manual Pull Station Box
            Vector3 pullBoxPos = new Vector3(121.85f, 1.45f, -15.90f);
            CreatePrimitiveCube(kitchenGroup.transform, "Ansul_PullStation",
                pullBoxPos,
                new Vector3(0.06f, 0.22f, 0.18f),
                ansulPipingMat);
            CreatePrimitiveCylinder(kitchenGroup.transform, "Ansul_PullRing",
                pullBoxPos + new Vector3(-0.04f, -0.04f, 0f),
                new Vector3(0.06f, 0.02f, 0.06f),
                brassGoldMat, Quaternion.Euler(0f, 0f, 90f));

            // Stainless Steel Ansul Piping running under hood canopy
            CreatePrimitiveCube(kitchenGroup.transform, "Ansul_DistributionPipe",
                new Vector3(120.65f, 2.22f, -18.75f),
                new Vector3(0.04f, 0.04f, 4.40f),
                chromePolishedMat);

            // 4 Down-Firing Yellow/Brass Fire Suppression Nozzles aimed over Stoves & Sinks
            float[] nozzleZ = new float[] { -17.14f, -18.64f, -20.14f };
            for (int n = 0; n < nozzleZ.Length; n++)
            {
                CreatePrimitiveCylinder(kitchenGroup.transform, $"Ansul_Nozzle_{n + 1}",
                    new Vector3(120.65f, 2.16f, nozzleZ[n]),
                    new Vector3(0.06f, 0.08f, 0.06f),
                    brassGoldMat);
            }

            // 3. The Scullery / Commercial Dishwashing Pit (Sink & Plates Area)
            // Commercial Pre-Rinse High-Pressure Spring Faucet Assembly above Sink (Z = -20.14)
            Vector3 sinkFaucetPos = new Vector3(120.92f, 0.90f, -20.14f);
            // Vertical Chrome Riser Column
            CreatePrimitiveCylinder(kitchenGroup.transform, "PreRinse_RiserColumn",
                sinkFaucetPos + new Vector3(0f, 0.45f, 0f),
                new Vector3(0.04f, 0.90f, 0.04f),
                chromePolishedMat);
            // Coiled Spring Arc Housing
            CreatePrimitiveCylinder(kitchenGroup.transform, "PreRinse_SpringHousing",
                sinkFaucetPos + new Vector3(-0.10f, 0.95f, 0f),
                new Vector3(0.12f, 0.25f, 0.12f),
                chromePolishedMat);
            // Flexible Downward Spray Valve with Squeeze Grip Handle
            CreatePrimitiveCylinder(kitchenGroup.transform, "PreRinse_SprayHose",
                sinkFaucetPos + new Vector3(-0.16f, 0.65f, 0f),
                new Vector3(0.03f, 0.45f, 0.03f),
                checkerboardDarkMat);
            CreatePrimitiveCylinder(kitchenGroup.transform, "PreRinse_SprayNozzle",
                sinkFaucetPos + new Vector3(-0.16f, 0.40f, 0f),
                new Vector3(0.08f, 0.10f, 0.08f),
                chromePolishedMat);

            // Dual Stainless Splash Shield Barriers (flanking SinkCounter to protect Stoves and Plates)
            CreatePrimitiveCube(kitchenGroup.transform, "Sink_SplashShield_North",
                new Vector3(121.14f, 1.15f, -19.39f),
                new Vector3(1.20f, 0.45f, 0.03f),
                stainlessSteelMat);
            CreatePrimitiveCube(kitchenGroup.transform, "Sink_SplashShield_South",
                new Vector3(121.14f, 1.15f, -20.89f),
                new Vector3(1.20f, 0.45f, 0.03f),
                stainlessSteelMat);

            // Chemical Dosing Station (Wall-mounted automatic detergent & sanitizer dispenser)
            Vector3 chemPos = new Vector3(121.84f, 1.60f, -20.85f);
            CreatePrimitiveCube(kitchenGroup.transform, "ChemicalDosing_Housing",
                chemPos,
                new Vector3(0.08f, 0.38f, 0.32f),
                stainlessSteelMat);
            CreatePrimitiveCube(kitchenGroup.transform, "Chemical_DetergentTubes",
                chemPos + new Vector3(-0.05f, 0f, -0.06f),
                new Vector3(0.04f, 0.24f, 0.08f),
                sanitizerBlueMat);
            CreatePrimitiveCube(kitchenGroup.transform, "Chemical_SanitizerTubes",
                chemPos + new Vector3(-0.05f, 0f, 0.06f),
                new Vector3(0.04f, 0.24f, 0.08f),
                plantGreenMat);

            // Color-Coded Sanitation Buckets (Red = Sanitizer, Green = Detergent)
            CreatePrimitiveCylinder(kitchenGroup.transform, "SanitizerBucket_Red",
                new Vector3(120.35f, 0.15f, -20.80f),
                new Vector3(0.26f, 0.28f, 0.26f),
                ketchupRedMat);
            CreatePrimitiveCylinder(kitchenGroup.transform, "SanitizerBucket_Green",
                new Vector3(120.35f, 0.15f, -21.15f),
                new Vector3(0.26f, 0.28f, 0.26f),
                plantGreenMat);

            // Commercial Polypropylene Peg Dish Racks (Standard 500x500mm Commercial Wash Racks)
            Vector3 rackPos = new Vector3(120.85f, 1.05f, -22.39f);
            CreatePrimitiveCube(kitchenGroup.transform, "DishRack_BluePeg",
                rackPos,
                new Vector3(0.50f, 0.12f, 0.50f),
                sanitizerBlueMat);
            // Washed ceramic plates resting in peg slots
            for (int p = 0; p < 4; p++)
            {
                CreatePrimitiveCylinder(kitchenGroup.transform, $"DryingPlate_{p + 1}",
                    rackPos + new Vector3(-0.15f + (p * 0.10f), 0.12f, 0f),
                    new Vector3(0.28f, 0.02f, 0.28f),
                    awningWhiteMat, Quaternion.Euler(0f, 0f, 90f));
            }

            // Wall-Mounted Commercial Lever Paper Towel Dispenser
            Vector3 towelPos = new Vector3(121.84f, 1.65f, -21.80f);
            CreatePrimitiveCube(kitchenGroup.transform, "PaperTowelDispenser",
                towelPos,
                new Vector3(0.12f, 0.36f, 0.28f),
                stainlessSteelMat);
            CreatePrimitiveCube(kitchenGroup.transform, "PaperTowel_Sheet",
                towelPos + new Vector3(-0.07f, -0.22f, 0f),
                new Vector3(0.02f, 0.12f, 0.22f),
                awningWhiteMat);

            // 4. Center Island Overhead Double-Deck Gantry & Cambro Food Prep Station
            // Spanning Island_Clear_1 (115.14) and Island_Cutting (116.64) at Z = -20.89
            Vector3 islandCenter = new Vector3(115.89f, 0f, -20.89f);

            // 4 Stainless Steel Tubular Support Stanchions
            Vector3[] stanchionOffsets = new Vector3[] {
                new Vector3(-1.35f, 1.10f, -0.65f),
                new Vector3(1.35f, 1.10f, -0.65f),
                new Vector3(-1.35f, 1.10f, 0.65f),
                new Vector3(1.35f, 1.10f, 0.65f),
            };
            for (int s = 0; s < stanchionOffsets.Length; s++)
            {
                CreatePrimitiveCylinder(kitchenGroup.transform, $"Gantry_Stanchion_{s + 1}",
                    islandCenter + stanchionOffsets[s],
                    new Vector3(0.04f, 2.20f, 0.04f),
                    chromePolishedMat);
            }

            // Lower Gantry Prep Shelf at Y = 1.85m
            CreatePrimitiveCube(kitchenGroup.transform, "GantryShelf_Lower",
                islandCenter + new Vector3(0f, 1.85f, 0f),
                new Vector3(2.90f, 0.04f, 1.45f),
                stainlessSteelMat);

            // Upper Gantry Storage Shelf at Y = 2.18m
            CreatePrimitiveCube(kitchenGroup.transform, "GantryShelf_Upper",
                islandCenter + new Vector3(0f, 2.18f, 0f),
                new Vector3(2.90f, 0.04f, 1.45f),
                stainlessSteelMat);

            // 6 Gastro-Norm Hotel Pans / Cambro Prep Inserts on Lower Shelf
            // 2 Large Stainless 1/3 Hotel Pans (Lettuce & Shredded Onions)
            CreatePrimitiveCube(kitchenGroup.transform, "HotelPan_Lettuce",
                islandCenter + new Vector3(-0.95f, 1.93f, -0.32f),
                new Vector3(0.55f, 0.12f, 0.35f),
                stainlessSteelMat);
            CreatePrimitiveCube(kitchenGroup.transform, "Lettuce_Contents",
                islandCenter + new Vector3(-0.95f, 1.98f, -0.32f),
                new Vector3(0.48f, 0.05f, 0.28f),
                plantGreenMat);

            CreatePrimitiveCube(kitchenGroup.transform, "HotelPan_Onions",
                islandCenter + new Vector3(-0.95f, 1.93f, 0.32f),
                new Vector3(0.55f, 0.12f, 0.35f),
                stainlessSteelMat);
            CreatePrimitiveCube(kitchenGroup.transform, "Onions_Contents",
                islandCenter + new Vector3(-0.95f, 1.98f, 0.32f),
                new Vector3(0.48f, 0.05f, 0.28f),
                awningWhiteMat);

            // 4 Polycarbonate Cambro Pans (Cheese, Patties, Tomato Slices, Pickles)
            float[] cambroZ = new float[] { -0.45f, -0.15f, 0.15f, 0.45f };
            Material[] cambroFillMats = new Material[] { mustardYellowMat, boothBurgundyMat, ketchupRedMat, plantGreenMat };
            string[] cambroNames = new string[] { "CheeseSlices", "BurgerPatties", "TomatoSlices", "Pickles" };

            for (int cp = 0; cp < 4; cp++)
            {
                CreatePrimitiveCube(kitchenGroup.transform, $"Cambro_{cambroNames[cp]}",
                    islandCenter + new Vector3(0.15f, 1.93f, cambroZ[cp]),
                    new Vector3(0.42f, 0.12f, 0.25f),
                    plasticHotelPanMat);
                CreatePrimitiveCube(kitchenGroup.transform, $"Fill_{cambroNames[cp]}",
                    islandCenter + new Vector3(0.15f, 1.97f, cambroZ[cp]),
                    new Vector3(0.36f, 0.05f, 0.20f),
                    cambroFillMats[cp]);
            }

            // Chef Squeeze Bottle Wire Caddy (Ketchup, Mustard, Aioli, Olive Oil)
            Vector3 caddyPos = islandCenter + new Vector3(0.95f, 1.90f, 0f);
            CreatePrimitiveCube(kitchenGroup.transform, "SqueezeBottle_Caddy",
                caddyPos,
                new Vector3(0.24f, 0.05f, 0.70f),
                chromePolishedMat);

            Material[] bottleMats = new Material[] { ketchupRedMat, mustardYellowMat, awningWhiteMat, brassGoldMat };
            string[] bottleNames = new string[] { "Ketchup", "Mustard", "Aioli", "OliveOil" };
            for (int b = 0; b < 4; b++)
            {
                float bz = -0.24f + (b * 0.16f);
                Vector3 bPos = caddyPos + new Vector3(0f, 0.14f, bz);
                CreatePrimitiveCylinder(kitchenGroup.transform, $"Bottle_{bottleNames[b]}",
                    bPos,
                    new Vector3(0.10f, 0.22f, 0.10f),
                    bottleMats[b]);
                CreatePrimitiveCylinder(kitchenGroup.transform, $"Nozzle_{bottleNames[b]}",
                    bPos + new Vector3(0f, 0.14f, 0f),
                    new Vector3(0.03f, 0.08f, 0.03f),
                    awningWhiteMat);
            }

            // High-Visibility Commercial Kitchen LED Digital Clock / Shift Timer
            Vector3 timerPos = islandCenter + new Vector3(0f, 2.05f, 0.74f);
            CreatePrimitiveCube(kitchenGroup.transform, "KitchenTimer_Housing",
                timerPos,
                new Vector3(0.65f, 0.22f, 0.06f),
                checkerboardDarkMat);
            CreateWallSignUI(kitchenGroup.transform,
                timerPos + new Vector3(0f, 0f, 0.04f),
                Quaternion.Euler(0f, 0f, 0f),
                new Vector2(300f, 80f), 0.0035f, "12:45 PM  --  SHIFT 2", 22f, new Color(0.2f, 1.0f, 0.3f));

            // Upper Gantry Equipment: 3 Nesting Stainless Mixing Bowls & Sheet Pans
            Vector3 bowlPos = islandCenter + new Vector3(-0.85f, 2.24f, 0f);
            CreatePrimitiveCylinder(kitchenGroup.transform, "MixingBowl_Large",
                bowlPos,
                new Vector3(0.38f, 0.12f, 0.38f),
                stainlessSteelMat);
            CreatePrimitiveCylinder(kitchenGroup.transform, "MixingBowl_Medium",
                bowlPos + new Vector3(0f, 0.05f, 0f),
                new Vector3(0.30f, 0.10f, 0.30f),
                chromePolishedMat);
            CreatePrimitiveCylinder(kitchenGroup.transform, "MixingBowl_Small",
                bowlPos + new Vector3(0f, 0.09f, 0f),
                new Vector3(0.22f, 0.08f, 0.22f),
                stainlessSteelMat);

            // Stack of Commercial Aluminum Baking Half-Sheet Pans
            Vector3 sheetPos = islandCenter + new Vector3(0.65f, 2.22f, 0f);
            for (int sp = 0; sp < 4; sp++)
            {
                CreatePrimitiveCube(kitchenGroup.transform, $"SheetPan_{sp + 1}",
                    sheetPos + new Vector3(0f, sp * 0.025f, 0f),
                    new Vector3(0.60f, 0.02f, 0.42f),
                    stainlessSteelMat);
            }

            // Suspended Hanging Chef Utensils from Gantry Lip
            string[] hangingTools = new string[] { "Tongs", "Spatula", "Skimmer", "Whisk" };
            for (int ht = 0; ht < 4; ht++)
            {
                float hz = -0.45f + (ht * 0.30f);
                CreatePrimitiveCube(kitchenGroup.transform, $"Tool_{hangingTools[ht]}",
                    islandCenter + new Vector3(-1.38f, 1.70f, hz),
                    new Vector3(0.02f, 0.26f, 0.05f),
                    chromePolishedMat);
            }

            // 5. South Line Commercial Fryer & Beverage Infrastructure
            // Stainless Steel Splash Barrier between Deep Fryer and Beverage Dispenser
            CreatePrimitiveCube(kitchenGroup.transform, "Fryer_Beverage_SplashBarrier",
                new Vector3(117.39f, 1.25f, -26.14f),
                new Vector3(0.04f, 0.65f, 1.20f),
                stainlessSteelMat);

            // Stack of Clean Red & White French Fry Cartons beside the Fryer
            Vector3 cartonPos = new Vector3(118.85f, 0.95f, -26.40f);
            for (int c = 0; c < 3; c++)
            {
                CreatePrimitiveCube(kitchenGroup.transform, $"FryCarton_{c + 1}",
                    cartonPos + new Vector3(0f, c * 0.06f, 0f),
                    new Vector3(0.14f, 0.08f, 0.12f),
                    (c % 2 == 0) ? ketchupRedMat : awningWhiteMat);
            }

            // Commercial Multi-Line Beverage Syrup Line Trunk running under counter
            CreatePrimitiveCylinder(kitchenGroup.transform, "BeverageSyrup_Trunk",
                new Vector3(116.64f, 0.50f, -26.85f),
                new Vector3(0.12f, 1.0f, 0.12f),
                checkerboardDarkMat);

            // 6. Utility Corner Commercial Metro Wire Storage Rack (X = 109.50, Z = -26.50)
            Vector3 rackCorner = new Vector3(109.45f, 0f, -26.50f);
            // 4 Upright Chrome Corner Posts
            CreatePrimitiveCylinder(kitchenGroup.transform, "Metro_Post_1",
                rackCorner + new Vector3(-0.35f, 1.10f, -0.40f),
                new Vector3(0.03f, 2.20f, 0.03f),
                chromePolishedMat);
            CreatePrimitiveCylinder(kitchenGroup.transform, "Metro_Post_2",
                rackCorner + new Vector3(0.35f, 1.10f, -0.40f),
                new Vector3(0.03f, 2.20f, 0.03f),
                chromePolishedMat);
            CreatePrimitiveCylinder(kitchenGroup.transform, "Metro_Post_3",
                rackCorner + new Vector3(-0.35f, 1.10f, 0.40f),
                new Vector3(0.03f, 2.20f, 0.03f),
                chromePolishedMat);
            CreatePrimitiveCylinder(kitchenGroup.transform, "Metro_Post_4",
                rackCorner + new Vector3(0.35f, 1.10f, 0.40f),
                new Vector3(0.03f, 2.20f, 0.03f),
                chromePolishedMat);

            // 4 Chrome Wire Shelves
            for (int sh = 0; sh < 4; sh++)
            {
                float sy = 0.35f + (sh * 0.55f);
                CreatePrimitiveCube(kitchenGroup.transform, $"Metro_Shelf_{sh + 1}",
                    rackCorner + new Vector3(0f, sy, 0f),
                    new Vector3(0.75f, 0.03f, 0.85f),
                    chromePolishedMat);
            }

            // Shelf 1 (Bottom): 50 lb Commercial Flour Sacks
            CreatePrimitiveCube(kitchenGroup.transform, "FlourSack_1",
                rackCorner + new Vector3(-0.12f, 0.50f, 0f),
                new Vector3(0.38f, 0.26f, 0.50f),
                awningWhiteMat);
            CreatePrimitiveCube(kitchenGroup.transform, "FlourSack_2",
                rackCorner + new Vector3(0.15f, 0.50f, 0f),
                new Vector3(0.35f, 0.26f, 0.45f),
                awningWhiteMat);

            // Shelf 2: Giant 5-Liter Italian Olive Oil Tins (Olive Green & Gold)
            CreatePrimitiveCube(kitchenGroup.transform, "OilTin_1",
                rackCorner + new Vector3(-0.16f, 1.05f, -0.15f),
                new Vector3(0.20f, 0.32f, 0.16f),
                plantGreenMat);
            CreatePrimitiveCube(kitchenGroup.transform, "OilTin_2",
                rackCorner + new Vector3(0.16f, 1.05f, -0.15f),
                new Vector3(0.20f, 0.32f, 0.16f),
                plantGreenMat);
            CreatePrimitiveCube(kitchenGroup.transform, "OilTin_3",
                rackCorner + new Vector3(0f, 1.05f, 0.18f),
                new Vector3(0.20f, 0.32f, 0.16f),
                brassGoldMat);

            // Shelf 3: Crates of San Marzano Whole Peeled Tomatoes
            CreatePrimitiveCube(kitchenGroup.transform, "CannedTomatoCrate",
                rackCorner + new Vector3(0f, 1.58f, 0f),
                new Vector3(0.55f, 0.18f, 0.65f),
                ketchupRedMat);

            // 7. Chef Wall Organization: Spices, Knife Strip, Hanging Pots
            // Multi-Tier Spice & Oil Shelf on Subway Tile
            CreatePrimitiveCube(kitchenGroup.transform, "ChefSpiceShelf",
                new Vector3(121.84f, 1.65f, -17.80f),
                new Vector3(0.12f, 0.04f, 1.90f),
                stainlessSteelMat);

            Vector3[] jarPositions = new Vector3[] {
                new Vector3(121.84f, 1.74f, -18.55f),
                new Vector3(121.84f, 1.74f, -18.25f),
                new Vector3(121.84f, 1.74f, -17.95f),
                new Vector3(121.84f, 1.74f, -17.65f),
                new Vector3(121.84f, 1.74f, -17.35f),
                new Vector3(121.84f, 1.74f, -17.05f),
            };
            for (int j = 0; j < jarPositions.Length; j++)
            {
                CreatePrimitiveCylinder(kitchenGroup.transform, $"SpiceJar_{j + 1}",
                    jarPositions[j],
                    new Vector3(0.08f, 0.08f, 0.08f),
                    (j % 2 == 0) ? brassGoldMat : awningWhiteMat);
            }

            // Magnetic Knife Strip on North Wall (X = 118.0, Z = -14.85)
            CreatePrimitiveCube(kitchenGroup.transform, "MagneticKnifeStrip",
                new Vector3(118.0f, 1.55f, -14.85f),
                new Vector3(1.60f, 0.12f, 0.05f),
                stainlessSteelMat);
            for (int k = -1; k <= 2; k++)
            {
                CreatePrimitiveCube(kitchenGroup.transform, $"ChefKnife_{k + 2}",
                    new Vector3(118.0f + (k * 0.32f), 1.50f, -14.82f),
                    new Vector3(0.04f, 0.32f, 0.02f),
                    stainlessSteelMat);
            }

            // Hanging Copper & Stainless Pot Rack above Sink (Z = -20.14)
            CreatePrimitiveCube(kitchenGroup.transform, "HangingPotRack_Bar",
                new Vector3(121.0f, 2.25f, -20.14f),
                new Vector3(0.06f, 0.06f, 1.80f),
                brassGoldMat);
            CreatePrimitiveCylinder(kitchenGroup.transform, "HangingPan_1",
                new Vector3(121.0f, 1.95f, -19.60f),
                new Vector3(0.26f, 0.04f, 0.26f),
                brassGoldMat);
            CreatePrimitiveCylinder(kitchenGroup.transform, "HangingPan_2",
                new Vector3(121.0f, 1.95f, -20.60f),
                new Vector3(0.28f, 0.05f, 0.28f),
                stainlessSteelMat);

            // 8. Commercial High-Bay Ceiling Troffer Lighting (4500K Clean Daylight Task Lights)
            Vector3[] trofferPositions = new Vector3[] {
                new Vector3(113.5f, 3.10f, -18.0f),
                new Vector3(118.5f, 3.10f, -18.0f),
                new Vector3(113.5f, 3.10f, -23.5f),
                new Vector3(118.5f, 3.10f, -23.5f),
            };

            for (int tr = 0; tr < trofferPositions.Length; tr++)
            {
                Vector3 trPos = trofferPositions[tr];
                GameObject trofferObj = new GameObject($"TrofferFixture_{tr + 1}");
                trofferObj.transform.SetParent(kitchenGroup.transform, false);

                // Troffer Outer Stainless Housing
                CreatePrimitiveCube(trofferObj.transform, "Housing",
                    trPos,
                    new Vector3(1.40f, 0.08f, 0.70f),
                    stainlessSteelMat);
                // Glowing Fluorescent / LED Diffuser Panel
                CreatePrimitiveCube(trofferObj.transform, "Diffuser",
                    trPos + new Vector3(0f, -0.04f, 0f),
                    new Vector3(1.30f, 0.02f, 0.60f),
                    awningWhiteMat);
                // Crisp 4500K Commercial Task Spotlight
                CreatePointLight(trofferObj.transform, "TaskLight",
                    trPos + new Vector3(0f, -0.25f, 0f),
                    new Color(0.96f, 0.98f, 1.0f),
                    2.8f, 5.0f);
            }
        }

        // ── 7. Atmosphere, Art, Jukebox & Neon Sign ───────────────────────────

        private void BuildAtmosphereArtAndJukebox(Transform parent)
        {
            GameObject decorGroup = new GameObject("07_AtmosphereAndArt");
            decorGroup.transform.SetParent(parent, false);

            // 1. Classic 1950s Illuminated Retro Jukebox in Dining Corner (X = 89.6, Z = -11.5)
            Vector3 jukePos = new Vector3(89.6f, 0f, -11.5f);
            GameObject jukeObj = new GameObject("RetroJukebox_1950s");
            jukeObj.transform.SetParent(decorGroup.transform, false);

            CreatePrimitiveCube(jukeObj.transform, "Jukebox_Cabinet",
                jukePos + new Vector3(0f, 0.90f, 0f),
                new Vector3(0.95f, 1.80f, 0.75f),
                wainscotingMat);
            CreatePrimitiveCylinder(jukeObj.transform, "Jukebox_ArchedTop",
                jukePos + new Vector3(0f, 1.80f, 0f),
                new Vector3(0.95f, 0.75f, 0.45f),
                boothBurgundyMat, Quaternion.Euler(0f, 0f, 90f));

            // Glowing Rainbow Neon Arches
            CreatePrimitiveCylinder(jukeObj.transform, "Neon_Arch_Outer",
                jukePos + new Vector3(0.02f, 1.80f, -0.32f),
                new Vector3(0.85f, 0.06f, 0.40f),
                jukeboxNeonMat, Quaternion.Euler(0f, 0f, 90f));
            CreatePrimitiveCylinder(jukeObj.transform, "Neon_Arch_Inner",
                jukePos + new Vector3(0.02f, 1.80f, -0.32f),
                new Vector3(0.65f, 0.06f, 0.30f),
                neonCyanMat, Quaternion.Euler(0f, 0f, 90f));

            // Speaker Grille & Amber Accent
            CreatePrimitiveCube(jukeObj.transform, "SpeakerGrille",
                jukePos + new Vector3(0f, 0.55f, -0.38f),
                new Vector3(0.75f, 0.70f, 0.04f),
                brassGoldMat);
            CreatePointLight(jukeObj.transform, "Jukebox_Glow",
                jukePos + new Vector3(0.35f, 1.10f, -0.50f),
                new Color(1.0f, 0.2f, 0.8f),
                2.5f, 3.5f);

            // 2. Large Daily Specials Chalkboard Menu (North-West Wall, X = 88.58, Z = -18.5)
            Vector3 chalkPos = new Vector3(88.58f, 2.30f, -18.50f);
            CreatePrimitiveCube(decorGroup.transform, "Chalkboard_Frame",
                chalkPos,
                new Vector3(0.06f, 1.50f, 2.60f),
                wainscotingMat);
            CreatePrimitiveCube(decorGroup.transform, "Chalkboard_Slate",
                chalkPos + new Vector3(0.02f, 0f, 0f),
                new Vector3(0.03f, 1.38f, 2.48f),
                chalkboardMat);

            // Clean World-Space TextMeshPro for Chalkboard
            CreateWallSignUI(decorGroup.transform,
                chalkPos + new Vector3(0.04f, 0.48f, 0f),
                Quaternion.Euler(0f, 90f, 0f),
                new Vector2(400f, 60f), 0.0055f, "DAILY SPECIALS", 30f, new Color(0.95f, 0.90f, 0.70f));
            CreateWallSignUI(decorGroup.transform,
                chalkPos + new Vector3(0.04f, 0.18f, 0f),
                Quaternion.Euler(0f, 90f, 0f),
                new Vector2(450f, 50f), 0.0045f, "Cheeseburger Deluxe Combo ... $30", 20f, Color.white);
            CreateWallSignUI(decorGroup.transform,
                chalkPos + new Vector3(0.04f, -0.12f, 0f),
                Quaternion.Euler(0f, 90f, 0f),
                new Vector2(450f, 50f), 0.0045f, "Crispy Golden French Fries ... $10", 20f, Color.white);
            CreateWallSignUI(decorGroup.transform,
                chalkPos + new Vector3(0.04f, -0.42f, 0f),
                Quaternion.Euler(0f, 90f, 0f),
                new Vector2(450f, 50f), 0.0045f, "Fountain Soda & Craft Shakes ... $5", 20f, new Color(0.7f, 0.9f, 1.0f));

            // 3. Radiant Glowing Neon Wall Sign ("DISH-ASTER DINER") on Brick Wall
            Vector3 neonPos = new Vector3(88.58f, 2.65f, -26.00f);
            CreatePrimitiveCube(decorGroup.transform, "NeonSign_Backplate",
                neonPos,
                new Vector3(0.05f, 0.85f, 3.40f),
                checkerboardDarkMat);
            CreatePrimitiveCube(decorGroup.transform, "NeonSign_Border",
                neonPos + new Vector3(0.02f, 0f, 0f),
                new Vector3(0.04f, 0.78f, 3.32f),
                neonCyanMat);

            CreateWallSignUI(decorGroup.transform,
                neonPos + new Vector3(0.04f, 0.12f, 0f),
                Quaternion.Euler(0f, 90f, 0f),
                new Vector2(500f, 70f), 0.0055f, "DISH-ASTER DINER", 34f, new Color(0.10f, 0.88f, 1.0f));
            CreateWallSignUI(decorGroup.transform,
                neonPos + new Vector3(0.04f, -0.18f, 0f),
                Quaternion.Euler(0f, 90f, 0f),
                new Vector2(500f, 50f), 0.0040f, "-- BEST BURGERS & SHAKES IN TOWN --", 18f, new Color(1.0f, 0.75f, 0.2f));

            CreatePointLight(decorGroup.transform, "NeonSign_Glow",
                neonPos + new Vector3(0.35f, 0f, 0f),
                new Color(0.10f, 0.85f, 1.0f),
                2.8f, 4.5f);

            // 4. Overhead Industrial Ceiling Beams & AC Ductwork
            float[] beamZ = new float[] { -11.0f, -18.5f, -26.0f, -33.5f };
            for (int b = 0; b < beamZ.Length; b++)
            {
                CreatePrimitiveCube(decorGroup.transform, $"CeilingBeam_{b + 1}",
                    new Vector3(105.5f, 3.55f, beamZ[b]),
                    new Vector3(34.0f, 0.35f, 0.35f),
                    ceilingBeamMat);
            }

            // Suspended Spiral Ventilation Ducting (from CityLife library)
            InstantiateACVent(decorGroup.transform, "ACVent_Dining_1", new Vector3(96.74f, 3.30f, -18.5f));
            InstantiateACVent(decorGroup.transform, "ACVent_Dining_2", new Vector3(96.74f, 3.30f, -26.0f));

            // Suspended Cozy Brass Bell Pendant Lamps over Dining Tables
            Vector3[] pendantTargets = new Vector3[] {
                new Vector3(100.74f, 0f, -14.84f),
                new Vector3(92.74f, 0f, -14.84f),
                new Vector3(100.74f, 0f, -22.34f),
                new Vector3(92.74f, 0f, -22.34f),
                new Vector3(100.74f, 0f, -29.84f),
                new Vector3(92.74f, 0f, -29.84f)
            };
            for (int p = 0; p < pendantTargets.Length; p++)
            {
                Vector3 tPos = pendantTargets[p];
                GameObject pendant = new GameObject($"Pendant_Table_{p + 1}");
                pendant.transform.SetParent(decorGroup.transform, false);

                // Brass suspension cord
                CreatePrimitiveCylinder(pendant.transform, "Cord",
                    new Vector3(tPos.x, 2.90f, tPos.z),
                    new Vector3(0.02f, 0.65f, 0.02f),
                    windowFrameMat);

                // Brass bell lamp shade
                CreatePrimitiveCylinder(pendant.transform, "BellShade",
                    new Vector3(tPos.x, 2.25f, tPos.z),
                    new Vector3(0.48f, 0.18f, 0.48f),
                    brassGoldMat);

                // Warm ambient downlight illumination
                CreatePointLight(pendant.transform, "AmberLight",
                    new Vector3(tPos.x, 2.05f, tPos.z),
                    new Color(1.0f, 0.88f, 0.65f),
                    2.2f, 3.8f);
            }
        }

        // ── 8. Exterior Takeout Bay & Delivery Window ─────────────────────────

        private void BuildTakeoutAndCourierBay(Transform parent)
        {
            GameObject takeoutGroup = new GameObject("08_TakeoutCourierBay");
            takeoutGroup.transform.SetParent(parent, false);

            Vector3 canopyPos = new Vector3(110.64f, 2.70f, -23.00f);

            // Striped Canvas Awning Structure (Alternating Red and White Slats)
            for (int s = 0; s < 7; s++)
            {
                float zOffset = -0.90f + (s * 0.30f);
                Material sMat = (s % 2 == 0) ? awningRedMat : awningWhiteMat;
                CreatePrimitiveCube(takeoutGroup.transform, $"AwningSlat_{s + 1}",
                    canopyPos + new Vector3(-0.45f, 0f, zOffset),
                    new Vector3(0.90f, 0.08f, 0.28f),
                    sMat);
            }

            // Awning Front Scalloped Trim
            CreatePrimitiveCube(takeoutGroup.transform, "AwningValance",
                canopyPos + new Vector3(-0.90f, -0.15f, 0f),
                new Vector3(0.06f, 0.30f, 2.20f),
                awningRedMat);

            // Neon Sign: "TAKE OUT & DELIVERY"
            Vector3 signPos = canopyPos + new Vector3(0f, 0.28f, 0f);
            CreatePrimitiveCube(takeoutGroup.transform, "TakeoutSign_Plate",
                signPos,
                new Vector3(0.12f, 0.45f, 2.80f),
                checkerboardDarkMat);
            CreatePrimitiveCube(takeoutGroup.transform, "TakeoutSign_NeonRim",
                signPos + new Vector3(-0.07f, 0f, 0f),
                new Vector3(0.04f, 0.40f, 2.70f),
                neonAmberMat);

            CreateWallSignUI(takeoutGroup.transform,
                signPos + new Vector3(-0.10f, 0f, 0f),
                Quaternion.Euler(0f, 270f, 0f),
                new Vector2(400f, 70f), 0.0055f, "TAKE OUT & COURIER", 26f, new Color(1.0f, 0.65f, 0.15f));

            // Exterior Delivery Scooter Parking Bay Safety Striping
            CreatePrimitiveCube(takeoutGroup.transform, "ScooterBay_HatchedBorder",
                new Vector3(110.64f, 0.018f, -23.0f),
                new Vector3(1.60f, 0.025f, 2.80f),
                neonAmberMat);
        }

        // ── Clean Geometric, Plant & UI Helpers ───────────────────────────────

        private GameObject CreatePrimitiveCube(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, Quaternion? rot = null)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            if (rot.HasValue) go.transform.rotation = rot.Value;

            Collider col = go.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null && mat != null) mr.sharedMaterial = mat;

            return go;
        }

        private GameObject CreatePrimitiveCylinder(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, Quaternion? rot = null)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            if (rot.HasValue) go.transform.rotation = rot.Value;

            Collider col = go.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null && mat != null) mr.sharedMaterial = mat;

            return go;
        }

        private void CreatePointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
        {
            GameObject lightGo = new GameObject(name);
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.position = pos;

            Light l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
        }

        private void InstantiatePlant(Transform parent, string name, Vector3 pos, string plantFbxName)
        {
            #if UNITY_EDITOR
            string path = $"Assets/CityLife/CityElements/Models/{plantFbxName}.fbx";
            GameObject plantAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (plantAsset != null)
            {
                GameObject plantInstance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(plantAsset);
                plantInstance.name = name;
                plantInstance.transform.SetParent(parent, false);
                plantInstance.transform.position = pos;
                plantInstance.transform.localScale = Vector3.one * 1.15f;

                foreach (var col in plantInstance.GetComponentsInChildren<Collider>(true))
                {
                    DestroyImmediate(col);
                }
                return;
            }
            #endif

            // Fallback decorative planter if FBX cannot be loaded in standalone build
            GameObject fallback = CreatePrimitiveCylinder(parent, name, pos + new Vector3(0f, 0.45f, 0f), new Vector3(0.55f, 0.45f, 0.55f), wainscotingMat);
            CreatePrimitiveCube(fallback.transform, "Leaves", pos + new Vector3(0f, 1.10f, 0f), new Vector3(0.85f, 0.70f, 0.85f), plantGreenMat);
        }

        private void InstantiateACVent(Transform parent, string name, Vector3 pos)
        {
            #if UNITY_EDITOR
            string path = "Assets/CityLife/CityElements/Models/Prop_ACVent_Stright.fbx";
            GameObject ventAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (ventAsset != null)
            {
                GameObject ventInstance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(ventAsset);
                ventInstance.name = name;
                ventInstance.transform.SetParent(parent, false);
                ventInstance.transform.position = pos;
                ventInstance.transform.localScale = new Vector3(1.4f, 1.2f, 1.4f);
                ventInstance.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                foreach (var col in ventInstance.GetComponentsInChildren<Collider>(true))
                {
                    DestroyImmediate(col);
                }
                return;
            }
            #endif
            CreatePrimitiveCylinder(parent, name, pos, new Vector3(0.40f, 2.50f, 0.40f), stainlessSteelMat, Quaternion.Euler(90f, 0f, 0f));
        }

        private GameObject CreateWallSignUI(
            Transform parent,
            Vector3 pos,
            Quaternion rot,
            Vector2 sizeDelta,
            float scaleFactor,
            string text,
            float fontSize,
            Color textColor)
        {
            GameObject canvasGo = new GameObject("WallSignUI");
            canvasGo.transform.SetParent(parent, false);
            canvasGo.transform.position = pos;
            canvasGo.transform.rotation = rot;
            canvasGo.transform.localScale = Vector3.one * scaleFactor;

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = sizeDelta;

            GameObject textObj = new GameObject("Text_Label");
            textObj.transform.SetParent(canvasGo.transform, false);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.rectTransform.sizeDelta = sizeDelta;
            tmp.rectTransform.anchoredPosition = Vector2.zero;

            #if UNITY_EDITOR
            var font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font != null) tmp.font = font;
            #endif

            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor;
            tmp.raycastTarget = false;

            return canvasGo;
        }
    }
}
