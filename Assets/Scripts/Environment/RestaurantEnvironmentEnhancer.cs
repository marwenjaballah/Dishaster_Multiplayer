using System;
using UnityEngine;
using UnityEngine.UI;
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
    /// 5. Tabletop condiment sets (ketchup, mustard, salt/pepper) & diegetic table numbers ON the table surface.
    /// 6. Pass-through order ticket rail & holding heat-lamp bar.
    /// 7. Exterior takeout window canopy & neon courier signage.
    /// 8. Dining room wall menu chalkboard, planter dividers, and neon diner sign with clean World-Space Canvas scaling.
    /// 9. Kitchen magnetic knife racks, hanging pots, and analog wall clock.
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

            // Hide old generic single-plane floor
            Transform oldFloor = restObj.transform.Find("walls/Floor (1)");
            if (oldFloor != null)
            {
                MeshRenderer mr = oldFloor.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
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

            // 5. Tabletop Condiments & Table Number Signs ON Table Surfaces
            BuildTabletopCondiments(root.transform);

            // 6. Pass-Through Window Overhead Valance & Heat Lamp
            BuildPassThroughValance(root.transform);

            // 7. Exterior Takeout Window Canopy & Neon Sign
            BuildTakeoutExteriorCanopy(root.transform);

            // 8. Dining Room Decor, Planters & Neon Wall Sign (Clean World Canvas)
            BuildDiningDecorAndPlanters(root.transform);

            // 9. Kitchen Organization (Magnetic Knife Strip, Pot Rack & Analog Clock)
            BuildKitchenUtensilsAndClock(root.transform);

            Debug.Log("[RestaurantEnvironmentEnhancer] ✨ Restaurant environment design successfully enhanced!");
        }

        // ── 1. Zoned Flooring ─────────────────────────────────────────────────

        private void BuildZonedFlooring(Transform parent)
        {
            GameObject floorGroup = new GameObject("01_ZonedFloors");
            floorGroup.transform.SetParent(parent, false);

            // Dining Room Hardwood Parquet: X ~ 88.5 to 110.0, Z ~ -5.2 to -36.4
            CreatePrimitiveCube(floorGroup.transform, "Floor_DiningHardwood",
                new Vector3(99.25f, 0.012f, -20.8f),
                new Vector3(21.5f, 0.02f, 31.2f),
                hardwoodFloorMat);

            // Kitchen Heavy-Duty Non-Slip Tile: X ~ 110.0 to 122.5, Z ~ -14.0 to -27.5
            CreatePrimitiveCube(floorGroup.transform, "Floor_KitchenTile",
                new Vector3(116.25f, 0.015f, -20.89f),
                new Vector3(12.5f, 0.02f, 13.5f),
                kitchenTileMat);

            // Brass Threshold Transition Strip between Dining and Kitchen
            CreatePrimitiveCube(floorGroup.transform, "Floor_BrassThreshold",
                new Vector3(110.0f, 0.022f, -20.89f),
                new Vector3(0.18f, 0.03f, 13.5f),
                brassGoldMat);

            // Courier Parking Bay Threshold Accent
            CreatePrimitiveCube(floorGroup.transform, "Floor_CourierBayBorder",
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
            CreatePrimitiveCube(wallGroup.transform, "Wainscot_WestWall",
                new Vector3(88.55f, 0.55f, -20.8f),
                new Vector3(0.12f, 1.1f, 31.2f),
                wainscotingMat);

            // Chair Rail Molding Strip
            CreatePrimitiveCube(wallGroup.transform, "Molding_WestWall",
                new Vector3(88.58f, 1.12f, -20.8f),
                new Vector3(0.16f, 0.08f, 31.2f),
                brassGoldMat);

            // Dining Room South Wall Wainscoting
            CreatePrimitiveCube(wallGroup.transform, "Wainscot_SouthWall",
                new Vector3(99.25f, 0.55f, -36.35f),
                new Vector3(21.5f, 1.1f, 0.12f),
                wainscotingMat);

            // Dining Room North Wall Wainscoting
            CreatePrimitiveCube(wallGroup.transform, "Wainscot_NorthWall",
                new Vector3(99.25f, 0.55f, -5.25f),
                new Vector3(21.5f, 1.1f, 0.12f),
                wainscotingMat);

            // Kitchen Subway Tile Splashback (East Wall behind Stoves, Deep Fryer, Sink)
            CreatePrimitiveCube(wallGroup.transform, "Splashback_KitchenSubwayTile",
                new Vector3(121.32f, 1.4f, -20.89f),
                new Vector3(0.08f, 2.8f, 13.5f),
                subwayTileMat);
        }

        // ── 3. Commercial Exhaust Hood ─────────────────────────────────────────

        private void BuildKitchenExhaustHood(Transform parent)
        {
            GameObject hoodGroup = new GameObject("03_KitchenExhaustHood");
            hoodGroup.transform.SetParent(parent, false);

            Vector3 hoodCenter = new Vector3(121.0f, 2.45f, -18.75f);
            CreatePrimitiveCube(hoodGroup.transform, "Canopy_Body",
                hoodCenter,
                new Vector3(1.3f, 0.55f, 4.6f),
                stainlessSteelMat);

            CreatePrimitiveCube(hoodGroup.transform, "Canopy_Lip",
                hoodCenter + new Vector3(-0.65f, -0.25f, 0f),
                new Vector3(0.08f, 0.08f, 4.6f),
                stainlessSteelMat);

            // Dual Vertical Exhaust Ducts rising to ceiling
            CreatePrimitiveCube(hoodGroup.transform, "VentDuct_1",
                new Vector3(121.0f, 3.1f, -17.5f),
                new Vector3(0.45f, 0.8f, 0.45f),
                stainlessSteelMat);

            CreatePrimitiveCube(hoodGroup.transform, "VentDuct_2",
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
                new Vector3(100.74f, 0.0f, -14.84f),
                new Vector3(92.74f, 0.0f, -14.84f),
                new Vector3(100.74f, 0.0f, -22.34f),
                new Vector3(92.74f, 0.0f, -22.34f),
                new Vector3(100.74f, 0.0f, -29.84f),
                new Vector3(92.74f, 0.0f, -29.84f)
            };

            for (int i = 0; i < tablePositions.Length; i++)
            {
                Vector3 tablePos = tablePositions[i];
                GameObject pendant = new GameObject($"Pendant_Table_{i + 1}");
                pendant.transform.SetParent(lightsGroup.transform, false);

                // Suspension Wire
                CreatePrimitiveCube(pendant.transform, "Cord",
                    new Vector3(tablePos.x, 2.85f, tablePos.z),
                    new Vector3(0.03f, 1.2f, 0.03f),
                    wainscotingMat);

                // Brass Bell Shade
                CreatePrimitiveCube(pendant.transform, "BrassShade",
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

        // ── 5. Tabletop Condiments & Table Numbers (ON Tabletop Surface) ────────

        private void BuildTabletopCondiments(Transform parent)
        {
            GameObject condimentGroup = new GameObject("05_TabletopCondiments");
            condimentGroup.transform.SetParent(parent, false);

            for (int i = 1; i <= 6; i++)
            {
                GameObject tableObj = GameObject.Find($"EggTable_{i}");
                if (tableObj == null) continue;

                // Determine exact top surface height of this table
                float tableTopY = 1.00f;
                Transform tableTopTrans = tableObj.transform.Find("TableTop");
                if (tableTopTrans != null)
                {
                    Renderer r = tableTopTrans.GetComponent<Renderer>();
                    if (r != null) tableTopY = r.bounds.max.y;
                }

                // Offset towards the north/back edge of the table (Z + 0.40m) so serving area is unobstructed
                Vector3 basePos = new Vector3(tableObj.transform.position.x, tableTopY, tableObj.transform.position.z + 0.40f);

                GameObject set = new GameObject($"CondimentSet_Table_{i}");
                set.transform.SetParent(condimentGroup.transform, false);
                set.transform.position = basePos;

                // Condiment Caddy Tray resting flush on the table surface (Y = tableTopY to tableTopY + 0.02m)
                CreatePrimitiveCube(set.transform, "CaddyTray",
                    basePos + new Vector3(0f, 0.01f, 0f),
                    new Vector3(0.24f, 0.02f, 0.18f),
                    stainlessSteelMat);

                // Ketchup Squeeze Bottle (resting on tray at Y = tableTopY + 0.02m)
                CreatePrimitiveCylinder(set.transform, "Bottle_Ketchup",
                    basePos + new Vector3(-0.06f, 0.09f, 0f),
                    new Vector3(0.05f, 0.07f, 0.05f),
                    ketchupRedMat);

                // Mustard Squeeze Bottle (resting on tray at Y = tableTopY + 0.02m)
                CreatePrimitiveCylinder(set.transform, "Bottle_Mustard",
                    basePos + new Vector3(0.06f, 0.09f, 0f),
                    new Vector3(0.05f, 0.07f, 0.05f),
                    mustardYellowMat);

                // Salt Shaker (resting on tray at Y = tableTopY + 0.02m)
                CreatePrimitiveCylinder(set.transform, "Shaker_Salt",
                    basePos + new Vector3(-0.06f, 0.055f, -0.05f),
                    new Vector3(0.035f, 0.035f, 0.035f),
                    wallAccentMat);

                // Pepper Shaker (resting on tray at Y = tableTopY + 0.02m)
                CreatePrimitiveCylinder(set.transform, "Shaker_Pepper",
                    basePos + new Vector3(0.06f, 0.055f, -0.05f),
                    new Vector3(0.035f, 0.035f, 0.035f),
                    wainscotingMat);

                // Brass Stem for Table Number Badge (resting on tray at Y = tableTopY + 0.02m)
                CreatePrimitiveCylinder(set.transform, "NumberStandStem",
                    basePos + new Vector3(0f, 0.05f, 0.045f),
                    new Vector3(0.015f, 0.04f, 0.015f),
                    brassGoldMat);

                // Sleek Tabletop Number Badge matching the existing UI design (LookAtCamera, dark card, gold outline, LiberationSans bold)
                CreateExistingStyleCardUI(set.transform,
                    basePos + new Vector3(0f, 0.12f, 0.045f),
                    new Vector2(46f, 32f),
                    0.0035f,
                    $"#{i}",
                    18,
                    Color.white,
                    new Color(0.08f, 0.10f, 0.14f, 0.94f),
                    new Color(0.85f, 0.72f, 0.35f, 0.95f),
                    useLookAtCamera: true);
            }
        }

        // ── 6. Pass-Through Window Overhead Valance ───────────────────────────

        private void BuildPassThroughValance(Transform parent)
        {
            GameObject passGroup = new GameObject("06_PassThroughValance");
            passGroup.transform.SetParent(parent, false);

            Vector3 passCenter = new Vector3(110.64f, 1.85f, -20.69f);

            // Overhead Pass Valance Canopy
            CreatePrimitiveCube(passGroup.transform, "Valance_Header",
                passCenter,
                new Vector3(0.45f, 0.15f, 3.5f),
                wainscotingMat);

            // Stainless Steel Order Ticket Rail
            CreatePrimitiveCube(passGroup.transform, "TicketRail",
                passCenter + new Vector3(-0.18f, -0.08f, 0f),
                new Vector3(0.06f, 0.05f, 3.2f),
                stainlessSteelMat);

            // Simulated hanging paper tickets
            for (int t = -2; t <= 2; t++)
            {
                CreatePrimitiveCube(passGroup.transform, $"OrderTicket_{t + 3}",
                    passCenter + new Vector3(-0.21f, -0.16f, t * 0.55f),
                    new Vector3(0.01f, 0.16f, 0.18f),
                    wallAccentMat);
            }

            // Emissive Heat-Lamp Bar
            CreatePrimitiveCube(passGroup.transform, "HeatLamp_Housing",
                passCenter + new Vector3(0f, -0.06f, 0f),
                new Vector3(0.18f, 0.06f, 2.8f),
                stainlessSteelMat);

            CreatePrimitiveCube(passGroup.transform, "HeatLamp_Tube",
                passCenter + new Vector3(0f, -0.10f, 0f),
                new Vector3(0.08f, 0.04f, 2.6f),
                neonAmberMat);

            // Amber Heat Lamp Glow
            CreatePointLight(passGroup.transform, "HeatLamp_Glow",
                passCenter + new Vector3(0f, -0.25f, 0f),
                new Color(1.0f, 0.65f, 0.20f),
                2.8f, 3.2f);
        }

        // ── 7. Exterior Takeout Canopy & Signage ───────────────────────────────

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

            for (int s = -4; s <= 4; s++)
            {
                Material m = (s % 2 == 0) ? awningRedMat : awningWhiteMat;
                CreatePrimitiveCube(awning.transform, $"AwningSlat_{s + 5}",
                    new Vector3(s * 0.28f, 0f, 0.65f),
                    new Vector3(0.28f, 0.05f, 1.4f),
                    m);
            }

            // Signboard frame above Takeout Counter
            CreatePrimitiveCube(extGroup.transform, "Sign_TakeoutHeader",
                new Vector3(110.64f, 2.95f, -23.0f),
                new Vector3(0.12f, 0.45f, 2.8f),
                wainscotingMat);

            // Cyan neon border
            CreatePrimitiveCube(extGroup.transform, "Sign_NeonBorder",
                new Vector3(110.57f, 2.95f, -23.0f),
                new Vector3(0.04f, 0.40f, 2.6f),
                neonCyanMat);

            // Takeout & Delivery Header Card matching existing UI design (LookAtCamera, cyan outline, LiberationSans bold)
            CreateExistingStyleCardUI(extGroup.transform,
                new Vector3(110.53f, 2.95f, -23.0f),
                new Vector2(340f, 50f),
                0.007f,
                "TAKE OUT & DELIVERY",
                20,
                Color.cyan,
                new Color(0.08f, 0.10f, 0.14f, 0.94f),
                new Color(0.0f, 0.85f, 0.95f, 0.95f),
                useLookAtCamera: true);
        }

        // ── 8. Dining Room Decor, Planters & Neon Wall Sign ────────────────────

        private void BuildDiningDecorAndPlanters(Transform parent)
        {
            GameObject decorGroup = new GameObject("08_DiningDecorAndPlanters");
            decorGroup.transform.SetParent(parent, false);

            // Low Planter Divider Boxes between table rows
            BuildPlanterDivider(decorGroup.transform, "PlanterDivider_1", new Vector3(96.74f, 0.45f, -18.5f));
            BuildPlanterDivider(decorGroup.transform, "PlanterDivider_2", new Vector3(96.74f, 0.45f, -26.0f));

            // Corner Indoor Palm Planters
            BuildCornerPlanter(decorGroup.transform, "CornerPlant_NW", new Vector3(89.5f, 0f, -6.5f));
            BuildCornerPlanter(decorGroup.transform, "CornerPlant_SW", new Vector3(89.5f, 0f, -34.8f));

            // Framed Chalkboard Menu on West Wall (pos = 88.62, Z = -20.8)
            CreatePrimitiveCube(decorGroup.transform, "ChalkboardMenu_Frame",
                new Vector3(88.62f, 2.1f, -20.8f),
                new Vector3(0.08f, 1.4f, 2.6f),
                wainscotingMat);

            CreatePrimitiveCube(decorGroup.transform, "ChalkboardMenu_Blackboard",
                new Vector3(88.66f, 2.1f, -20.8f),
                new Vector3(0.04f, 1.25f, 2.4f),
                kitchenTileMat);

            // Framed Chalkboard Menu Card on West Wall matching existing UI design (LookAtCamera, gold outline, LiberationSans)
            CreateExistingStyleCardUI(decorGroup.transform,
                new Vector3(88.75f, 2.1f, -20.8f),
                new Vector2(300f, 180f),
                0.0065f,
                "<b>TODAY'S SPECIALS</b>\n\n• Deluxe Combo Meal  +$30\n• Crispy French Fries\n• Fountain Drinks\n\n<i>Fast Delivery • Hot & Fresh!</i>",
                14,
                new Color(0.95f, 0.95f, 0.88f),
                new Color(0.08f, 0.10f, 0.14f, 0.95f),
                new Color(0.85f, 0.72f, 0.35f, 0.90f),
                useLookAtCamera: true);

            // Neon Sign: "DISH-ASTER DINER" on South Wall (facing North 0°)
            CreatePrimitiveCube(decorGroup.transform, "NeonSign_Backdrop",
                new Vector3(99.25f, 2.45f, -36.25f),
                new Vector3(4.5f, 0.85f, 0.08f),
                wainscotingMat);

            CreatePrimitiveCube(decorGroup.transform, "NeonSign_GlowBorder",
                new Vector3(99.25f, 2.45f, -36.18f),
                new Vector3(4.3f, 0.72f, 0.04f),
                neonAmberMat);

            // Glowing Neon Diner Header Card matching existing UI design (LookAtCamera, amber outline, LiberationSans bold)
            CreateExistingStyleCardUI(decorGroup.transform,
                new Vector3(99.25f, 2.45f, -36.10f),
                new Vector2(360f, 65f),
                0.009f,
                "DISH-ASTER DINER",
                26,
                new Color(1f, 0.85f, 0.25f),
                new Color(0.08f, 0.10f, 0.14f, 0.95f),
                new Color(1.0f, 0.65f, 0.15f, 0.95f),
                useLookAtCamera: true);

            CreatePointLight(decorGroup.transform, "NeonSign_AmbientGlow",
                new Vector3(99.25f, 2.3f, -35.8f),
                new Color(1f, 0.65f, 0.15f),
                2.2f, 4.5f);
        }

        private void BuildPlanterDivider(Transform parent, string name, Vector3 pos)
        {
            GameObject planter = new GameObject(name);
            planter.transform.SetParent(parent, false);

            CreatePrimitiveCube(planter.transform, "Box",
                pos,
                new Vector3(4.2f, 0.85f, 0.55f),
                wainscotingMat);

            CreatePrimitiveCube(planter.transform, "Soil",
                pos + new Vector3(0f, 0.42f, 0f),
                new Vector3(4.0f, 0.05f, 0.45f),
                wainscotingMat);

            CreatePrimitiveCube(planter.transform, "Foliage",
                pos + new Vector3(0f, 0.62f, 0f),
                new Vector3(3.8f, 0.40f, 0.38f),
                plantGreenMat);
        }

        private void BuildCornerPlanter(Transform parent, string name, Vector3 pos)
        {
            GameObject plant = new GameObject(name);
            plant.transform.SetParent(parent, false);

            CreatePrimitiveCylinder(plant.transform, "Pot",
                pos + new Vector3(0f, 0.45f, 0f),
                new Vector3(0.65f, 0.9f, 0.65f),
                wallAccentMat);

            CreatePrimitiveCube(plant.transform, "Leaves_1",
                pos + new Vector3(0f, 1.15f, 0f),
                new Vector3(0.9f, 0.7f, 0.9f),
                plantGreenMat);

            CreatePrimitiveCube(plant.transform, "Leaves_2",
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
            CreatePrimitiveCube(kitchenDetails.transform, "MagneticKnifeStrip",
                new Vector3(118.0f, 1.55f, -15.58f),
                new Vector3(1.6f, 0.12f, 0.05f),
                stainlessSteelMat);

            for (int k = -1; k <= 2; k++)
            {
                CreatePrimitiveCube(kitchenDetails.transform, $"Knife_{k + 2}",
                    new Vector3(118.0f + (k * 0.32f), 1.50f, -15.52f),
                    new Vector3(0.04f, 0.32f, 0.02f),
                    stainlessSteelMat);
            }

            // Hanging Pot Rack above Sink (X: 121.14, Z: -20.14, Y: 2.2)
            CreatePrimitiveCube(kitchenDetails.transform, "HangingPotRack_Bar",
                new Vector3(121.0f, 2.2f, -20.14f),
                new Vector3(0.06f, 0.06f, 1.6f),
                brassGoldMat);

            // Analog Wall Clock above Pass-Through Window (facing West 270°)
            Vector3 clockPos = new Vector3(110.64f, 2.35f, -19.0f);
            CreatePrimitiveCylinder(kitchenDetails.transform, "ClockBody",
                clockPos,
                new Vector3(0.55f, 0.05f, 0.55f),
                wallAccentMat,
                Quaternion.Euler(0f, 0f, 90f));

            CreatePrimitiveCylinder(kitchenDetails.transform, "ClockRim",
                clockPos + new Vector3(-0.02f, 0f, 0f),
                new Vector3(0.58f, 0.02f, 0.58f),
                brassGoldMat,
                Quaternion.Euler(0f, 0f, 90f));

            // Clock hands
            CreatePrimitiveCube(kitchenDetails.transform, "ClockHandHour",
                clockPos + new Vector3(-0.035f, 0.06f, 0f),
                new Vector3(0.01f, 0.14f, 0.02f),
                wainscotingMat);

            CreatePrimitiveCube(kitchenDetails.transform, "ClockHandMinute",
                clockPos + new Vector3(-0.035f, 0f, 0.09f),
                new Vector3(0.01f, 0.02f, 0.20f),
                wainscotingMat);
        }

        // ── Clean Geometric & UI Helpers ──────────────────────────────────────

        private GameObject CreatePrimitiveCube(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;

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

        private GameObject CreateExistingStyleCardUI(
            Transform parent,
            Vector3 pos,
            Vector2 sizeDelta,
            float scaleFactor,
            string text,
            float fontSize,
            Color textColor,
            Color bgColor,
            Color borderColor,
            bool useLookAtCamera = true)
        {
            GameObject canvasGo = new GameObject("WorldCardUI");
            canvasGo.transform.SetParent(parent, false);
            canvasGo.transform.position = pos;
            canvasGo.transform.localScale = Vector3.one * scaleFactor;

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = sizeDelta;

            // Background card matching existing UI styling (DeepFryer, Sink, TableId)
            Image bg = canvasGo.AddComponent<Image>();
            bg.color = bgColor;
            bg.raycastTarget = false;

            // Accent Outline matching existing UI
            Outline outline = canvasGo.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Text Label using LiberationSans SDF
            GameObject textObj = new GameObject("Text_Label");
            textObj.transform.SetParent(canvasGo.transform, false);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.rectTransform.sizeDelta = sizeDelta - new Vector2(8f, 8f);
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

            // LookAtCamera to guarantee left-to-right reading & seamless camera alignment
            if (useLookAtCamera)
            {
                LookAtCamera la = canvasGo.AddComponent<LookAtCamera>();
                var field = typeof(LookAtCamera).GetField("mode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    var modeEnum = typeof(LookAtCamera).GetNestedType("Mode", System.Reflection.BindingFlags.NonPublic);
                    if (modeEnum != null)
                    {
                        var val = System.Enum.Parse(modeEnum, "LookAtInverted");
                        field.SetValue(la, val);
                    }
                }
            }

            return canvasGo;
        }
    }
}
