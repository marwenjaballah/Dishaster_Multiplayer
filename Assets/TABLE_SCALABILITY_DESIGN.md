# Table System - Scalable Architecture Design

## Table Identification System

### Design Principle
Each table has a **unique ID** (GUID) and a **display number** that can be customized. This allows:
- ✅ Tables to be added/removed without breaking the system
- ✅ Display numbers to be changed (Table 1 could become Table 5)
- ✅ Special table types with different numbering schemes
- ✅ Save/load table configurations
- ✅ Network synchronization by ID instead of array index

### Table Structure

```csharp
[System.Serializable]
public class TableData {
    public string tableId;           // Unique identifier (GUID)
    public int displayNumber;        // What players see (1, 2, 3, etc.)
    public TableType tableType;      // Normal, VIP, Express, etc.
    public Vector3 position;         // For future procedural placement
}

public enum TableType {
    Normal,
    VIP,        // Future: 2x points
    Express,    // Future: faster orders
    Group       // Future: requires multiple dishes
}
```

### Network Synchronization

```csharp
public struct TableOrder : INetworkSerializable {
    public FixedString64Bytes tableId;  // Use GUID string instead of index
    public int recipeSOIndex;
    public float orderStartTime;        // For future urgency system
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref tableId);
        serializer.SerializeValue(ref recipeSOIndex);
        serializer.SerializeValue(ref orderStartTime);
    }
}
```

### Table Registration

```csharp
// TableManager maintains dictionary for fast lookups
private Dictionary<string, CustomerTable> tableRegistry = new Dictionary<string, CustomerTable>();

public void RegisterTable(CustomerTable table) {
    if (!tableRegistry.ContainsKey(table.GetTableId())) {
        tableRegistry.Add(table.GetTableId(), table);
    }
}

public CustomerTable GetTableById(string tableId) {
    return tableRegistry.TryGetValue(tableId, out CustomerTable table) ? table : null;
}

// Get all tables of a specific type
public List<CustomerTable> GetTablesByType(TableType type) {
    return tableRegistry.Values.Where(t => t.GetTableType() == type).ToList();
}
```

### CustomerTable Component

```csharp
public class CustomerTable : BaseCounter {
    [Header("Table Identity")]
    [SerializeField] private string tableId = "";  // Set in inspector or auto-generate
    [SerializeField] private int displayNumber = 1;
    [SerializeField] private TableType tableType = TableType.Normal;
    
    private void Awake() {
        // Auto-generate ID if not set
        if (string.IsNullOrEmpty(tableId)) {
            tableId = System.Guid.NewGuid().ToString();
        }
    }
    
    private void Start() {
        // Register with TableManager
        TableManager.Instance.RegisterTable(this);
    }
    
    public string GetTableId() => tableId;
    public int GetDisplayNumber() => displayNumber;
    public TableType GetTableType() => tableType;
    
    // Can change display number at runtime
    public void SetDisplayNumber(int newNumber) {
        displayNumber = newNumber;
        // Update UI
        if (tableOrderUI != null) {
            tableOrderUI.UpdateTableNumber(displayNumber);
        }
    }
}
```

### Scalability Features

#### 1. Dynamic Table Addition
```csharp
// TableManager can spawn new tables at runtime
public void SpawnTable(Vector3 position, int displayNumber, TableType type) {
    GameObject tablePrefab = GetTablePrefab(type);
    GameObject tableObj = Instantiate(tablePrefab, position, Quaternion.identity);
    
    CustomerTable table = tableObj.GetComponent<CustomerTable>();
    table.SetDisplayNumber(displayNumber);
    // Table auto-registers on Start()
}
```

#### 2. Table Configuration Saving
```csharp
[System.Serializable]
public class TableConfiguration {
    public List<TableData> tables = new List<TableData>();
}

public void SaveTableConfiguration(string configName) {
    TableConfiguration config = new TableConfiguration();
    
    foreach (var table in tableRegistry.Values) {
        config.tables.Add(new TableData {
            tableId = table.GetTableId(),
            displayNumber = table.GetDisplayNumber(),
            tableType = table.GetTableType(),
            position = table.transform.position
        });
    }
    
    string json = JsonUtility.ToJson(config);
    PlayerPrefs.SetString($"TableConfig_{configName}", json);
}
```

#### 3. Table Filtering & Queries
```csharp
// Get available tables (no active order)
public List<CustomerTable> GetAvailableTables() {
    return tableRegistry.Values.Where(t => !t.HasActiveOrder()).ToList();
}

// Get available tables of specific type
public List<CustomerTable> GetAvailableTablesOfType(TableType type) {
    return tableRegistry.Values
        .Where(t => !t.HasActiveOrder() && t.GetTableType() == type)
        .ToList();
}

// Get table count by type
public int GetTableCountByType(TableType type) {
    return tableRegistry.Values.Count(t => t.GetTableType() == type);
}
```

#### 4. Order Assignment with Priorities
```csharp
private void SpawnRecipeToRandomTable() {
    // Can prioritize by table type
    var availableTables = GetAvailableTables();
    
    if (availableTables.Count == 0) return;
    
    // Future: Could implement weighted random based on table type
    CustomerTable selectedTable = availableTables[Random.Range(0, availableTables.Count)];
    int recipeIndex = Random.Range(0, recipeListSO.recipeSOList.Count);
    
    // Assign by ID, not index
    AssignOrderToTableClientRpc(
        new FixedString64Bytes(selectedTable.GetTableId()), 
        recipeIndex
    );
}

[ClientRpc]
private void AssignOrderToTableClientRpc(FixedString64Bytes tableId, int recipeIndex) {
    CustomerTable table = GetTableById(tableId.ToString());
    if (table == null) {
        Debug.LogError($"Table not found: {tableId}");
        return;
    }
    
    RecipeSO recipe = recipeListSO.recipeSOList[recipeIndex];
    table.AssignRecipe(recipe, recipeIndex);
}
```

## Benefits of This Architecture

### Immediate Benefits
- ✅ Easy to add more tables in editor (just drag prefab, assign number)
- ✅ Can reorder table numbers without breaking saves or configs
- ✅ Network sync by ID is more robust than array index
- ✅ Tables can be enabled/disabled at runtime

### Future Extensibility
- ✅ Different table types (VIP, Express, etc.)
- ✅ Procedural table generation
- ✅ Save/load custom kitchen layouts
- ✅ Table unlocking progression system
- ✅ Modding support (add custom tables)
- ✅ Analytics per table type

### Editor Experience
```csharp
// Custom editor to generate IDs automatically
#if UNITY_EDITOR
[CustomEditor(typeof(CustomerTable))]
public class CustomerTableEditor : Editor {
    public override void OnInspectorGUI() {
        CustomerTable table = (CustomerTable)target;
        
        DrawDefaultInspector();
        
        if (GUILayout.Button("Generate New ID")) {
            SerializedProperty idProp = serializedObject.FindProperty("tableId");
            idProp.stringValue = System.Guid.NewGuid().ToString();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
```

## Migration Path

### Phase 1 Implementation
Start with ID-based system from the beginning:
1. Create CustomerTable with tableId + displayNumber
2. TableManager uses dictionary lookups
3. Network sync by ID

### Future Enhancements (Post-Phase 5)
- Add TableType enum and filtering
- Implement save/load system
- Add dynamic spawning
- Add table-specific modifiers

This architecture future-proofs the system without adding complexity to the initial implementation!
