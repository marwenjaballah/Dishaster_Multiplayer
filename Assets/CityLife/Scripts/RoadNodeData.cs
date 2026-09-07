using UnityEngine;

namespace CityLife
{
    /// <summary>
    /// Lightweight struct representing one road cell's world position and connectivity.
    /// Populated by CityGenerator and consumed by CityLifeManager / CityWaypointGraph.
    /// </summary>
    [System.Serializable]
    public struct RoadNodeData
    {
        public int x;
        public int z;
        public Vector3 worldPosition;
        public bool hasNorthRoad;
        public bool hasSouthRoad;
        public bool hasEastRoad;
        public bool hasWestRoad;
    }
}
