using UnityEngine;
using UnityEditor;

namespace SevenDays.World.Editor
{
    /// <summary>
    /// Custom editor for CityGenerator with convenient buttons and prefab auto-assignment.
    /// </summary>
    [CustomEditor(typeof(CityGenerator))]
    public class CityGeneratorEditor : UnityEditor.Editor
    {
        private CityGenerator generator;

        private void OnEnable()
        {
            generator = (CityGenerator)target;
        }

        public override void OnInspectorGUI()
        {
            // Draw default inspector
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("City Generation", EditorStyles.boldLabel);

            // Generation buttons
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Generate City", GUILayout.Height(40)))
            {
                Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Generate City");
                generator.GenerateCity();
                EditorUtility.SetDirty(generator.gameObject);
            }

            if (GUILayout.Button("Clear City", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Clear City",
                    "Are you sure you want to clear the generated city?",
                    "Yes", "Cancel"))
                {
                    Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Clear City");
                    generator.ClearCity();
                    EditorUtility.SetDirty(generator.gameObject);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Quick actions
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Randomize Seed"))
            {
                Undo.RecordObject(generator, "Randomize Seed");
                SerializedObject so = new SerializedObject(generator);
                so.FindProperty("seed").intValue = Random.Range(0, 999999);
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(generator);
            }

            if (GUILayout.Button("Auto-Assign Prefabs"))
            {
                AutoAssignPrefabs();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Info box
            EditorGUILayout.HelpBox(
                "1. Assign prefabs from Pandazole_Ultimate_Pack\n" +
                "2. Adjust generation settings\n" +
                "3. Click 'Generate City' to create\n" +
                "4. Use 'Randomize Seed' for different layouts\n" +
                "5. Click 'Auto-Assign Prefabs' to automatically find and assign Pandazole assets",
                MessageType.Info);

            // Statistics
            if (Application.isPlaying || HasGeneratedCity())
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Generated City Info", EditorStyles.boldLabel);

                Transform cityRoot = generator.transform.Find("GeneratedCity");
                if (cityRoot != null)
                {
                    int buildingCount = GetChildCount(cityRoot, "Buildings");
                    int roadCount = GetChildCount(cityRoot, "Roads");
                    int propCount = GetChildCount(cityRoot, "Props");

                    EditorGUILayout.LabelField($"Buildings: {buildingCount}");
                    EditorGUILayout.LabelField($"Roads: {roadCount}");
                    EditorGUILayout.LabelField($"Props: {propCount}");
                    EditorGUILayout.LabelField($"Total Objects: {buildingCount + roadCount + propCount}");
                }
            }
        }

        private void AutoAssignPrefabs()
        {
            AutoAssignPrefabs(generator);
        }

        public static string FindPrefabsPath()
        {
            if (AssetDatabase.IsValidFolder("Assets/CityLife/CityElements/Prefabs"))
                return "Assets/CityLife/CityElements/Prefabs";

            string[] guids = AssetDatabase.FindAssets("Env_Road_Straight_01 t:Prefab");
            if (guids.Length > 0)
            {
                string p = AssetDatabase.GUIDToAssetPath(guids[0]);
                return System.IO.Path.GetDirectoryName(p).Replace('\\', '/');
            }

            return "Assets/CityLife/CityElements/Prefabs";
        }

        public static void AutoAssignPrefabs(CityGenerator targetGen)
        {
            if (targetGen == null) return;
            SerializedObject so = new SerializedObject(targetGen);

            string basePath = FindPrefabsPath();

            // Buildings
            AssignPrefabArray(so, "residentialBuildings", basePath, "Env_ResidentBuilding_");
            AssignPrefabArray(so, "commercialBuildings", basePath, "Env_CommercialBuilding_");
            AssignPrefabArray(so, "companyBuildings", basePath, "Env_CompanyBuilding_");
            AssignPrefabArray(so, "motelBuildings", basePath, "Env_Motel_");

            // Roads - find one of each type
            AssignSinglePrefab(so, "roadStraight", basePath, "Env_Road_Straight_01");
            AssignSinglePrefab(so, "roadCorner", basePath, "Env_Road_Cornor_01");
            AssignSinglePrefab(so, "roadCross", basePath, "Env_Road_Cross_01");
            AssignSinglePrefab(so, "roadTJunction", basePath, "Env_Road_Side_03");
            AssignSinglePrefab(so, "roadEnd", basePath, "Env_Road_End_01");

            // Props
            AssignPrefabArray(so, "trees", basePath, "Prop_Tree_");
            AssignPrefabArray(so, "streetSigns", basePath, "Prop_StreetSign_");
            AssignPrefabArray(so, "trashCans", basePath, "Prop_CTPTrashCan_");
            AssignPrefabArray(so, "plants", basePath, "Prop_Plant_");

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(targetGen);

            Debug.Log($"Auto-assigned prefabs to CityGenerator from '{basePath}'!");
        }

        private static void AssignPrefabArray(SerializedObject so, string propertyName, string basePath, string prefix)
        {
            string[] guids = AssetDatabase.FindAssets($"{prefix} t:Prefab", new[] { basePath });

            if (guids.Length > 0)
            {
                SerializedProperty prop = so.FindProperty(propertyName);
                prop.arraySize = guids.Length;

                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = prefab;
                }

                Debug.Log($"Assigned {guids.Length} prefabs to {propertyName}");
            }
            else
            {
                Debug.LogWarning($"No prefabs found for {propertyName} with prefix '{prefix}' in {basePath}");
            }
        }

        private static void AssignSinglePrefab(SerializedObject so, string propertyName, string basePath, string prefabName)
        {
            string[] guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab", new[] { basePath });

            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                SerializedProperty prop = so.FindProperty(propertyName);
                prop.objectReferenceValue = prefab;
                Debug.Log($"Assigned {prefabName} to {propertyName}");
            }
            else
            {
                Debug.LogWarning($"Prefab '{prefabName}' not found in {basePath}");
            }
        }

        private bool HasGeneratedCity()
        {
            return generator.transform.Find("GeneratedCity") != null;
        }

        private int GetChildCount(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            return child != null ? child.childCount : 0;
        }

        // Draw scene view handles
        private void OnSceneGUI()
        {
            // Could add handles for adjusting grid size visually here
        }
    }
}
