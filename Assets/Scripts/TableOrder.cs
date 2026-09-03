using System;
using Unity.Netcode;
using Unity.Collections;

/// <summary>
/// Network-serializable struct representing an order assigned to a specific table.
/// Uses table ID for scalability instead of array index.
/// </summary>
public struct TableOrder : INetworkSerializable, IEquatable<TableOrder> {
    public FixedString64Bytes tableId;  // Unique table identifier (GUID string)
    public int recipeSOIndex;           // Index into RecipeListSO for network efficiency
    public float orderStartTime;        // Timestamp when order was created (for future urgency system)

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref tableId);
        serializer.SerializeValue(ref recipeSOIndex);
        serializer.SerializeValue(ref orderStartTime);
    }

    // IEquatable implementation required for NetworkList<T>
    public bool Equals(TableOrder other) {
        return tableId.Equals(other.tableId) && recipeSOIndex == other.recipeSOIndex;
    }

    public override bool Equals(object obj) {
        return obj is TableOrder other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(tableId, recipeSOIndex);
    }
}
