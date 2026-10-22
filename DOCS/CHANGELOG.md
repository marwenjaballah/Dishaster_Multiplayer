# 📜 CHANGELOG
All notable changes to the **Dishaster: City Express** project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

## [0.33.0-scene-hierarchy-categorization] - 2026-10-22
*Milestone D: Professional Scene Hierarchy Categorization & Cleanup (EXP-017).*

### Added & Reorganized
- **Professional Scene Hierarchy Architecture (`GameSceneTableService.unity`):**
  - **Category Folders:** Grouped 27 scattered root-level GameObjects into 5 clean, collapsible category hierarchies:
    - `--- 01_SYSTEMS & MANAGERS ---`: All 14 system singletons and managers (`KitchenGameManager`, `DeliveryManager`, `TableManager`, `KitchenCrisisManager`, `RestaurantEconomyManager`, `DifficultyManager`, `CoopPingManager`, `SoloChefHotSwapManager`, `GameSaveManager`, `LevelSelectionManager`, `GameInput`, `SoundManager`, `MusicManager`, `RuntimeNetStatsMonitor`).
    - `--- 02_CAMERAS & LIGHTING ---`: All view and environmental lighting systems (`Main Camera`, `Virtual Camera`, `Directional Light`, `Global Volume`).
    - `--- 03_ENVIRONMENT & WORLD ---`: Physical world architecture (`Restaurant`, `CitySystem`).
    - `--- 04_GAMEPLAY & VEHICLES ---`: Interactive entities and vehicles (`ElectricScooter`, `DeliveryTarget3DIndicator`).
    - `--- 05_UI & CANVASES ---`: User interface systems (`EventSystem`, `Canvas`, `MobileTouchControlsUI`, `LevelSelectUI`).
  - **Counter Consolidation in `Restaurant/Counters`:**
    - Reparented `DeliveryBagCounter` (previously stranded at scene root) and the 4 crisis utility counters (`WaterLeakValveCounter`, `FuseBoxCounter`, `FireExtinguisherCounter`, `PipeWrenchCounter`) into `Restaurant/Counters`.
    - All 31 counters now live together in a single consolidated, cleanly organized location with exact world transforms preserved.
  - **Hierarchy Hygiene:**
    - Removed duplicate ` (1)` name suffixes across 12 GameObjects (`KitchenGameManager`, `DeliveryManager`, `SoundManager`, `MusicManager`, `GameInput`, `EventSystem`, `Canvas`, `Main Camera`, `Virtual Camera`, `Directional Light`, `Global Volume`, `RuntimeNetStatsMonitor`, `Counters`).
    - Logically ordered all 17 HUD sub-panels under `Canvas` by gameplay flow (Pre-game $\rightarrow$ Gameplay HUDs $\rightarrow$ Multiplayer/Pause $\rightarrow$ Results/Options).
  - **Verification:**
    - Validated all singletons, cameras, Netcode bindings, and serialized references with **0 missing references, 0 errors, and 0 warnings**.

## [0.32.0-kitchen-hud-zero-occlusion-sightlines] - 2026-10-22
*Milestone D: Kitchen HUD Zero-Occlusion Sightlines Overhaul (EXP-016).*

### Fixed & Enhanced
- **Zero-Occlusion Isometric Sightline Architecture (`RestaurantEnvironmentEnhancer.cs`):**
  - **Overhead Dining Ceiling Beams Confined to Dining Hall:**
    - Truncated rustic wooden ceiling beams (`CeilingBeam_1` through `CeilingBeam_4`) to span strictly across the dining hall from $X = 88.57\text{m}$ to $110.57\text{m}$ ($\text{width} = 22.0\text{m}$, centered at $X = 99.57\text{m}$).
    - Eliminated previous kitchen airspace overhang where `CeilingBeam_3` directly occluded camera rays to `CuttingCounter_Bottom`, `DeepFryerCounterUI`, and `BeverageDispenserCounterUI`.
  - **Cook Line Ventilation Elevation:**
    - Replaced protruding 3D canopy hoods and hanging vertical roof duct tubes with a sleek, flush-mounted commercial exhaust plenum mounted against the East subway wall ($X = 121.88\text{m}$) at $Y = 3.35\text{m}$—comfortably above the highest HUD element (`StoveBurnWarningUI` at $Y = 3.00\text{m}$) with zero counter overhang.
  - **Center Prep Island Airspace Opening:**
    - Completely removed overhead double-deck gantry stanchions, overhead shelves, and hanging utensils from above `Island_Cutting` and `Island_Clear_1`.
    - Relocated all 6 stainless/polycarbonate mise-en-place pans (lettuce, onions, cheese, patties, tomatoes, pickles) and the chef squeeze bottle caddy to a wall-mounted stainless shelf flush against the North subway wall behind the vegetable counters ($Z = -14.82\text{m}, Y = 1.65\text{m}$).
    - High-visibility commercial LED clock/timer mounted flush on North subway wall facing South ($Z = -14.78\text{m}, Y = 2.45\text{m}$).
  - **South Kitchen Line Sightline Clearance:**
    - Converted the former full-height south subway tile wall to a low-profile counter-height ($0.90\text{m}$) sanitary stainless kick-plate flush behind the counter backs.
    - Removed `FryerExhaust_WallVent` from $Z = -27.02\text{m}$ which previously crossed the downward 46° camera ray to `DeepFryerCounterUI`.
  - **Scullery Splash Rims & Pass-Through Clearance:**
    - Maintained sink side splash shields at counter level ($Y = 0.95\text{m}$), 1.45m below `SinkCounterUI` ($Y = 2.40\text{m}$).
    - Raised pass-through valance header to ceiling line ($Y = 2.85\text{m}$) and converted all general kitchen lighting to flush ceiling task downlights ($Y = 3.60\text{m}$).
- **Verification & Raycast Audits:**
  - Automated raycast simulation executed across 10 distinct player kitchen stations (Stove 1, Stove 2, Sink, Deep Fryer, Cutting Bottom, Beverage Dispenser, Island North, Island South, Cutting Top, Pass-through) testing all 14 interactive kitchen HUD canvases (`ProgressBarUI`, `StoveBurnWarningUI`, `DeepFryerCounterUI`, `BeverageDispenserCounterUI`, `SinkCounterUI`, `InteractKeyPromptUI`).
  - Passed **140 out of 140 dynamic line-of-sight sightline tests with 0 occlusions (100% visibility)**.
  - Scene saved in `Assets/Scenes/GameSceneTableService.unity`. Zero compiler errors, zero warnings.

## [0.31.0-commercial-kitchen-scullery-overhaul] - 2026-10-22
*Milestone D: Commercial Kitchen Infrastructure, Scullery & Ergonomics Overhaul (EXP-015).*

### Added
- **Commercial Kitchen Master Systems (`RestaurantEnvironmentEnhancer.cs`):**
  - **Ergonomics & Flooring:**
    - Heavy-duty slotted anti-fatigue rubber drainage mats (`Restaurant_RubberFloorMat`) along the hot cook line, north cold prep line, south fryer line, and around the center island pad.
  - **Sanitary Wall Cladding & Hygiene Regulations:**
    - Full 3-wall white ceramic subway tile wrap with dark grout (`Restaurant_SubwayTile`) across East cook line, North prep line, and South fryer/beverage line.
    - Commercial stainless steel sanitary cove baseboards (0.12m high) running along all kitchen wall intersections.
    - Heavy-duty stainless steel corner impact guards on all exposed corners.
    - Official Department of Public Health Certificate of Inspection ("GRADE A - SCORE 100") framed beside the entrance.
    - OSHA/Health Code "EMPLOYEES MUST WASH HANDS" regulation sign mounted by the handwash station.
  - **Scullery & Commercial Dishwashing Pit:**
    - Heavy-duty commercial pre-rinse high-pressure coiled spring faucet with chrome spray valve and squeeze trigger.
    - Dual stainless steel side splash shields flanking `SinkCounter` to isolate dish washing from cook stoves and clean plates.
    - Automated wall-mounted dual-chemical dosing dispenser with blue detergent and green sanitizer feed lines.
    - Commercial blue polypropylene peg dish drying racks with upright ceramic plates.
    - Color-coded sanitation buckets (Red for sanitizer, Green for detergent).
    - Wall-mounted lever paper towel dispenser with pull-down towel.
  - **Hot Cook Line & ANSUL Fire Suppression System:**
    - Full-length stainless range hood canopy with angled removable grease baffle filters and twin rooftop exhaust ducts.
    - Dedicated fryer exhaust canopy over `DeepFryerCounter`.
    - Industrial ANSUL R-102 fire suppression system with red agent cylinder, pressure gauge, emergency manual pull station, stainless distribution manifold, and 4 down-firing brass/yellow discharge nozzles over burners and fryers.
  - **Center Prep Island Double-Deck Gantry:**
    - 4 stainless tubular support stanchions rising to Y = 2.25m over the prep island.
    - Lower gantry shelf holding 2 stainless 1/3 hotel pans (shredded lettuce, sliced onions) and 4 amber polycarbonate Cambro pans (`Restaurant_PlasticHotelPan`) with cheese slices, burger patties, tomato slices, and pickles.
    - Chef squeeze bottle wire organizer caddy with 4 bottles (ketchup, mustard, aioli, olive oil).
    - Commercial digital kitchen LED clock / shift timer ("12:45 PM -- SHIFT 2").
    - Upper gantry shelf with 3 nested stainless mixing bowls and commercial aluminum half-sheet pans.
    - Hanging stainless chef utensils (tongs, grill spatulas, skimmers, whisks).
  - **South Line & Utility Storage:**
    - Stainless steel splash barrier separating `DeepFryerCounter` from `BeverageDispenserCounter`.
    - Stack of clean French fry cartons beside the fryer.
    - Commercial 4-tier chrome Metro wire dry storage rack holding 50 lb flour sacks, 5-liter Italian olive oil tins, and crates of canned San Marzano plum tomatoes.
  - **Expediting & High-Bay Lighting:**
    - Double-sided quartz warming heat lamps, rotating ticket order carousel tree, and expeditor dry-erase board ("RUSH TABLE 2 -- 86 TOMATOES").
    - 4 ceiling-recessed 4500K commercial LED troffer task fixtures providing crisp, shadow-free illumination.
- **New URP Lit Materials in `Assets/_Assets/Materials/Restaurant/`:**
  - `Restaurant_RubberFloorMat.mat`, `Restaurant_AnsulFirePiping.mat`, `Restaurant_ChromePolished.mat`, `Restaurant_PlasticHotelPan.mat`, `Restaurant_SanitizerBlue.mat`.
- Verified in Unity 6 Editor and Play Mode with **0 compilation errors, 0 warnings, and 0 collider overlaps across all 35 interactive furniture colliders**.

## [0.30.0-the-gilded-spoon-diner-master-redesign] - 2026-10-22
*Milestone D: Complete Restaurant Interior & Architectural Master Redesign (EXP-014).*

### Added
- **"The Gilded Spoon Diner & Bistro" Environmental Masterwork (`RestaurantEnvironmentEnhancer.cs`):**
  - **Zoned Floor Architecture:**
    - Dining room: Warm polished dark herringbone parquet (`Floor_DiningHardwood`) with satin sheen.
    - Plush vintage burgundy velvet carpet insets (`Carpet_BoothRow_1-3`) framed with brass trim beneath all dining booths.
    - Kitchen: Classic commercial slate tile with high-traffic checkerboard quarry tile runner.
    - Entrance vestibule: Stone hexagonal entryway mat with brass threshold transition.
  - **Architectural Envelope & Windows:**
    - West accent wall: Upper exposed red brick texture (`Restaurant_ExposedBrick`) with lower dark mahogany wainscoting and brass chair rail.
    - 3 panoramic multi-pane French arched city-view windows with dark bronze frames, translucent glass panes (`Restaurant_WindowGlass`), interior walnut sills, and warm exterior sunlight spill lights.
    - North wall: Grand arched brass wall mirror (`Mirror_Frame` & `Mirror_Glass`) creating deep visual room reflection.
    - South wall: Double mahogany entrance doors with frosted glass, brass kickplates, and push bars.
  - **Luxury Dining Booths & Bistro Seating Sets:**
    - Replaced generic egg tables with handcrafted diner furniture:
      - West column (Tables 2, 4, 6): Double-sided high-back fluted banquettes in rich burgundy tufted leather (`Restaurant_BoothLeatherBurgundy`), walnut tabletops with brass rim edging, and cast-iron pedestals.
      - Center column (Tables 1, 3, 5): Round walnut bistro tables with brass rims and pair of bentwood bistro chairs with emerald green leather cushions (`Restaurant_BoothLeatherEmerald`).
      - Each table dressed with linen runner, glass ketchup/mustard bottles, salt/pepper shakers, mini brass mood candle lamp with golden glow, and clean diegetic table tent signs (`#1` to `#6`).
      - Realigned AI seating anchors (`EggStool_X_Left/Right`) directly onto booth and chair cushions for natural `DineInCustomerController` sitting kinematics.
  - **Maître D' Host Station & Waiting Lounge:**
    - Carved mahogany host podium with reservation ledger book, seating clipboard, and brass desk lamp.
    - 2.6m golden velvet tufted guest waiting bench (`Restaurant_VelvetPlush`).
    - Standing vintage brass coat and umbrella rack.
    - Real 3D potted ficus trees (`Prop_Plant_01`, `Prop_Plant_02`) flanking the vestibule.
  - **Diner Service Bar & Coffee Station:**
    - 5.7m polished white Carrera marble bar counter (`Restaurant_MarbleBarTop`) with mahogany front and brass footrail.
    - 4 chrome swivel diner barstools with red padded leather seats.
    - Commercial stainless espresso machine with portafilters, glass drip coffee carafes, tiered glass pastry dome with chocolate cherry layer cake, and chrome napkin/straw dispensers.
  - **Exhibition Kitchen & Pass-Through:**
    - Pass-through window partition with Carrera marble pass shelf, heat lamps with warm holding glow, ticket rail, brass service call bell, and double-swinging chef saloon doors with brass portholes.
    - Full stainless range hood with grease filters, rooftop exhaust ducts, and halogen cook line downlights.
    - Chef spice rack with olive oil cruets and spice jars on subway tile.
  - **Atmosphere, Vintage Art & Retro Jukebox:**
    - 1950s illuminated arched retro jukebox with rainbow neon tubes (`Restaurant_JukeboxNeon`) and glowing interior.
    - Large framed daily specials chalkboard menu with clean World-Space TextMeshPro typography.
    - Radiant glowing neon wall script sign ("DISH-ASTER DINER -- BEST BURGERS & SHAKES IN TOWN --").
    - Overhead dark timber ceiling rafters (`Restaurant_CeilingBeam`) with suspended spiral ventilation ductwork (`Prop_ACVent_Stright`).
    - Suspended brass bell pendant lamps over all dining tables.
  - Verified in Unity 6 Editor and Play Mode with **0 compilation errors, 0 warnings, and 0 volumetric collider overlaps across all 44 colliders**.

## [0.29.0-kitchen-collision-potato-pipeline] - 2026-10-22
*Milestone D: Kitchen Enhancement, Zero-Collision Layout & Complete Potato Culinary Pipeline (EXP-013).*

### Added
- **Dedicated Container Counter for Potatoes (`ContainerCounter_Potato.prefab`):**
  - Created and registered `ContainerCounter_Potato.prefab` in `Assets/DefaultNetworkPrefabs.asset` with standard `ContainerCounter` Netcode logic.
  - Custom 2D HUD/Container icon `Assets/_Assets/Textures/Icons/Potato.png` assigned to `ObjectSprite`.
  - Added a diegetic 3D rustic wooden potato crate on top of the counter holding 6 miniature textured potatoes for immediate visual recognition.
  - Replaced the scene's empty `ClearCounter` placeholder with this new prefab instance at `(112.14, 0.00, -26.14)`.
- **High-Resolution Culinary 2D Sprites & URP Lit Materials:**
  - `Potato.png`, `PotatoSlices.png`, `FrenchFries.png`, `SodaCup.png` with crisp pixel-perfect UI alpha transparency.
  - 6 calibrated URP Lit materials in `Assets/_Assets/Materials/Food/` for realistic golden fries, potato skin/flesh, and delivery soda cups.
- **Complete Culinary Recipe Pipeline:**
  - Wired `Potato-PotatoSlices` cutting recipe to `CuttingCounter.prefab` and all scene cutting stations.
  - Configured `DeepFryerCounter` with `fryInputSO = PotatoSlices`, frying into `FrenchFries` and burning to `MeatPattyBurned`.
  - Added ingredient validation in `DeepFryerCounter.Interact()` to prevent plates or invalid food items from entering the boiling oil.
  - Registered `FrenchFries` and `SodaCup` in `Plate.prefab`'s valid ingredient list and verified procedural 3D plate visualization.
- **Kitchen Collision & Pass-Through Clearance Overhaul:**
  - Relocated `FireExtinguisherCounter` from the front of the pass-through window to North utility station `(109.18, 0.00, -13.90)`.
  - Relocated `PipeWrenchCounter` from the front of the pass-through window to South utility station `(108.94, 0.00, -27.14)` adjacent to `WaterLeakValveCounter`.
  - Pass-Through window (`PassThrough_Counter_1` & `PassThrough_Counter_2`) now has over 2 meters of clear unobstructed floor space on both kitchen and dining room sides.
  - Shifted `Splashback_KitchenSubwayTile` flush against the East wall at $X = 121.90\text{m}$ (0 penetration into cook line counters).
  - Added stainless steel chef spice rack with condiment cruets and warm overhead island prep spotlight (`Island_PrepSpotlight`).
  - Removed redundant mesh colliders on customer tables. Scene collision audit verified **0 volumetric overlaps across all 80 colliders**.

## [0.28.0-restaurant-interior-design-overhaul] - 2026-10-22
*Milestone D: Restaurant Interior Environment & Architectural Overhaul (EXP-012).*

### Added
- **Zoned Floor Architecture (`RestaurantEnvironmentEnhancer.cs`):**
  - Replaced prototype single-plane floor with dual-zone architecture:
    - **Dining Room:** Warm polished hardwood diner parquet flooring with subtle satin sheen.
    - **Kitchen:** Heavy-duty commercial slate/terracotta non-slip tile with neat joint grid.
    - **Service Boundary:** Satin brass transition strip running between kitchen and dining hall.
    - **Courier Bay:** Exterior threshold border framing the scooter takeout parking bay.
- **Architectural Wainscoting & Subway Splashbacks:**
  - Added lower 1.1m dark mahogany wood wainscoting paneling with brass chair-rail molding along all dining room walls.
  - Added glossy white commercial subway tile splashback running along the full cook line behind the stoves, deep fryer, and sink counters.
- **Commercial Kitchen Stainless Exhaust Hood & Downlights:**
  - Installed a stainless steel range hood canopy over the stoves and deep fryer with twin vertical exhaust ventilation ducts rising to the ceiling.
  - Added dual warm halogen recessed downlights illuminating the cook line workstations.
- **Cozy Suspended Dining Pendant Lighting:**
  - Suspended 6 brass bell pendant lamps directly centered over all dining tables (`EggTable_1` through `EggTable_6`) with warm amber point light illumination.
- **Table Service Condiments & Diegetic Table Numbers:**
  - Equipped each dining table with tabletop condiment caddies: classic red ketchup & yellow mustard squeeze bottles, salt and pepper shakers, and 3D table number tent stands (`#1` through `#6`). Colliders disabled to guarantee zero interference with plate serving triggers.
- **Pass-Through Window Header & Exterior Takeout Canopy:**
  - Added overhead wooden pass valance with stainless order ticket rail and amber heat-lamp holding glow.
  - Installed retro red-and-white striped exterior canvas awning over the takeout pickup window with neon signage board ("TAKE OUT & DELIVERY").
- **Dining Decor, Planters & Neon Sign:**
  - Low planter divider boxes with rich green foliage separating dining rows and guiding customer flow.
  - Large framed daily specials chalkboard menu and radiant glowing neon wall sign ("DISH-ASTER DINER").
  - Kitchen magnetic chef knife rack, hanging pot rack, and shift wall clock.
  - Added `RestaurantEnvironmentEnhancer.ResetStaticData()` wired to `ResetStaticDataManager`.
  - Verified in Unity 6 Editor and live Play Mode with 0 errors and 0 warnings.

### Fixed
- **Tabletop Prop Elevations & Bounds Clamping (`RestaurantEnvironmentEnhancer.cs`):**
  - Recalculated tabletop heights directly from `TableTop` renderer bounds ($Y = 0.995\text{m}$). Raised condiment caddies, ketchup/mustard bottles, salt/pepper shakers, and table number tent stands from floor level to sit flush on table surfaces ($Y = 1.005\text{m} - 1.155\text{m}$) offset to the back edge ($Z + 0.40\text{m}$) with colliders stripped to preserve clean plate delivery interaction zones.
- **Diegetic Typography & World-Space Canvas Scaling:**
  - Replaced legacy 3D `TextMeshPro` text meshes (which suffered from non-uniform parent scale distortions causing oversized 15–30m floating letters) with dedicated `Canvas` components configured in `RenderMode.WorldSpace` using `TextMeshProUGUI` with micro-scale anchoring (`0.001f - 0.0035f`).
  - Clamped all diegetic signage (chalkboard daily specials, neon diner logo, takeout header, and table number tents `#1`–`#6`) neatly inside their respective architectural backdrops.
- **Static Architectural Wall Signage & Left-to-Right Orientation (`RestaurantEnvironmentEnhancer.cs`):**
  - Removed `LookAtCamera` from all architectural wall signage to preserve realistic, flush in-world decor (Chalkboard Menu, South Wall Neon Sign, Takeout Header) and tabletop brass tents.
  - Implemented `CreateWallSignUI` with mathematically verified fixed rotations:
    - West Wall Chalkboard: `rot = (0, 270, 0)` flush on blackboard, reading strictly left-to-right when viewed from the dining hall.
    - South Wall Neon Sign: `rot = (0, 180, 0)` flush on South wall, reading strictly left-to-right when viewed looking South.
    - Takeout Window Header: `rot = (0, 90, 0)` flush on awning canopy, reading strictly left-to-right when approaching from the courier driveway.
    - Tabletop Condiment Tents: 3D brass tent blocks with dual-sided angled faces (South face `rot = (-15, 0, 0)`, North face `rot = (-15, 180, 0)`), both reading `#1`–`#6` strictly left-to-right without dynamic camera billboarding.
  - Updated `TableOrderUI.cs` so floating table numbers (`TableId`) remain visible within viewing range (25m), keeping recipe food panels hidden until customers are seated.

*Milestone D: Level 2 & Level 3 Environments, Courier Scaling & Venue Selection (EXP-011).*

### Added
- **Multi-Level Progression & Venue System (`LevelSelectionManager.cs`):**
  - Configured 3 distinct restaurant venue profiles:
    - **Level 1 (The Sunny Side Diner):** Cozy suburban diner, $150 daily rent, 1.0x delivery distance (18–120m), 1.0x customer patience, warm daylight illumination.
    - **Level 2 (Downtown Burger Bar):** Bustling urban center, $250 daily rent, 1.4x delivery distance (25–168m), 0.85x fast-paced customer patience, electric cyan evening dusk lighting.
    - **Level 3 (Boardwalk Grill):** High-stakes coastal venue, $400 daily rent, 1.8x delivery distance (32–216m), 0.75x rush customer patience, golden sunset illumination.
  - Server-authoritative level selection with `RequestSelectLevelRpc` and campaign unlock validation via `GameSaveManager.IsLevelUnlocked`.
  - Supports both offline solo play and online multiplayer lobbies.
- **Dynamic Level Economy & Logistics Scaling:**
  - Connected `DayLoopManager.CalculateRentForDay` to automatically apply active level base daily rent.
  - Connected `DeliveryCustomerManager` to scale courier delivery radii (`deliveryDistanceScale`) and customer patience timers (`customerPatienceMultiplier`).
- **City Venue Browser UI (`LevelSelectUI.cs`):**
  - Designed interactive modal UI showcasing all 3 restaurant venues with stats, rent requirements, courier distance curves, best shift records, and 1–3 star ratings.
  - Interactive selection buttons and locked badges based on campaign save data.
  - Added `LevelSelectUI.ResetStaticData()` wired to `ResetStaticDataManager`.
  - Deployed `LevelSelectionManager` and `LevelSelectUI` in `GameSceneTableService.unity`.
  - Verified in Unity 6 Editor and Play Mode with 0 errors and 0 warnings.

## [0.26.0-mobile-touch-controls-polish] - 2026-10-22
*Milestone D: Mobile Touch Controls Polish & Safe-Area Adaptation (EXP-010).*

### Added
- **Mobile Safe-Area Notch Adaptation (`MobileTouchControlsUI.cs`):**
  - Implemented dynamic `Screen.safeArea` anchor calculation and root padding container, ensuring HUD buttons and virtual joystick avoid display cutouts, camera notches, and home indicator bars across iOS and Android devices.
- **Virtual Joystick Deadzone & Smooth Response Curve:**
  - Added configurable 15% joystick deadzone (`joystickDeadzone = 0.15f`) to eliminate idle thumb jitter and drift.
  - Implemented smooth normalized remapping above deadzone for precise analog steering and walking kinematics.
- **Tactile Touch Haptics:**
  - Added optional tactile haptic pulses (`Handheld.Vibrate()`) on button down for Interact, Action, Ping, and Pause.
- **Solo Mode On-Screen Swap Chef Button:**
  - Added dedicated on-screen `SWAP CHEF` touch button dynamically visible during 1-player sessions, allowing mobile players to hot-swap between Chef A and Chef B without requiring physical keyboard/gamepad inputs.
- **Scene Deployment:**
  - Deployed `MobileTouchControlsUI` in `GameSceneTableService.unity`.
  - Verified in Unity 6 Editor with 0 errors and 0 warnings.

## [0.25.0-save-system-campaign-persistence] - 2026-10-22
*Milestone D: Local Save System & Campaign Persistence (EXP-009).*

### Added
- **Campaign Persistence Architecture (`GameSaveManager.cs`):**
  - Robust JSON serialization storing career bank savings, unlocked levels, best star ratings (1–3 stars), highest daily revenue, total dishes delivered, and Night Market upgrade masks.
  - Stored safely in `Application.persistentDataPath/dishaster_savegame.json` with atomic temporary file swapping to prevent save corruption.
  - Automatic campaign progression rules:
    - Completing Level 0 (The Sunny Side Diner) with $\ge 1$ star unlocks Level 1 (Downtown Burger Bar).
    - Completing Level 1 with $\ge 2$ stars unlocks Level 2 (Boardwalk Grill).
  - Listens to `DayLoopManager.OnAnyDaySummaryCalculated` to automatically bank profits and log shift stats.
  - Listens to `NightMarketManager.OnAnyUpgradePurchased` to persist unlocked kitchen equipment.
  - Automatically restores saved Night Market upgrades and career bank balance upon starting active sessions.
  - Added `GameSaveManager.ResetStaticData()` wired to `ResetStaticDataManager.cs`.
  - Added `GameSaveManager` scene object in `GameSceneTableService.unity`.
  - Verified in Unity 6 Editor with 0 errors and 0 warnings.


## [0.24.0-customer-reviews-yelp] - 2026-10-22
*Milestone D: Customer Review Cards System ("Yelp for Dishaster").*

### Added
- **Dynamic Customer Review Engine (`CustomerReviewManager.cs`):**
  - Generates 3 contextual Yelp-style review cards at the conclusion of every day based on player performance metrics.
  - Generates humorous user handles (`@BurgerLover99`, `@SpeedyScooterFan`, `@KarenReviews`, `@ChaosEnthusiast`).
  - Evaluates dishes delivered, rating stars, crisis occurrences, and food warmth to output customized customer quotes (e.g. praising sizzling deliveries vs. comedic commentary on kitchen fires and water leaks).
- **Day Summary UI Integration (`DaySummaryUI.cs`):**
  - Added review quotes and star formatting to the end-of-day statement screen.
  - Fallback support gracefully surfacing the headline review in `subtitleText` without requiring inspector canvas rebinding.
  - Verified in Unity 6 Play Mode with 0 errors and 0 warnings.

## [0.23.0-solo-chef-hot-swap] - 2026-10-22
*Milestone D: Solo Mode Dual-Chef Hot-Swapping System.*

### Added
- **Dual-Chef Solo Assist (`SoloChefHotSwapManager.cs` & `SoloChefHotSwapUI.cs`):**
  - Solves single-player pacing bottleneck by auto-spawning Chef B (yellow visual identity) at the Takeout Window in 1-player sessions.
  - Added `OnSwapChefAction` to `GameInput.cs` triggered via `[Tab]`, `[Q]`, or Gamepad North (`[Y]`/`[Triangle]`).
  - Seamlessly hot-swaps active control between Chef A (Kitchen) and Chef B (Front-of-House / Courier).
  - Automatically retargets `Player.LocalInstance` and `CameraFollowPlayer` cinemachine target to the active chef.
  - Inactive chef safely stays stationary in idle pose and retains active counter engagements.
  - Designed `SoloChefHotSwapUI` HUD pill showing active chef role with color-coded prompts.
  - `SoloChefHotSwapManager.ResetStaticData()` wired to `ResetStaticDataManager`.
  - Verified in Unity 6 Play Mode with 0 errors and 0 warnings.

## [0.22.0-thermal-freshness-compass] - 2026-10-22
*Milestone D: Thermal Food Freshness Decay & 3D In-World Delivery Compass.*

### Added
- **Thermal Freshness Economy (`DeliveryBagKitchenObject.cs` & `DeliveryCustomerManager.cs`):**
  - Added timestamp tracking for every dish placed inside the insulated delivery bag.
  - Implemented 90-second linear thermal decay curve (expanded to 150 seconds with Night Market HeavyDutyDeliveryBag upgrade).
  - Wired freshness rating evaluation upon sidewalk customer hand-off:
    - **HOT & FRESH ($\ge 65\%$):** Grants +$15 customer tip bonus and 5-star customer review rating.
    - **WARM ($30\% - 65\%$):** Standard delivery payout.
    - **STONE COLD ($< 30\%$):** Applies -$15 tip penalty and lowers star rating.
  - Updated `DeliveryBagUI.cs` with live thermal color badges (`HOT`, `WARM`, `COLD`) and per-dish warmth percentages.
- **3D City Delivery Navigation Compass (`DeliveryTarget3DIndicator.cs`):**
  - Generates a procedural emissive neon chevron arrow floating 2.4m above the player with gentle bobbing.
  - Dynamically activates when carrying food deliveries or mounted on the scooter.
  - Horizontally rotates to point directly at the closest active city customer.
  - Displays real-time billboarded distance readouts (e.g., `45m`) switching from cyan to emerald green within 8 meters.
  - Auto-instantiated in `DeliveryCustomerManager.Awake()` and wired to `ResetStaticDataManager`.
  - Verified in Unity 6 Play Mode with 0 errors and 0 warnings.

## [0.21.0-french-fries-combos] - 2026-10-22
*Milestone D: French Fries, Fountain Drinks & Deluxe Combo Meals.*

### Added
- **Culinary Expansion & Recipe Combos:**
  - Integrated `Potato` and `PotatoSlices` cutting workflow on `CuttingCounter` (4 cuts).
  - Wired `DeepFryerCounter` to fry `PotatoSlices` into crispy golden `FrenchFries`.
  - Wired `BeverageDispenserCounter` to dispense chilled `SodaCup` fountain drinks.
  - Added procedural 3D plate side visuals in `PlateCompleteVisual.cs` rendering stylized red French Fry cartons with golden potato sticks and blue soda fountain cups with straws.
  - Configured `DeluxeComboMeal` recipe (Burger + French Fries + Soda) awarding **+$30 bonus tip** and **+25s shift timer extension** across both `CustomerTable` and `DeliveryCustomerManager`.
  - Added `AddShiftBonusTimeServerRpc` in `DayLoopManager.cs`.
  - Placed `ContainerCounter_Potato` in `GameSceneTableService.unity`.
  - Registered all new items in `_KitchenObjectListSO`, `_RecipeListSO`, and `DefaultNetworkPrefabs`.
  - Verified in Unity 6 Play Mode with 0 errors and 0 warnings.

## [0.20.0-input-lifecycle-approval-fixes] - 2026-10-22
*Milestone A: PlayerInputActions Lifecycle Leak Fix, EventSystem InputSystemUI Upgrade, & Host Connection Approval.*

### Fixed
- **PlayerInputActions Lifecycle Leak Assertion (`GameInput.cs`):**
  - Added explicit `playerInputActions.Player.Disable()` call prior to `playerInputActions.Dispose()` in `GameInput.OnDestroy()`.
  - Resolves destructor assertion: `"This will cause a leak and performance issues, PlayerInputActions.Player.Disable() has not been called."`
- **Legacy StandaloneInputModule in LoadingScene (`Assets/Scenes/LoadingScene.unity`):**
  - Upgraded EventSystem in `LoadingScene.unity` from legacy `StandaloneInputModule` to `InputSystemUIInputModule`.
  - Eliminates `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class, but you have switched active Input handling to Input System package in Player Settings` when transitioning between menus and lobbies.
  - Audited all 6 project scenes; verified 100% of scenes now use `InputSystemUIInputModule`.
- **Host Connection Approval & Netcode Rejection Warning (`KitchenGameMultiplayer.cs`):**
  - Updated `NetworkManager_ConnectionApprovalCallback` to explicitly approve host/server client connections (`ClientNetworkId == LocalClientId || ClientNetworkId == ServerClientId`).
  - Added `LobbyScene` as a recognized joinable scene alongside `CharacterSelectScene`.
  - Resolves NGO warning: `[Netcode] [HostServerInitialize] You cannot decline the host connection. The connection was automatically approved.`

## [0.19.0-counter-visuals-input-fixes] - 2026-10-22
*Milestone A: Input System Legacy Exceptions, Permanent Counter Highlighting, Distinct UIs, & Appliance 3D Models.*

### Fixed
- **Input System Package Compatibility (`PortableFireExtinguisher.cs`, `WaterLeakValveCounter.cs`, `InteractKeyPromptUI.cs`, `PerformanceTelemetryManager.cs`):**
  - Removed all legacy `UnityEngine.Input.GetKey(KeyCode)` calls that threw `InvalidOperationException` with Unity's new Input System package active.
  - Replaced with `GameInput.Instance.IsInteractAlternatePressed()`, `GameInput.Instance.IsInteractPressed()`, and `UnityEngine.InputSystem.Keyboard.current` queries.
- **Permanent Highlight Bug on Counters (`SelectedCounterVisual.cs`):**
  - Fixed false-positive `e.selectedCounter == baseCounter` comparison when both were `null`, which permanently highlighted counters whenever the player looked away.
  - Implemented automatic parent `BaseCounter` resolution in `Awake()`.
  - Added explicit `baseCounter != null && e.selectedCounter == baseCounter` guard and guaranteed all visual children start inactive.

### Added & Enhanced
- **Distinct Thematic World-Space UIs for Kitchen Stations:**
  - **Sink Counter UI (`SinkCounterUI.cs`):** Dedicated aqua cyan (`#06B6D4`) UI with `"SCRUBBING DISHES"`, progress fill gauge, and `"SPARKLING CLEAN!"` emerald completion flash.
  - **Deep Fryer UI (`DeepFryerCounterUI.cs`):** Dedicated golden amber (`#F59E0B`) UI with `"DEEP FRYING..."`, `"CRISPY & READY!"` green status, and pulsing danger-red `"BURNING OIL!"` warning.
  - **Beverage Dispenser UI (`BeverageDispenserCounterUI.cs`):** Dedicated cold soda violet/cyan (`#8B5CF6`) UI with `"DISPENSING SODA..."` and `"CUP FULL!"` ready status.
- **Stylized Low-Poly Counter Appliance 3D Models:**
  - Constructed stylized 3D appliances matching standard `Kitchen Counter` dimensions, scale `(1, 1, 1)`, and heights (`CounterTopPoint = (0, 1.3, 0)`):
    - *Sink:* Stainless steel drop-in basin, curved chrome gooseneck faucet spout, hot/cold tap knobs, and drain plug.
    - *Deep Fryer:* Stainless steel dual-vat housing, golden sizzling oil bath, twin wire fryer baskets with black heat-resistant handles, and front dial knobs.
    - *Beverage Dispenser:* Commercial stainless soda fountain tower, branded logo marquee, 3 dispenser spouts with push-levers (Cola, Lime, Orange), and drip tray grill.
  - Integrated appliances seamlessly into `SelectedCounterVisual` so the entire appliance glows with `CounterSelected` material when approached by players.

## [0.18.0-table-customer-fixes] - 2026-10-22
*Milestone A: Dining Table Highlighting, Order Counter Timers, Delivery Failure, & Chair Seating Stabilization.*

### Fixed & Enhanced
- **Accurate Chair Seating Kinematics (`DineInCustomerController.cs`):**
  - Resolved designated table stools (`EggStool_{N}_Left` and `EggStool_{N}_Right`) instead of hardcoding arbitrary offsets into dining aisles.
  - Customers now navigate directly onto the chair cushion, rotate to face the table, and apply an 85° seated hip bend with resting arms on the tabletop.
  - Customers stand back up to a walking posture and unseat from the table when leaving after meals or upon delivery failure.
- **Order UI & Patience Timer Gating (`CustomerTable.cs` & `TableOrderUI.cs`):**
  - Added synchronized `isCustomerSeated` NetworkVariable on `CustomerTable`.
  - Order card billboards and patience countdown timers remain strictly hidden while the customer is walking from the entrance.
  - Order UI only reveals and starts the patience timer once the customer is firmly seated at their chair.
- **Distance & Frustum Culling for Table Highlighting (`TableOrderUI.cs`):**
  - Deactivated `orderPanel`, `deliveryResultPanel`, and `TableId` badges by default in `Awake()` and `Start()`.
  - Implemented distance-based (25m) and customer-seated visibility guards in `LateUpdate()`, preventing table UI badges from floating in empty space or rendering through restaurant exterior walls when far away.
- **Delivery Failure & Timeout Cleanup (`CustomerTable.cs` & `TableOrderUI.cs`):**
  - On delivery failure (expired order or wrong dish delivered), `ClearOrder()` now immediately calls `HideOrder()`, shutting down the timer bar (`timerBar.fillAmount = 0f`) and closing the order panel instantly.
  - Deducts wrong delivery fine according to difficulty settings via `RestaurantEconomyManager.ProcessWrongDeliveryPenalty(fine)`.
  - Dissatisfied customers immediately stand up and exit towards the restaurant entrance.

## [0.17.0-network-sync-audit] - 2026-10-22
*Milestone C: Multi-Client Network Synchronization & Session Audit.*

### Added
- **Multi-Client Network Synchronization Test Runner (`MultiClientNetworkAuditRunner.cs`):**
  - Production-grade automated network test harness simulating 4-player multiplayer sessions across consecutive in-game days.
  - 7 integrated test suites verified live in Unity 6 Play Mode:
    1. *Day Loop State Machine:* Validated all 8 day phases and shift pacing transitions.
    2. *Restaurant Economy:* Verified balance deductions, tipping rewards, and rent billing.
    3. *Night Market Upgrades:* Verified bitmask serialization across all 6 equipment upgrades.
    4. *Table Service Registry:* Verified table order creation, customer dining, and dirty dish drops.
    5. *Co-op Communication Channel:* Verified ping RPCs, 3D world beacons, and audio queues.
    6. *Steamworks Platform Hooks:* Verified Rich Presence and achievement triggers.
    7. *Performance & Memory Telemetry:* Confirmed stable managed heap allocations and zero memory leaks.
  - `MultiClientNetworkAuditRunner.ResetStaticData()` wired to `ResetStaticDataManager`.

## [0.16.0-multi-level-progression] - 2026-10-22
*Milestone D: Multi-Level Level Progression Architecture.*

### Added
- **Multi-Level Architecture (`LevelProfileSO.cs` & `LevelSelectionManager.cs`):**
  - Implemented modular level profile system configuring distinct restaurant environments, rent rates, courier distance scaling, and patience curves.
  - Three distinct level profiles created and validated:
    1. *Level 1: The Sunny Side Diner:* Starting suburban diner ($150 base rent, 1.0x courier distance, $600 target revenue).
    2. *Level 2: Downtown Burger Bar:* High-volume urban joint ($250 base rent, 1.4x courier distance across avenues, $1,000 target revenue).
    3. *Level 3: Boardwalk Grill:* Expansive beachside restaurant ($400 base rent, 1.8x courier distance, combo meals, $1,600 target revenue).
  - Server-authoritative level selection synced via Netcode `NetworkVariable<int>`.
  - `LevelSelectionManager.ResetStaticData()` wired to `ResetStaticDataManager`.

## [0.15.0-low-poly-avatar] - 2026-10-22
*Milestone B: Stylized Low-Poly Avatar Rigging & Kinematics.*

### Added
- **Low-Poly Chef Humanoid Rig (`PlayerAvatarVisual.cs`):**
  - Upgraded player character visual representation from basic prototype spheres to a stylized low-poly chef avatar.
  - Multi-part rig with chef toque (toque hat with dynamic secondary physics sway), tailored chef jacket, dark slate apron, articulated shoulders/arms, and articulated legs/shoes.
  - Procedural walk kinematics featuring alternating leg strides, arm counter-swings, and sinusoidal vertical torso bounce.
  - Dynamic carrying pose: when carrying food plates, frying pans, or delivery bags, arms smoothly pivot up into an ergonomic holding stance at chest height.
  - Vehicle mounting kinematics: when hopping onto the scooter, legs tuck comfortably into driving posture and hands grip handlebars.
  - Automatic multiplayer team color tinting based on player ID.
  - `PlayerAvatarVisual.ResetStaticData()` wired to `ResetStaticDataManager`.

## [0.14.0-pass-through-window] - 2026-10-22
*Milestone B: Kitchen-to-Diner Pass-Through Service Window.*

### Added
- **Pass-Through Window Counter (`PassThroughWindowCounter.cs` & `PassThroughWindowVisual.cs`):**
  - Dedicated service pass counter connecting kitchen cooking lines directly to the dining room and courier pickup station.
  - Server-authoritative dual-sided access allowing kitchen staff to place plated meals and front-of-house runners/couriers to grab them from the opposite side.
  - Interactive heat-lamp warming system to keep prepared dishes warm before pickup.
  - Audible 'Order Up!' bell ding and visual prompt indicator upon food delivery to the shelf.
  - Full delivery bag packing support (plating directly into bags or bagging from the shelf).
  - Static event `OnAnyPassOrderPlaced` enabling co-op notifications.
  - `PassThroughWindowCounter.ResetStaticData()` wired to `ResetStaticDataManager`.

## [0.13.0-legal-credits] - 2026-10-22
*Milestone E: Legal & Third-Party License Compliance Audit.*

### Added
- **Commercial Licensing Audit (`CREDITS.md`):**
  - Audited all 57 engine dependencies and package manifests for commercial distribution readiness.
  - Documented compliance registries for Unity 6 (URP 17.5.0, NGO 2.13.1, Input System 1.20.0, Relay/Lobby, Cinemachine 2.10.7).
  - Clarified licensing for MCP for Unity (MIT), Steamworks SDK (Valve Dev Agreement), Pandazole City Town Pack (Unity Asset Store Commercial EULA), Liberation Sans (OFL 1.1), and audio assets.
  - Certified zero unlicensed dependencies.

## [0.12.0-perf-telemetry] - 2026-10-22
*Milestone E: Performance Profiling, Frame Pacing & Telemetry.*

### Added
- **Performance Telemetry Architecture (`PerformanceTelemetryManager.cs`):**
  - Targets 120 FPS on Desktop/PC and 60 FPS on Mobile devices (`QualitySettings.vSyncCount = 0`, platform-aware `targetFrameRate`).
  - Rolling 120-frame ring buffer tracking average FPS, frame times (ms), and 1% Low FPS.
  - Managed heap memory tracking via `GC.GetTotalMemory(false)`.
  - In-engine performance telemetry HUD overlay toggleable with `F3` key, displaying real-time FPS, 1% Lows, Frame time, and Allocated GC memory.
  - `PerformanceTelemetryManager.ResetStaticData()` wired to `ResetStaticDataManager`.

## [0.11.0-steamworks-platform] - 2026-10-22
*Milestone E: Steamworks Platform Integration, Rich Presence & Achievements.*

### Added
- **Steam Platform Architecture (`PlatformSteamManager.cs`):**
  - Production-ready platform manager handling Steam Rich Presence and Steam Achievement hooks.
  - Rich Presence dynamically updates with shift state changes (e.g. "Day 3 - Shift 2 Lunch Rush").
  - 5 core Steam achievements implemented with gameplay event listeners:
    - `ACH_FIRST_DELIVERY` ("Order Up!"): Delivered first meal to a dining table.
    - `ACH_NIGHT_MARKET` ("Upgraded Kitchen"): Purchased first commercial upgrade.
    - `ACH_MASTER_DISHWASHER` ("Sparkling Clean"): Scrubbed 5 dirty plates clean.
    - `ACH_TEAM_PLAYER` ("Chef Communication"): Coordinated with teammates via tactical ping wheel.
    - `ACH_DAY_SURVIVOR` ("Seasoned Restaurateur"): Reached Day 3 without going bankrupt.
  - `PlatformSteamManager.ResetStaticData()` wired to `ResetStaticDataManager`.
- **Steam Achievement Notification Toast (`AchievementToastUI.cs`):**
  - Authentic Steam-styled overlay toast in the bottom right corner with gold trophy badge, Cyan header, bold title, description, and celebratory audio chime.

## [0.10.0-dine-in-customers] - 2026-10-22
*Milestone D: 3D Animated Dine-In Customer Agents.*

### Added
- **3D Animated Dine-In Customers (`DineInCustomerController.cs`):**
  - Full procedural locomotion gait (alternating leg strides, arm counter-swings, vertical pelvis bob).
  - State machine: `WalkingToTable` $\rightarrow$ `Sitting` (80° forward hip bend, resting arms on table) $\rightarrow$ `Eating` (head bobbing and gestures) $\rightarrow$ `Leaving` (standing up and walking out).
  - Server-authoritative client spawning via `TableManager.SpawnDineInCustomerClientRpc`.
  - Self-generating stylized low-poly avatar with randomized outfit color palettes (sky blue, coral, emerald, lavender, mustard) and clean non-colliding physics.
  - Event listeners on `CustomerTable.OnAnyTableDeliverySuccess` and `OnAnyTableDeliveryFailed` for seamless eating and departure loops.
  - `DineInCustomerController.ResetStaticData()` wired to `ResetStaticDataManager`.

## [0.9.0-fryer-beverage] - 2026-10-22
*Milestone D: Deep Fryer Station & Beverage Fountain Dispenser.*

### Added
- **Deep Fryer Station (`DeepFryerCounter.cs` & `DeepFryerCounterVisual.cs`):**
  - Server-authoritative cooking station supporting fast-food frying (French Fries, Crispy Patties).
  - Oil sizzling audio loop, progress bar feedback (`IHasProgress`), and state transitions (`Idle`, `Frying`, `Fried`, `Burned`).
  - Supports Night Market `SpeedyStove` cooking speed boost (+30%) and pauses automatically during blackout crises.
  - Plating integration: chefs can transfer fried foods directly onto a plate with Alternate Interact (`F`).
  - `DeepFryerCounter.ResetStaticData()` wired to `ResetStaticDataManager`.
- **Beverage Fountain Dispenser (`BeverageDispenserCounter.cs` & `BeverageDispenserVisual.cs`):**
  - Server-authoritative cold drink dispensing station with pouring stream visual and audio feedback.
  - Fill progress bar (`IHasProgress`) over 2.0s duration.
  - Meal combo integration: drinks can be carried, placed on counters, packed into delivery bags, or served with meals.
  - `BeverageDispenserCounter.ResetStaticData()` wired to `ResetStaticDataManager`.

## [0.8.0-input-glyphs] - 2026-10-22
*Milestone E: Gamepad Glyph Auto-Detection & Input Customization.*

### Added
- **Device-Aware Gamepad Auto-Detection (`GameInput.cs`):**
  - Live listener for active input devices distinguishing between Keyboard/Mouse, Xbox / XInput, PlayStation (DualShock/DualSense), Steam Deck, and Touchscreen.
  - Fires static `OnActiveDeviceChanged` event for real-time prompt updating across all menus and in-world HUDs.
  - Wired `GameInput.ResetStaticData()` to `ResetStaticDataManager` for clean scene resets without memory leaks.
- **Contextual Gamepad Glyphs & Options UI Refresh (`OptionsUI.cs` & `GameInput.cs`):**
  - Dynamically formats controller button labels matching platform-specific glyphs (e.g. PlayStation: `[X] Cross`, `[Sqr] Square`, `[Options]`; Xbox/Steam Deck: `[A]`, `[X]`, `[Menu]`).
  - Options UI binds directly to device changes, seamlessly switching prompts when switching between controller and keyboard.

## [0.7.0-vehicle-physics] - 2026-10-22
*Milestone D: Scooter 2.0 Physics Polish & Crash Feedback.*

### Added
- **Dynamic Corner Banking & Lean Physics (`ElectricScooter.cs`):**
  - Added visual roll kinematics calculating signed steering angles into turns.
  - Smoothly leans scooter frame and rider up to 18 degrees into corners with realistic banking.
  - Returns upright automatically when traveling straight or dismounting.
- **Collision Feedback & Knockback Recoil (`ElectricScooter.cs`):**
  - Detects high-speed impacts against obstacles or walls.
  - Applies instant backward recoil bounce impulse with smooth exponential decay.
  - Server-authoritative crash audio broadcasting (`SoundManager.PlayCrashSound`).
- **Crash Penalty & Economy Impact (`RestaurantEconomyManager.cs`):**
  - High-speed collisions jolt the courier and penalize the restaurant with a $10 food damage fee.
  - Deducts penalty from daily earnings and logs the event in `DayLoopManager`.

## [0.6.0-balancing] - 2026-10-22
*Milestone C: Pacing, Distance Balancing & Dynamic Shift Audio Transitions.*

### Added
- **Dynamic Shift Audio System (`MusicManager.cs`):**
  - Subscribed to `DayLoopManager.OnAnyDayPhaseChanged` to dynamically transition background music pitch and volume curves across shift phases:
    - *Morning Prep:* Relaxed tempo (0.95x pitch, 0.85x volume).
    - *Lunch Rush:* Upbeat surge (1.05x pitch, 1.05x volume).
    - *Afternoon:* Steady working groove (1.00x pitch, 0.95x volume).
    - *Dinner Rush:* Adrenaline rush (1.15x pitch, 1.15x volume).
    - *Closing Time:* Calming wrap-down (0.90x pitch, 0.80x volume).
    - *Night Market & Summary:* Lounge vibe (0.92x pitch, 0.85x volume).
  - Wired `KitchenCrisisManager.OnFireSpawned` to spike urgency immediately to 1.18x pitch and 1.15x volume with smooth 0.8s s-curve recovery.
- **Dynamic Distance-Based Courier Pacing (`DeliveryCustomerManager.cs`):**
  - Scaled delivery target waypoint selection dynamically per shift: Morning (18-55m), Lunch (25-85m), Afternoon (20-70m), Dinner (30-120m).
  - Added distance tip bonus: couriers earn a bonus $1 per 5 meters distance from the restaurant, rewarding long-range city deliveries.

## [0.5.0-vertical-slice] - 2026-10-22
*Milestone B: Finite Plate Economy & Dishwashing Station.*

### Added
- **Dirty Plate Lifecycle (`DirtyPlateKitchenObject.cs`):**
  - Created `DirtyPlateKitchenObject` component, prefab (`DirtyPlate.prefab`), and ScriptableObject (`DirtyPlate.asset`), registered in `DefaultNetworkPrefabs` and `_KitchenObjectListSO`.
  - Customers finish their meals and leave dirty dishes on `CustomerTable` for bussers to collect.
  - Packing food into `DeliveryBagKitchenObject` transfers the meal and leaves behind dirty plates in the kitchen for recycling.
- **Dishwashing Sink Counter (`SinkCounter.cs` & `SinkCounterVisual.cs`):**
  - Server-authoritative scrubbing station implementing `IHasProgress` with floating `ProgressBarUI`.
  - Players scrub dirty plates clean via Alternate Interact (`F`), transforming soiled dishes into sparkling clean `PlateKitchenObject`s.
  - Water splash particle feedback and clean completion sound events wired.
  - `SinkCounter.ResetStaticData()` registered in `ResetStaticDataManager` for clean scene reload safety.
- **Finite Plate Economy & Rack Restock (`PlatesCounter.cs`):**
  - Plates no longer spawn infinitely out of thin air. The rack begins with 5 plates.
  - Players can return empty clean plates to the `PlatesCounter` stack to replenish available dishes for the chefs.
  - At the start of each morning shift (`Shift1_MorningPrep`), the plate rack is automatically restocked to full capacity.
- **Tactical Co-op Ping & Emote Wheel (`CoopPingManager.cs`, `CoopPingWheelUI.cs`, `WorldPingIndicator.cs`):**
  - Hold `Q` (Keyboard) or D-Pad Up (Gamepad) to summon a sleek radial communication wheel.
  - 6 rapid callout presets: `[ORDER UP]`, `[COURIER]`, `[THANKS!]`, `[HELP!]`, `[PLATES]`, and `[FIRE!]`.
  - Spawns billboarded 3D world beacons at ping coordinates (counter/table/scooter context) that dynamically face the camera and bob in the air.
  - Audio alert cues triggered via `SoundManager` and top-left HUD toast notifications styled with sender player name and team color.
  - Dedicated mobile touch button added to `MobileTouchControlsUI`.
  - `CoopPingManager.ResetStaticData()` wired to `ResetStaticDataManager`.

## [0.4.0-vertical-slice] - 2026-10-21


*Milestone B: Day Loop Architecture & Night Market Upgrade Shop.*

### Added
- **Day Loop State Machine (`DayLoopManager.cs`):** Server-authoritative system driving Day Start $\rightarrow$ 4 Shifts (Morning Prep, Lunch Rush, Afternoon Orders, Dinner Rush) $\rightarrow$ Closing Time $\rightarrow$ Day Summary $\rightarrow$ Night Market $\rightarrow$ Day N+1.
- **Dynamic Rent & Grace Period:** Daily rent formula $150 \times 1.20^{\text{day}-1}$, calculated and billed once per day at closing with a 2-strike debt bankruptcy threshold.
- **Night Market Equipment Shop (`NightMarketManager.cs`):** Server-authoritative catalog supporting 6 game-changing equipment upgrades:
  1. *Turbo Electric Scooter:* +35% top speed and faster acceleration.
  2. *Auto-Chop Knife:* 2x chopping speed on all cutting counters.
  3. *Kitchen Sprinklers:* Auto-extinguishes grease fires within 5 seconds.
  4. *Heavy-Duty Thermal Bag:* Expands bag capacity from 3 to 5 meals.
  5. *Non-Slip Chef Shoes:* Full immunity to water leak slowdown and slips.
  6. *Turbo Induction Stove:* Patties and ingredients cook 30% faster.
- **HUD & Shop UIs:**
  - `DayLoopHUDUI.cs`: Top banner with day number, current shift name, and remaining shift countdown progress bar.
  - `DaySummaryUI.cs`: End-of-day financial breakdown (Revenue, Tips, Rent, Fines, Net Profit, Dishes Served, 1-3 Stars rating, and Debt Warning).
  - `NightMarketShopUI.cs`: Full-screen equipment shop overlay with live balance tracking, purchase buttons, and "Start Next Day" action.

### Changed
- **Pacing & Order Spawning:** Synced `TableManager.cs` and `DeliveryCustomerManager.cs` to `DayLoopManager.CanAcceptNewOrders()`. Spawning pauses automatically during Closing Time and after-hours.
- **Gameplay Upgrades Wired:** Upgrades dynamically modulate `ElectricScooter.cs`, `CuttingCounter.cs`, `StoveCounter.cs`, `DeliveryBagKitchenObject.cs`, `KitchenCrisisManager.cs`, and `WaterLeakHazard.cs`.

## [0.3.0-stabilization] - 2026-10-21
*Milestone A: Architecture Stabilization, Technical Debt Paydown & Netcode Modernization.*

### Added
- **MCP Unity Bridge Integration:** Fully connected `com.coplaydev.unity-mcp` v10.3.0 to Antigravity IDE and live Editor over port 8080.
- **Production Governance Framework:** Added [`AGENTS.md`](file:///c:/Users/marwe/Unity%20Apps/KitchenChaos_Multiplayer/AGENTS.md) studio rules, [`DOCS/PRODUCTION_TRACKER.md`](file:///c:/Users/marwe/Unity%20Apps/KitchenChaos_Multiplayer/DOCS/PRODUCTION_TRACKER.md) sprint board, and [`DOCS/ENGINEERING_STANDARDS.md`](file:///c:/Users/marwe/Unity%20Apps/KitchenChaos_Multiplayer/DOCS/ENGINEERING_STANDARDS.md).
- **Legal Attribution:** Added [`CREDITS.md`](file:///c:/Users/marwe/Unity%20Apps/KitchenChaos_Multiplayer/CREDITS.md) tracking third-party licenses and asset store compliance.
- **Static Event Reset Lifecycle:** Implemented `ResetStaticData()` for all crisis & interaction tools (`FuseBoxCounter`, `PipeWrenchCounter`, `PortablePipeWrench`, `FireExtinguisherCounter`, `PortableFireExtinguisher`, `Player.OnAnyPickedSomething`) registered in `ResetStaticDataManager`.
- **Sustainable Economy & Grace Period:** Replaced $1.35\times$ exponential rent growth with 1.15f scaling, 90s billing intervals, and a 1-strike bankruptcy grace period warning in `RestaurantEconomyManager` & HUD UI.

### Changed
- **Unity 6 Netcode RPC Modernization:** Migrated 100% of deprecated `[ServerRpc(RequireOwnership = false)]` and `[ClientRpc]` attributes across 12 scripts to Unity 6 `[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]` and `[Rpc(SendTo.ClientsAndHost)]`.
- **Server-Authoritative Deliveries:** Fixed delivery bag plate insertion and customer deliveries to pass `NetworkObjectReference` with server-side despawning, eliminating client-side unilateral destructions.
- **Multiplayer Pause Behavior:** Opening the pause menu in multiplayer now only activates the local overlay and preserves `Time.timeScale = 1.0f` to prevent freezing connected players.

### Fixed & Removed
- Removed stale folder structures (`Assets/Scripts/Assets/`) and relocated misplaced `TableOrderUI.cs`.
- Deleted legacy `.backup` files in `Assets/BACKUP_DeliverySystem/`.
- Resolved all Unity 6 CS0618 deprecation warnings (`FindFirstObjectByType`, `FindObjectsSortMode`) and CS0108/CS0114 member hiding warnings, achieving 0 errors and 0 warnings.

---

## [0.2.0-poc] - 2026-10-20
*Working Prototype of City Express & Table Service Mode.*

### Added
- Front-of-House `CustomerTable` and `TableManager` service prototype.
- Outdoor procedural city generation with `CityGenerator`, cars, and pedestrians.
- Sidewalk delivery counter with 3-plate thermal `DeliveryBagKitchenObject`.
- Drivable `ElectricScooter` vehicle with network mount/unmount and speed booster.
- Kitchen Crises: Fuse box blackout, pipe water puddle leaks, and stove fires.
- Dynamic mobile touch controls (virtual joystick + action buttons).

---

## [0.1.0-alpha] - 2026-09-01
*Core Kitchen Chaos Foundation.*

### Added
- Base counters: Clear, Container, Cutting, Plates, Stove, and Trash counters.
- Core food combinations (Burgers, Mega Burgers, Salads).
- Netcode for GameObjects multiplayer lobby and Relay connection.
