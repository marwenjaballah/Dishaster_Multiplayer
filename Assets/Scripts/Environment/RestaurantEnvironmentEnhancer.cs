using System;
using UnityEngine;
using TMPro;

namespace Environment
{
    /// <summary>
    /// Master Architectural & Visual Enhancer for the Restaurant interior & exterior.
    /// Transforms the plain prototype kitchen and dining area into a commercial-grade,
    /// vibrant diner experience without modifying or altering the surrounding city.
    /// Provides:
    /// 1. Zoned flooring: Warm hardwood diner parquet & heavy-duty non-slip kitchen tile.
    /// 2. Architectural wainscoting & glossy kitchen subway tile splashbacks.
    /// 3. Overhead stainless steel commercial kitchen exhaust hood with downlights.
    /// 4. Cozy suspended diner pendant lamps over all 6 dining tables.
    /// 5. Tabletop condiment sets (ketchup, mustard, salt/pepper) & diegetic table numbers.
    /// 6. Pass-through order ticket rail & holding heat-lamp bar.
    /// 7. Exterior takeout window awning canopy & neon courier signage.
    /// 8. Dining room wall menu chalkboards, planter dividers, and neon diner sign.
    /// 9. Kitchen magnetic knife racks, hanging pots, and wall shift clock.
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

        [Header("Material References (Auto-loaded if null)")]
        [SerializeField] private Material hardwoodFloorMat;
        [SerializeField] private Material kitchenTileMat;
        [SerializeField] private Material subwayTileMat;
        [SerializeField] private Material wainscotingMat;
        [SerializeField] private Material wallAccentMat;
        [SerializeField] private Material stainlessSteelMat;
        [SerializeField] private Material brassGoldMat;
        [SerializeField] private Material neonCyanMat;
        [SerializeField] private Material neonAmberMat;
        [SerializeField] private Material awningRedMat;
        [SerializeField] private Material awningWhiteMat;
        [SerializeField] private Material ketchupRedMat;
        [SerializeField] private Material mustardYellowMat;
        [SerializeField] private Material plantGreenMat;

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
            // Verify enhancements exist in scene
            GameObject existing = GameObject.Find(ROOT_CONTAINER_NAME);
            if (existing == null)
            {
                BakeRestaurantEnvironment();
            }
        }

        public void LoadMaterialsIfMissing()
        {
            if (hardwoodFloorMat == null) hardwoodFloorMat = Resources.Load<Material>("Restaurant_HardwoodFloor") ?? LoadAssetMat("Restaurant_HardwoodFloor");
            if (kitchenTileMat == null) kitchenTileMat = LoadAssetMat("Restaurant_KitchenTile");
            if (subwayTileMat == null) subwayTileMat = LoadAssetMat("Restaurant_SubwayTile");
            if (wainscotingMat == null) wainscotingMat = LoadAssetMat("Restaurant_Wainscoting");
            if (wallAccentMat == null) wallAccentMat = LoadAssetMat("Restaurant_WallAccent");
            if (stainlessSteelMat == null) stainlessSteelMat = LoadAssetMat("Restaurant_StainlessSteel");
            if (brassGoldMat == null) brassGoldMat = LoadAssetMat("Restaurant_BrassGold");
            if (neonCyanMat == null) neonCyanMat = LoadAssetMat("Restaurant_NeonCyan");
            if (neonAmberMat == null) neonAmberMat = LoadAssetMat("Restaurant_NeonAmber");
            if (awningRedMat == null) awningRedMat = LoadAssetMat("Restaurant_AwningRed");
            if (awningWhiteMat == null) awningWhiteMat = LoadAssetMat("Restaurant_AwningWhite");
            if (ketchupRedMat == null) ketchupRedMat = LoadAssetMat("Restaurant_KetchupRed");
            if (mustardYellowMat == null) mustardYellowMat = LoadAssetMat("Restaurant_MustardYellow");
            if (plantGreenMat == null) plantGreenMat = LoadAssetMat("Restaurant_PlantGreen");
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

            // Clean up existing enhancement container
            Transform oldContainer = restObj.transform.Find(ROOT_CONTAINER_NAME);
            if (oldContainer != null)
            {
                if (Application.isPlaying) Destroy(oldContainer.gameObject);
                else DestroyImmediate(oldContainer.gameObject);
            }

            GameObject root = new GameObject(ROOT_CONTAINER_NAME);
            root.transform.SetParent(restObj.transform, false);

            // 1. Zoned Flooring
            BuildZonedFlooring(root.transform);

            // 2. Wainscoting & Splashbacks
            BuildWallsAndWainscoting(root.transform);

            // 3. Commercial Exhaust Hood & Kitchen Track Lighting
            BuildKitchenExhaustHood(root.transform);

            // 4. Cozy Suspended Dining Table Pendant Lights
            BuildDiningPendantLights(root.transform);

            // 5. Tabletop Condiments & Table Number Signs
            BuildTabletopCondiments(root.transform);

            // 6. Pass-Through Window Overhead Valance & Heat Lamp
            BuildPassThroughValance(root.transform);

            // 7. Exterior Takeout Window Canopy & Neon Sign
            BuildTakeoutExteriorCanopy(root.transform);

            // 8. Dining Room Decor, Planters & Neon Wall Sign
            BuildDiningDecorAndPlanters(root.transform);

            // 9. Kitchen Organization (Magnetic Knife Strip & Pot Rack)
            BuildKitchenUtensilsAndClock(root.transform);

            Debug.Log("[RestaurantEnvironmentEnhancer] ✨ Restaurant environment design successfully enhanced!");
        }

        // ── 1. Zoned Flooring ─────────────────────────────────────────────────

        private void BuildZonedFlooring(Transform parent)
        {
            GameObject floorGroup = new GameObject("01_ZonedFloors");
            floorGroup.transform.SetParent(parent, false);

            // Dining Room Hardwood Parquet: X ~ 88.5 to 110.0, Z ~ -5.2 to -36.4
            CreatePrimitiveQuad(floorGroup.transform, "Floor_DiningHardwood",
                new Vector3(99.25f, 0.012f, -20.8f),
                new Vector3(21.5f, 0.02f, 31.2f),
                hardwoodFloorMat);

            // Kitchen Heavy-Duty Non-Slip Tile: X ~ 110.0 to 122.5, Z ~ -14.0 to -27.5
            CreatePrimitiveQuad(floorGroup.transform, "Floor_KitchenTile",
                new Vector3(116.25f, 0.015f, -20.89f),
                new Vector3(12.5f, 0.02f, 13.5f),
                kitchenTileMat);

            // Brass Threshold Transition Strip between Dining and Kitchen
            CreatePrimitiveQuad(floorGroup.transform, "Floor_BrassThreshold",
                new Vector3(110.0f, 0.022f, -20.89f),
                new Vector3(0.18f, 0.03f, 13.5f),
                brassGoldMat);

            // Courier Parking Bay Threshold Accent
            CreatePrimitiveQuad(floorGroup.transform, "Floor_CourierBayBorder",
                new Vector3(110.64f, 0.018f, -23.0f),
                new Vector3(1.4f, 0.025f, 2.6f),
                neonAmberMat);
        }

        // ── 2. Walls & Wainscoting ─────────────────────────────────────────────

        private void BuildWallsAndWainscoting(Transform parent)
        {
            GameObject wallGroup = new GameObject("02_ArchitecturalWalls");
            wallGroup.transform.SetParent(parent, false);

            // Dining Room West Wall Wainscoting (Lower 1.1m dark wood)
            CreatePrimitiveQuad(wallGroup.transform, "Wainscot_WestWall",
                new Vector3(88.55f, 0.55f, -20.8f),
                new Vector3(0.12f, 1.1f, 31.2f),
                wainscotingMat);

            // Chair Rail Molding Strip
            CreatePrimitiveQuad(wallGroup.transform, "Molding_WestWall",
                new Vector3(88.58f, 1.12f, -20.8f),
                new Vector3(0.16f, 0.08f, 31.2f),
                brassGoldMat);

            // Dining Room South Wall Wainscoting
            CreatePrimitiveQuad(wallGroup.transform, "Wainscot_SouthWall",
                new Vector3(99.25f, 0.55f, -36.35f),
                new Vector3(21.5f, 1.1f, 0.12f),
                wainscotingMat);

            // Dining Room North Wall Wainscoting
            CreatePrimitiveQuad(wallGroup.transform, "Wainscot_NorthWall",
                new Vector3(99.25f, 0.55f, -5.25f),
                new Vector3(21.5f, 1.1f, 0.12f),
                wainscotingMat);

            // Kitchen Subway Tile Splashback (East Wall behind Stoves, Deep Fryer, Sink)
            CreatePrimitiveQuad(wallGroup.transform, "Splashback_KitchenSubwayTile",
                new Vector3(121.32f, 1.4f, -20.89f),
                new Vector3(0.08f, 2.8f, 13.5f),
                subwayTileMat);
        }

        // ── 3. Commercial Exhaust Hood ─────────────────────────────────────────

        private void BuildKitchenExhaustHood(Transform parent)
        {
            GameObject hoodGroup = new GameObject("03_KitchenExhaustHood");
            hoodGroup.transform.SetParent(parent, false);

            // Stainless Steel Canopy above cook line (X: 121.14, Z: -16.5 to -21.0)
            Vector3 hoodCenter = new Vector3(121.0f, 2.45f, -18.75f);
            CreatePrimitiveQuad(hoodGroup.transform, "Canopy_Body",
                hoodCenter,
                new Vector3(1.3f, 0.55f, 4.6f),
                stainlessSteelMat);

            // Canopy Beveled Lip
            CreatePrimitiveQuad(hoodGroup.transform, "Canopy_Lip",
                hoodCenter + new Vector3(-0.65f, -0.25f, 0f),
                new Vector3(0.08f, 0.08f, 4.6f),
                stainlessSteelMat);

            // Dual Vertical Exhaust Ducts rising to ceiling
            CreatePrimitiveQuad(hoodGroup.transform, "VentDuct_1",
                new Vector3(121.0f, 3.1f, -17.5f),
                new Vector3(0.45f, 0.8f, 0.45f),
                stainlessSteelMat);

            CreatePrimitiveQuad(hoodGroup.transform, "VentDuct_2",
                new Vector3(121.0f, 3.1f, -20.0f),
                new Vector3(0.45f, 0.8f, 0.45f),
                stainlessSteelMat);

            // Halogen Downlights pointing at stoves
            CreatePointLight(hoodGroup.transform, "HoodLight_Stoves",
                new Vector3(120.9f, 2.15f, -17.8f),
                new Color(1.0f, 0.92f, 0.75f),
                2.5f, 3.5f);

            CreatePointLight(hoodGroup.transform, "HoodLight_Fryer",
                new Vector3(120.9f, 2.15f, -19.8f),
                new Color(1.0f, 0.92f, 0.75f),
                2.5f, 3.5f);
        }

        // ── 4. Suspended Dining Pendant Lights ─────────────────────────────────

        private void BuildDiningPendantLights(Transform parent)
        {
            GameObject lightsGroup = new GameObject("04_DiningPendantLights");
            lightsGroup.transform.SetParent(parent, false);

            Vector3[] tablePositions = new Vector3[]
            {
                new Vector3(100.74f, 0.0f, -14.84f), // Table 1
                new Vector3(92.74f, 0.0f, -14.84f),  // Table 2
                new Vector3(100.74f, 0.0f, -22.34f), // Table 3
                new Vector3(92.74f, 0.0f, -22.34f),  // Table 4
                new Vector3(100.74f, 0.0f, -29.84f), // Table 5
                new Vector3(92.74f, 0.0f, -29.84f)   // Table 6
            };

            for (int i = 0; i < tablePositions.Length; i++)
            {
                Vector3 tablePos = tablePositions[i];
                GameObject pendant = new GameObject($"Pendant_Table_{i + 1}");
                pendant.transform.SetParent(lightsGroup.transform, false);

                // Suspension Wire
                CreatePrimitiveQuad(pendant.transform, "Cord",
                    new Vector3(tablePos.x, 2.85f, tablePos.z),
                    new Vector3(0.03f, 1.2f, 0.03f),
                    wainscotingMat);

                // Brass Bell Shade
                CreatePrimitiveQuad(pendant.transform, "BrassShade",
                    new Vector3(tablePos.x, 2.22f, tablePos.z),
                    new Vector3(0.55f, 0.22f, 0.55f),
                    brassGoldMat);

                // Warm Amber Table Glow
                CreatePointLight(pendant.transform, $"Light_Table_{i + 1}",
                    new Vector3(tablePos.x, 2.05f, tablePos.z),
                    new Color(1.0f, 0.82f, 0.55f),
                    3.0f, 4.2f);
            }
        }

        // ── 5. Tabletop Condiments & Table Numbers ──────────────────────────────

        private void BuildTabletopCondiments(Transform parent)
        {
            GameObject condimentGroup = new GameObject("05_TabletopCondiments");
            condimentGroup.transform.SetParent(parent, false);

            Vector3[] tablePositions = new Vector3[]
            {
                new Vector3(100.74f, 0.51f, -14.84f),
                new Vector3(92.74f, 0.51f, -14.84f),
                new Vector3(100.74f, 0.51f, -22.34f),
                new Vector3(92.74f, 0.51f, -22.34f),
                new Vector3(100.74f, 0.51f, -29.84f),
                new Vector3(92.74f, 0.51f, -29.84f)
            };

            for (int i = 0; i < tablePositions.Length; i++)
            {
                Vector3 basePos = tablePositions[i] + new Vector3(0f, 0f, 0.38f); // Shifted slightly north to avoid dish collision
                GameObject set = new GameObject($"CondimentSet_Table_{i + 1}");
                set.transform.SetParent(condimentGroup.transform, false);

                // Condiment Caddy Tray
                CreatePrimitiveQuad(set.transform, "CaddyTray",
                    basePos + new Vector3(0f, 0.015f, 0f),
                    new Vector3(0.28f, 0.03f, 0.22f),
                    stainlessSteelMat);

                // Ketchup Bottle
                CreatePrimitiveQuad(set.transform, "Bottle_Ketchup",
                    basePos + new Vector3(-0.07f, 0.09f, 0f),
                    new Vector3(0.06f, 0.15f, 0.06f),
                    ketchupRedMat);

                // Mustard Bottle
                CreatePrimitiveQuad(set.transform, "Bottle_Mustard",
                    basePos + new Vector3(0.07f, 0.09f, 0f),
                    new Vector3(0.06f, 0.15f, 0.06f),
                    mustardYellowMat);

                // Salt Shaker
                CreatePrimitiveQuad(set.transform, "Shaker_Salt",
                    basePos + new Vector3(-0.07f, 0.05f, -0.06f),
                    new Vector3(0.04f, 0.09f, 0.04f),
                    wallAccentMat);

                // Pepper Shaker
                CreatePrimitiveQuad(set.transform, "Shaker_Pepper",
                    basePos + new Vector3(0.07f, 0.05f, -0.06f),
                    new Vector3(0.04f, 0.09f, 0.04f),
                    wainscotingMat);

                // 3D Table Number Tent Stand
                GameObject tentStand = CreatePrimitiveQuad(set.transform, "TableNumberTent",
                    basePos + new Vector3(0f, 0.06f, 0.06f),
                    new Vector3(0.12f, 0.08f, 0.08f),
                    brassGoldMat);

                // Add 3D text label
                Add3DText(tentStand.transform, $"#{i + 1}", new Vector3(0f, 0.01f, -0.045f), 14, Color.black);
            }
        }

        // ── 6. Pass-Through Window Overhead Valance ───────────────────────────

        private void BuildPassThroughValance(Transform parent)
        {
            GameObject passGroup = new GameObject("06_PassThroughValance");
            passGroup.transform.SetParent(parent, false);

            Vector3 passCenter = new Vector3(110.64f, 1.85f, -20.69f);

            // Overhead Pass Valance Canopy
            CreatePrimitiveQuad(passGroup.transform, "Valance_Header",
                passCenter,
                new Vector3(0.45f, 0.15f, 3.5f),
                wainscotingMat);

            // Stainless Steel Order Ticket Rail
            CreatePrimitiveQuad(passGroup.transform, "TicketRail",
                passCenter + new Vector3(-0.18f, -0.08f, 0f),
                new Vector3(0.06f, 0.05f, 3.2f),
                stainlessSteelMat);

            // Simulated hanging paper tickets
            for (int t = -2; t <= 2; t++)
            {
                CreatePrimitiveQuad(passGroup.transform, $"OrderTicket_{t + 3}",
                    passCenter + new Vector3(-0.21f, -0.16f, t * 0.55f),
                    new Vector3(0.01f, 0.16f, 0.18f),
                    wallAccentMat);
            }

            // Emissive Heat-Lamp Bar
            CreatePrimitiveQuad(passGroup.transform, "HeatLamp_Housing",
                passCenter + new Vector3(0f, -0.06f, 0f),
                new Vector3(0.18f, 0.06f, 2.8f),
                stainlessSteelMat);

            CreatePrimitiveQuad(passGroup.transform, "HeatLamp_Tube",
                passCenter + new Vector3(0f, -0.10f, 0f),
                new Vector3(0.08f, 0.04f, 2.6f),
                neonAmberMat);

            // Amber Heat Lamp Glow
            CreatePointLight(passGroup.transform, "HeatLamp_Glow",
                passCenter + new Vector3(0f, -0.25f, 0f),
                new Color(1.0f, 0.65f, 0.20f),
                2.8f, 3.2f);
        }

        // ── 7. Exterior Takeout Canopy & Neon Sign ─────────────────────────────

        private void BuildTakeoutExteriorCanopy(Transform parent)
        {
            GameObject extGroup = new GameObject("07_TakeoutExteriorCanopy");
            extGroup.transform.SetParent(parent, false);

            Vector3 takeoutCenter = new Vector3(110.64f, 2.55f, -23.0f);

            // Striped Canvas Awning Structure
            GameObject awning = new GameObject("Awning_Canopy");
            awning.transform.SetParent(extGroup.transform, false);
            awning.transform.position = takeoutCenter;
            awning.transform.rotation = Quaternion.Euler(20f, 90f, 0f);

            // Alternating Red & White Slats
            for (int s = -4; s <= 4; s++)
            {
                Material m = (s % 2 == 0) ? awningRedMat : awningWhiteMat;
                CreatePrimitiveQuad(awning.transform, $"AwningSlat_{s + 5}",
                    new Vector3(s * 0.28f, 0f, 0.65f),
                    new Vector3(0.28f, 0.05f, 1.4f),
                    m);
            }

            // Neon Signboard above Takeout Counter
            GameObject signBoard = CreatePrimitiveQuad(extGroup.transform, "Sign_TakeoutHeader",
                new Vector3(110.64f, 2.95f, -23.0f),
                new Vector3(0.12f, 0.45f, 2.8f),
                wainscotingMat);

            // Glowing Neon Sign Board
            CreatePrimitiveQuad(extGroup.transform, "Sign_NeonBorder",
                new Vector3(110.57f, 2.95f, -23.0f),
                new Vector3(0.04f, 0.40f, 2.6f),
                neonCyanMat);

            Add3DText(signBoard.transform, "TAKE OUT & DELIVERY", new Vector3(-0.08f, 0f, 0f), 22, Color.cyan);
        }

        // ── 8. Dining Room Decor, Planters & Neon Wall Sign ────────────────────

        private void BuildDiningDecorAndPlanters(Transform parent)
        {
            GameObject decorGroup = new GameObject("08_DiningDecorAndPlanters");
            decorGroup.transform.SetParent(parent, false);

            // Low Planter Divider Box between Row 1 & Row 2
            BuildPlanterDivider(decorGroup.transform, "PlanterDivider_1", new Vector3(96.74f, 0.45f, -18.5f));
            BuildPlanterDivider(decorGroup.transform, "PlanterDivider_2", new Vector3(96.74f, 0.45f, -26.0f));

            // Corner Indoor Palm Planters
            BuildCornerPlanter(decorGroup.transform, "CornerPlant_NW", new Vector3(89.5f, 0f, -6.5f));
            BuildCornerPlanter(decorGroup.transform, "CornerPlant_SW", new Vector3(89.5f, 0f, -34.8f));

            // Large Framed Chalkboard Menu on West Wall (pos = 88.58, Z = -20.8)
            GameObject chalkboard = CreatePrimitiveQuad(decorGroup.transform, "ChalkboardMenu_Frame",
                new Vector3(88.62f, 2.1f, -20.8f),
                new Vector3(0.08f, 1.4f, 2.6f),
                wainscotingMat);

            CreatePrimitiveQuad(decorGroup.transform, "ChalkboardMenu_Blackboard",
                new Vector3(88.66f, 2.1f, -20.8f),
                new Vector3(0.04f, 1.25f, 2.4f),
                kitchenTileMat);

            Add3DText(chalkboard.transform, "TODAY'S SPECIALS\n• DELUXE COMBO MEAL\n• CRISPY FRENCH FRIES\n• ICE COLD SODA",
                new Vector3(0.06f, 0f, 0f), 15, new Color(0.95f, 0.95f, 0.85f));

            // Main Emissive Neon Sign: "DISH-ASTER DINER" on South Wall
            GameObject neonSign = CreatePrimitiveQuad(decorGroup.transform, "NeonSign_Backdrop",
                new Vector3(99.25f, 2.45f, -36.25f),
                new Vector3(4.5f, 0.85f, 0.08f),
                wainscotingMat);

            CreatePrimitiveQuad(decorGroup.transform, "NeonSign_GlowBorder",
                new Vector3(99.25f, 2.45f, -36.18f),
                new Vector3(4.3f, 0.72f, 0.04f),
                neonAmberMat);

            Add3DText(neonSign.transform, "DISH-ASTER DINER", new Vector3(0f, 0f, 0.08f), 32, new Color(1f, 0.75f, 0.2f));

            CreatePointLight(decorGroup.transform, "NeonSign_AmbientGlow",
                new Vector3(99.25f, 2.3f, -35.8f),
                new Color(1f, 0.65f, 0.15f),
                2.2f, 4.5f);
        }

        private void BuildPlanterDivider(Transform parent, string name, Vector3 pos)
        {
            GameObject planter = new GameObject(name);
            planter.transform.SetParent(parent, false);

            // Wood box
            CreatePrimitiveQuad(planter.transform, "Box",
                pos,
                new Vector3(4.2f, 0.85f, 0.55f),
                wainscotingMat);

            // Soil
            CreatePrimitiveQuad(planter.transform, "Soil",
                pos + new Vector3(0f, 0.42f, 0f),
                new Vector3(4.0f, 0.05f, 0.45f),
                wainscotingMat);

            // Shrub Foliage
            CreatePrimitiveQuad(planter.transform, "Foliage",
                pos + new Vector3(0f, 0.62f, 0f),
                new Vector3(3.8f, 0.40f, 0.38f),
                plantGreenMat);
        }

        private void BuildCornerPlanter(Transform parent, string name, Vector3 pos)
        {
            GameObject plant = new GameObject(name);
            plant.transform.SetParent(parent, false);

            // Ceramic Pot
            CreatePrimitiveQuad(plant.transform, "Pot",
                pos + new Vector3(0f, 0.45f, 0f),
                new Vector3(0.65f, 0.9f, 0.65f),
                wallAccentMat);

            // Plant Foliage
            CreatePrimitiveQuad(plant.transform, "Leaves_1",
                pos + new Vector3(0f, 1.15f, 0f),
                new Vector3(0.9f, 0.7f, 0.9f),
                plantGreenMat);

            CreatePrimitiveQuad(plant.transform, "Leaves_2",
                pos + new Vector3(0f, 1.55f, 0f),
                new Vector3(0.6f, 0.5f, 0.6f),
                plantGreenMat);
        }

        // ── 9. Kitchen Organization & Utensils ─────────────────────────────────

        private void BuildKitchenUtensilsAndClock(Transform parent)
        {
            GameObject kitchenDetails = new GameObject("09_KitchenOrganization");
            kitchenDetails.transform.SetParent(parent, false);

            // Magnetic Knife Strip on North Wall (X: 118.0, Z: -15.55, Y: 1.55)
            GameObject knifeStrip = CreatePrimitiveQuad(kitchenDetails.transform, "MagneticKnifeStrip",
                new Vector3(118.0f, 1.55f, -15.58f),
                new Vector3(1.6f, 0.12f, 0.05f),
                stainlessSteelMat);

            // 4 Mini culinary knives attached
            for (int k = -1; k <= 2; k++)
            {
                CreatePrimitiveQuad(kitchenDetails.transform, $"Knife_{k + 2}",
                    new Vector3(118.0f + (k * 0.32f), 1.50f, -15.52f),
                    new Vector3(0.04f, 0.32f, 0.02f),
                    stainlessSteelMat);
            }

            // Hanging Pot Rack above Sink (X: 121.14, Z: -20.14, Y: 2.2)
            CreatePrimitiveQuad(kitchenDetails.transform, "HangingPotRack_Bar",
                new Vector3(121.0f, 2.2f, -20.14f),
                new Vector3(0.06f, 0.06f, 1.6f),
                brassGoldMat);

            // Wall Shift Clock above Pass-Through Window
            GameObject clock = CreatePrimitiveQuad(kitchenDetails.transform, "KitchenShiftClock",
                new Vector3(110.64f, 2.35f, -19.0f),
                new Vector3(0.08f, 0.55f, 0.55f),
                wallAccentMat);

            CreatePrimitiveQuad(clock.transform, "ClockRim",
                new Vector3(0.05f, 0f, 0f),
                new Vector3(0.03f, 0.52f, 0.52f),
                brassGoldMat);

            Add3DText(clock.transform, "12\n9   3\n6", new Vector3(0.07f, 0f, 0f), 16, Color.black);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private GameObject CreatePrimitiveQuad(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;

            // Remove or set collider to trigger so it never blocks pathfinding or interaction
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                DestroyImmediate(col);
            }

            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null && mat != null)
            {
                mr.sharedMaterial = mat;
            }

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

        private void Add3DText(Transform parent, string text, Vector3 localPos, float fontSize, Color color)
        {
            GameObject textGo = new GameObject("Label_3DText");
            textGo.transform.SetParent(parent, false);
            textGo.transform.localPosition = localPos;
            textGo.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            var tmp = textGo.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.rectTransform.sizeDelta = new Vector2(5f, 2f);
        }
    }
}
