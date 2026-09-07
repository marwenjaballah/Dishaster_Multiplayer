using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace CityLife.Editor
{
    public static class CityLifeBuilderEditor
    {
        private const string PrefabFolder = "Assets/CityLife/Prefabs";
        private const string MaterialFolder = "Assets/CityLife/Materials";

        [MenuItem("Tools/CityLife/Full Setup (Build Prefabs & Wire Scene)", false, 1)]
        public static void FullSetup()
        {
            BuildPrefabsAndMaterials();
            WireCurrentScene(generateCity: true);
            Debug.Log("<color=green><b>[CityLife] Full Setup Complete!</b></color> Prefabs created, scene wired with CityGenerator and CityLifeManager, and city generated.");
        }

        [MenuItem("Tools/CityLife/Build Prefabs and Materials", false, 2)]
        public static void BuildPrefabsAndMaterials()
        {
            EnsureFolders();
            var mats = CreateMaterials();
            CreateCarPrefab(mats);
            CreatePedestrianPrefab(mats);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CityLife] Prefabs and Materials created successfully in Assets/CityLife/");
        }

        [MenuItem("Tools/CityLife/Setup Current Scene (or TestWorld)", false, 3)]
        public static void WireTestWorldScene()
        {
            WireCurrentScene(generateCity: false);
        }

        public static void WireCurrentScene(bool generateCity = false)
        {
            // Find CityGenerator in current scene
            var cityGen = Object.FindFirstObjectByType<SevenDays.World.CityGenerator>();
            if (cityGen == null)
            {
                // Fallback to TestWorld if present
                string scenePath = "Assets/_Game/Scenes/World/TestWorld.unity";
                if (File.Exists(scenePath))
                {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    cityGen = Object.FindFirstObjectByType<SevenDays.World.CityGenerator>();
                }
            }

            if (cityGen == null)
            {
                // Create a new City object in the active scene so this works seamlessly in fresh projects
                GameObject go = new GameObject("CitySystem");
                cityGen = go.AddComponent<SevenDays.World.CityGenerator>();
                Undo.RegisterCreatedObjectUndo(go, "Create CitySystem");
                Debug.Log("[CityLife] Created new 'CitySystem' GameObject in scene.");
            }

            // Auto-assign Pandazole / CityElements prefabs to CityGenerator
            SevenDays.World.Editor.CityGeneratorEditor.AutoAssignPrefabs(cityGen);

            var carPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Car.prefab");
            var pedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Pedestrian.prefab");

            if (carPrefab == null || pedPrefab == null)
            {
                BuildPrefabsAndMaterials();
                carPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Car.prefab");
                pedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Pedestrian.prefab");
            }

            var lifeMgr = cityGen.GetComponent<CityLifeManager>();
            if (lifeMgr == null)
            {
                lifeMgr = cityGen.gameObject.AddComponent<CityLifeManager>();
            }

            // Assign prefabs via SerializedObject to ensure persistence
            SerializedObject soMgr = new SerializedObject(lifeMgr);
            soMgr.Update();
            soMgr.FindProperty("carPrefab").objectReferenceValue = carPrefab;
            soMgr.FindProperty("pedestrianPrefab").objectReferenceValue = pedPrefab;
            soMgr.ApplyModifiedProperties();

            // Also wire into CityGenerator
            SerializedObject soGen = new SerializedObject(cityGen);
            soGen.Update();
            var propMgr = soGen.FindProperty("cityLifeManager");
            if (propMgr != null)
            {
                propMgr.objectReferenceValue = lifeMgr;
                soGen.ApplyModifiedProperties();
            }

            if (generateCity)
            {
                cityGen.GenerateCity();
            }

            EditorUtility.SetDirty(cityGen.gameObject);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[CityLife] Scene successfully wired with CityLifeManager, CityGenerator, and prefabs!");
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/CityLife"))
                AssetDatabase.CreateFolder("Assets", "CityLife");
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
                AssetDatabase.CreateFolder("Assets/CityLife", "Materials");
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets/CityLife", "Prefabs");
        }

        private struct MaterialSet
        {
            public Material carBody;
            public Material carGlass;
            public Material carTire;
            public Material carRim;
            public Material carLightFront;
            public Material carLightBack;
            public Material pedSkin;
            public Material pedShirt;
            public Material pedPants;
            public Material pedHair;
        }

        private static MaterialSet CreateMaterials()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material GetOrCreateMat(string name, Color col, float metallic = 0f, float smoothness = 0.5f, Color? emission = null)
            {
                string path = $"{MaterialFolder}/{name}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(shader);
                    mat.color = col;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
                    if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
                    if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
                    if (emission.HasValue)
                    {
                        mat.EnableKeyword("_EMISSION");
                        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emission.Value);
                    }
                    AssetDatabase.CreateAsset(mat, path);
                }
                return mat;
            }

            var set = new MaterialSet
            {
                carBody = GetOrCreateMat("Mat_Car_Body", new Color(0.85f, 0.18f, 0.15f), 0.35f, 0.75f),
                carGlass = GetOrCreateMat("Mat_Car_Glass", new Color(0.1f, 0.15f, 0.22f), 0.1f, 0.95f),
                carTire = GetOrCreateMat("Mat_Car_Tire", new Color(0.12f, 0.12f, 0.12f), 0.0f, 0.2f),
                carRim = GetOrCreateMat("Mat_Car_Rim", new Color(0.78f, 0.8f, 0.82f), 0.85f, 0.8f),
                carLightFront = GetOrCreateMat("Mat_Car_Light_Front", new Color(1f, 0.96f, 0.82f), 0f, 0.9f, new Color(1f, 0.95f, 0.7f) * 2f),
                carLightBack = GetOrCreateMat("Mat_Car_Light_Back", new Color(0.88f, 0.12f, 0.1f), 0f, 0.8f, new Color(1f, 0.05f, 0.05f) * 2f),
                pedSkin = GetOrCreateMat("Mat_Ped_Skin", new Color(0.92f, 0.75f, 0.65f), 0f, 0.2f),
                pedShirt = GetOrCreateMat("Mat_Ped_Shirt", new Color(0.2f, 0.45f, 0.78f), 0f, 0.3f),
                pedPants = GetOrCreateMat("Mat_Ped_Pants", new Color(0.18f, 0.22f, 0.32f), 0f, 0.2f),
                pedHair = GetOrCreateMat("Mat_Ped_Hair", new Color(0.22f, 0.15f, 0.1f), 0f, 0.2f)
            };

            return set;
        }

        private static void CreateCarPrefab(MaterialSet mats)
        {
            GameObject carRoot = new GameObject("Car");
            var carCtrl = carRoot.AddComponent<CarController>();
            var carCol = carRoot.AddComponent<BoxCollider>();
            carCol.center = new Vector3(0f, 0.65f, 0f);
            carCol.size = new Vector3(1.8f, 1.1f, 3.8f);
            var carRb = carRoot.AddComponent<Rigidbody>();
            carRb.isKinematic = true;

            // Lower Chassis (Main Body)
            var chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chassis.name = "Chassis";
            chassis.transform.SetParent(carRoot.transform, false);
            chassis.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            chassis.transform.localScale = new Vector3(1.8f, 0.5f, 3.8f);
            chassis.GetComponent<Renderer>().sharedMaterial = mats.carBody;
            Object.DestroyImmediate(chassis.GetComponent<Collider>());
            carCtrl.bodyRenderer = chassis.GetComponent<Renderer>();

            // Cabin / Windshield
            var cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Cabin";
            cabin.transform.SetParent(carRoot.transform, false);
            cabin.transform.localPosition = new Vector3(0f, 0.95f, -0.2f);
            cabin.transform.localScale = new Vector3(1.5f, 0.55f, 2.0f);
            cabin.GetComponent<Renderer>().sharedMaterial = mats.carGlass;
            Object.DestroyImmediate(cabin.GetComponent<Collider>());

            // Bumpers
            var bFront = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bFront.name = "Bumper_Front";
            bFront.transform.SetParent(carRoot.transform, false);
            bFront.transform.localPosition = new Vector3(0f, 0.32f, 1.9f);
            bFront.transform.localScale = new Vector3(1.7f, 0.25f, 0.18f);
            bFront.GetComponent<Renderer>().sharedMaterial = mats.carTire;
            Object.DestroyImmediate(bFront.GetComponent<Collider>());

            var bRear = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bRear.name = "Bumper_Rear";
            bRear.transform.SetParent(carRoot.transform, false);
            bRear.transform.localPosition = new Vector3(0f, 0.32f, -1.9f);
            bRear.transform.localScale = new Vector3(1.7f, 0.25f, 0.18f);
            bRear.GetComponent<Renderer>().sharedMaterial = mats.carTire;
            Object.DestroyImmediate(bRear.GetComponent<Collider>());

            // Headlights
            var hlL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hlL.name = "Headlight_L";
            hlL.transform.SetParent(carRoot.transform, false);
            hlL.transform.localPosition = new Vector3(-0.6f, 0.5f, 1.91f);
            hlL.transform.localScale = new Vector3(0.35f, 0.18f, 0.05f);
            hlL.GetComponent<Renderer>().sharedMaterial = mats.carLightFront;
            Object.DestroyImmediate(hlL.GetComponent<Collider>());

            var hlR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hlR.name = "Headlight_R";
            hlR.transform.SetParent(carRoot.transform, false);
            hlR.transform.localPosition = new Vector3(0.6f, 0.5f, 1.91f);
            hlR.transform.localScale = new Vector3(0.35f, 0.18f, 0.05f);
            hlR.GetComponent<Renderer>().sharedMaterial = mats.carLightFront;
            Object.DestroyImmediate(hlR.GetComponent<Collider>());

            // Taillights
            var tlL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tlL.name = "Taillight_L";
            tlL.transform.SetParent(carRoot.transform, false);
            tlL.transform.localPosition = new Vector3(-0.6f, 0.55f, -1.91f);
            tlL.transform.localScale = new Vector3(0.35f, 0.18f, 0.05f);
            tlL.GetComponent<Renderer>().sharedMaterial = mats.carLightBack;
            Object.DestroyImmediate(tlL.GetComponent<Collider>());

            var tlR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tlR.name = "Taillight_R";
            tlR.transform.SetParent(carRoot.transform, false);
            tlR.transform.localPosition = new Vector3(0.6f, 0.55f, -1.91f);
            tlR.transform.localScale = new Vector3(0.35f, 0.18f, 0.05f);
            tlR.GetComponent<Renderer>().sharedMaterial = mats.carLightBack;
            Object.DestroyImmediate(tlR.GetComponent<Collider>());

            carCtrl.taillights = new Renderer[] { tlL.GetComponent<Renderer>(), tlR.GetComponent<Renderer>() };

            // Wheels
            Transform MakeWheel(string name, Vector3 pos)
            {
                var wObj = new GameObject(name);
                wObj.transform.SetParent(carRoot.transform, false);
                wObj.transform.localPosition = pos;

                var tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tire.name = "Tire";
                tire.transform.SetParent(wObj.transform, false);
                tire.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                tire.transform.localScale = new Vector3(0.65f, 0.15f, 0.65f);
                tire.GetComponent<Renderer>().sharedMaterial = mats.carTire;
                Object.DestroyImmediate(tire.GetComponent<Collider>());

                var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rim.name = "Rim";
                rim.transform.SetParent(wObj.transform, false);
                rim.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                rim.transform.localScale = new Vector3(0.42f, 0.16f, 0.42f);
                rim.GetComponent<Renderer>().sharedMaterial = mats.carRim;
                Object.DestroyImmediate(rim.GetComponent<Collider>());

                return wObj.transform;
            }

            carCtrl.wheelFL = MakeWheel("Wheel_FL", new Vector3(-0.95f, 0.32f, 1.1f));
            carCtrl.wheelFR = MakeWheel("Wheel_FR", new Vector3(0.95f, 0.32f, 1.1f));
            carCtrl.wheelRL = MakeWheel("Wheel_RL", new Vector3(-0.95f, 0.32f, -1.1f));
            carCtrl.wheelRR = MakeWheel("Wheel_RR", new Vector3(0.95f, 0.32f, -1.1f));

            string path = $"{PrefabFolder}/Car.prefab";
            PrefabUtility.SaveAsPrefabAsset(carRoot, path);
            Object.DestroyImmediate(carRoot);
        }

        private static void CreatePedestrianPrefab(MaterialSet mats)
        {
            GameObject pedRoot = new GameObject("Pedestrian");
            var pedCtrl = pedRoot.AddComponent<PedestrianController>();
            var pedCol = pedRoot.AddComponent<CapsuleCollider>();
            pedCol.center = new Vector3(0f, 0.9f, 0f);
            pedCol.height = 1.8f;
            pedCol.radius = 0.35f;
            var pedRb = pedRoot.AddComponent<Rigidbody>();
            pedRb.isKinematic = true;

            // Torso / Shirt
            var torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "Torso";
            torso.transform.SetParent(pedRoot.transform, false);
            torso.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            torso.transform.localScale = new Vector3(0.45f, 0.55f, 0.26f);
            torso.GetComponent<Renderer>().sharedMaterial = mats.pedShirt;
            Object.DestroyImmediate(torso.GetComponent<Collider>());

            // Head
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(pedRoot.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            head.transform.localScale = new Vector3(0.36f, 0.38f, 0.36f);
            head.GetComponent<Renderer>().sharedMaterial = mats.pedSkin;
            Object.DestroyImmediate(head.GetComponent<Collider>());

            // Hair (parented to Head so it rotates when looking around)
            var hair = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hair.name = "Hair";
            hair.transform.SetParent(head.transform, false);
            hair.transform.localPosition = new Vector3(0f, 0.35f, -0.05f);
            hair.transform.localScale = new Vector3(1.05f, 0.42f, 1.05f);
            hair.GetComponent<Renderer>().sharedMaterial = mats.pedHair;
            Object.DestroyImmediate(hair.GetComponent<Collider>());

            // Hips & Legs
            var hipL = new GameObject("Hip_L");
            hipL.transform.SetParent(pedRoot.transform, false);
            hipL.transform.localPosition = new Vector3(-0.12f, 0.80f, 0f);

            var legL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legL.name = "Leg_L";
            legL.transform.SetParent(hipL.transform, false);
            legL.transform.localPosition = new Vector3(0f, -0.38f, 0f);
            legL.transform.localScale = new Vector3(0.18f, 0.75f, 0.2f);
            legL.GetComponent<Renderer>().sharedMaterial = mats.pedPants;
            Object.DestroyImmediate(legL.GetComponent<Collider>());

            var hipR = new GameObject("Hip_R");
            hipR.transform.SetParent(pedRoot.transform, false);
            hipR.transform.localPosition = new Vector3(0.12f, 0.80f, 0f);

            var legR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legR.name = "Leg_R";
            legR.transform.SetParent(hipR.transform, false);
            legR.transform.localPosition = new Vector3(0f, -0.38f, 0f);
            legR.transform.localScale = new Vector3(0.18f, 0.75f, 0.2f);
            legR.GetComponent<Renderer>().sharedMaterial = mats.pedPants;
            Object.DestroyImmediate(legR.GetComponent<Collider>());

            // Shoulders & Arms
            var shldrL = new GameObject("Shoulder_L");
            shldrL.transform.SetParent(pedRoot.transform, false);
            shldrL.transform.localPosition = new Vector3(-0.31f, 1.25f, 0f);

            var armL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            armL.name = "Arm_L";
            armL.transform.SetParent(shldrL.transform, false);
            armL.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            armL.transform.localScale = new Vector3(0.14f, 0.52f, 0.16f);
            armL.GetComponent<Renderer>().sharedMaterial = mats.pedShirt;
            Object.DestroyImmediate(armL.GetComponent<Collider>());

            var shldrR = new GameObject("Shoulder_R");
            shldrR.transform.SetParent(pedRoot.transform, false);
            shldrR.transform.localPosition = new Vector3(0.31f, 1.25f, 0f);

            var armR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            armR.name = "Arm_R";
            armR.transform.SetParent(shldrR.transform, false);
            armR.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            armR.transform.localScale = new Vector3(0.14f, 0.52f, 0.16f);
            armR.GetComponent<Renderer>().sharedMaterial = mats.pedShirt;
            Object.DestroyImmediate(armR.GetComponent<Collider>());

            // Wire pivots into PedestrianController
            pedCtrl.hipL = hipL.transform;
            pedCtrl.hipR = hipR.transform;
            pedCtrl.shoulderL = shldrL.transform;
            pedCtrl.shoulderR = shldrR.transform;
            pedCtrl.torso = torso.transform;
            pedCtrl.head = head.transform;

            string path = $"{PrefabFolder}/Pedestrian.prefab";
            PrefabUtility.SaveAsPrefabAsset(pedRoot, path);
            Object.DestroyImmediate(pedRoot);
        }
    }
}
