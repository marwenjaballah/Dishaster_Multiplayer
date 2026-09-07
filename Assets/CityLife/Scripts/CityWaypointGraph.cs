using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityLife
{
    /// <summary>
    /// A single waypoint located strictly on the pedestrian sidewalk (offset ±4.0m from road center).
    /// </summary>
    public class SidewalkWaypoint
    {
        public Vector3 Position { get; set; }
        public List<SidewalkWaypoint> Connections { get; } = new();
        public int GridX { get; set; }
        public int GridZ { get; set; }

        public SidewalkWaypoint(Vector3 position, int x, int z)
        {
            Position = position;
            GridX = x;
            GridZ = z;
        }

        public void AddConnection(SidewalkWaypoint other)
        {
            if (other != null && other != this && !Connections.Contains(other))
            {
                Connections.Add(other);
                if (!other.Connections.Contains(this))
                    other.Connections.Add(this);
            }
        }
    }

    /// <summary>
    /// A single waypoint located strictly inside a vehicle driving lane (offset ±1.5m, right-hand traffic).
    /// </summary>
    public class LaneWaypoint
    {
        public Vector3 Position { get; set; }
        public List<LaneWaypoint> Connections { get; } = new();
        public bool IsDeadEnd { get; set; }
        public int GridX { get; set; }
        public int GridZ { get; set; }

        public LaneWaypoint(Vector3 position, int x, int z, bool isDeadEnd = false)
        {
            Position = position;
            GridX = x;
            GridZ = z;
            IsDeadEnd = isDeadEnd;
        }

        public void AddDirectedConnection(LaneWaypoint next)
        {
            if (next != null && next != this && !Connections.Contains(next))
            {
                Connections.Add(next);
            }
        }
    }

    /// <summary>
    /// Builds and queries directed navigation graphs for:
    /// 1. Sidewalks (pedestrians at ±4.0m offset)
    /// 2. Road Lanes (vehicles at ±1.5m offset, right-hand traffic)
    /// </summary>
    public class CityWaypointGraph
    {
        public List<SidewalkWaypoint> SidewalkWaypoints { get; } = new();
        public List<LaneWaypoint>     LaneWaypoints     { get; } = new();
        public List<LaneWaypoint>     LaneSpawnPoints   { get; } = new();
        public List<LaneWaypoint>     DeadEndSpawnPoints { get; } = new();

        private const float SidewalkOffset = 4.0f;
        private const float LaneOffset     = 1.5f;

        /// <summary>
        /// Builds both sidewalk and car lane waypoint graphs from CityGenerator road node data.
        /// </summary>
        public void Build(List<RoadNodeData> roadData, float cellSize = 10f)
        {
            SidewalkWaypoints.Clear();
            LaneWaypoints.Clear();
            LaneSpawnPoints.Clear();
            DeadEndSpawnPoints.Clear();

            if (roadData == null || roadData.Count == 0) return;

            float half = cellSize * 0.5f;

            // Dictionaries to weld edge boundary waypoints between adjacent tiles
            // key: quantised 2D grid boundary position
            var sidewalkEdgeMap = new Dictionary<long, SidewalkWaypoint>();
            var laneEdgeMap     = new Dictionary<long, LaneWaypoint>();

            long PosKey(Vector3 pos)
            {
                long xk = (long)Mathf.Round(pos.x * 10f);
                long zk = (long)Mathf.Round(pos.z * 10f);
                return (xk << 32) ^ (zk & 0xFFFFFFFFL);
            }

            SidewalkWaypoint GetOrCreateSidewalkEdge(Vector3 pos, int gx, int gz)
            {
                long key = PosKey(pos);
                if (!sidewalkEdgeMap.TryGetValue(key, out var wp))
                {
                    wp = new SidewalkWaypoint(pos, gx, gz);
                    sidewalkEdgeMap[key] = wp;
                    SidewalkWaypoints.Add(wp);
                }
                return wp;
            }

            LaneWaypoint GetOrCreateLaneEdge(Vector3 pos, int gx, int gz)
            {
                long key = PosKey(pos);
                if (!laneEdgeMap.TryGetValue(key, out var wp))
                {
                    wp = new LaneWaypoint(pos, gx, gz);
                    laneEdgeMap[key] = wp;
                    LaneWaypoints.Add(wp);
                }
                return wp;
            }

            foreach (var cell in roadData)
            {
                Vector3 c = cell.worldPosition;
                c.y = 0f;
                int gx = cell.x;
                int gz = cell.z;

                bool n = cell.hasNorthRoad;
                bool s = cell.hasSouthRoad;
                bool e = cell.hasEastRoad;
                bool w = cell.hasWestRoad;

                int connCount = (n ? 1 : 0) + (s ? 1 : 0) + (e ? 1 : 0) + (w ? 1 : 0);

                // ─────────────────────────────────────────────────────────────
                // 1. SIDEWALKS (Offset ±4.0 from cell center)
                // ─────────────────────────────────────────────────────────────
                // The 4 corner sidewalk points inside this tile:
                var swNE = new SidewalkWaypoint(c + new Vector3(+SidewalkOffset, 0, +SidewalkOffset), gx, gz);
                var swNW = new SidewalkWaypoint(c + new Vector3(-SidewalkOffset, 0, +SidewalkOffset), gx, gz);
                var swSE = new SidewalkWaypoint(c + new Vector3(+SidewalkOffset, 0, -SidewalkOffset), gx, gz);
                var swSW = new SidewalkWaypoint(c + new Vector3(-SidewalkOffset, 0, -SidewalkOffset), gx, gz);

                SidewalkWaypoints.Add(swNE);
                SidewalkWaypoints.Add(swNW);
                SidewalkWaypoints.Add(swSE);
                SidewalkWaypoints.Add(swSW);

                // Edge connections to adjacent tiles:
                if (n)
                {
                    var edgeN_E = GetOrCreateSidewalkEdge(c + new Vector3(+SidewalkOffset, 0, +half), gx, gz);
                    var edgeN_W = GetOrCreateSidewalkEdge(c + new Vector3(-SidewalkOffset, 0, +half), gx, gz);
                    swNE.AddConnection(edgeN_E);
                    swNW.AddConnection(edgeN_W);
                }
                if (s)
                {
                    var edgeS_E = GetOrCreateSidewalkEdge(c + new Vector3(+SidewalkOffset, 0, -half), gx, gz);
                    var edgeS_W = GetOrCreateSidewalkEdge(c + new Vector3(-SidewalkOffset, 0, -half), gx, gz);
                    swSE.AddConnection(edgeS_E);
                    swSW.AddConnection(edgeS_W);
                }
                if (e)
                {
                    var edgeE_N = GetOrCreateSidewalkEdge(c + new Vector3(+half, 0, +SidewalkOffset), gx, gz);
                    var edgeE_S = GetOrCreateSidewalkEdge(c + new Vector3(+half, 0, -SidewalkOffset), gx, gz);
                    swNE.AddConnection(edgeE_N);
                    swSE.AddConnection(edgeE_S);
                }
                if (w)
                {
                    var edgeW_N = GetOrCreateSidewalkEdge(c + new Vector3(-half, 0, +SidewalkOffset), gx, gz);
                    var edgeW_S = GetOrCreateSidewalkEdge(c + new Vector3(-half, 0, -SidewalkOffset), gx, gz);
                    swNW.AddConnection(edgeW_N);
                    swSW.AddConnection(edgeW_S);
                }

                // Internal sidewalk continuity:
                // Straight N-S (or dead-ends):
                if ((n && s) || (n && !e && !w) || (s && !e && !w))
                {
                    swNE.AddConnection(swSE); // East sidewalk straight
                    swNW.AddConnection(swSW); // West sidewalk straight
                }
                // Straight E-W (or dead-ends):
                if ((e && w) || (e && !n && !s) || (w && !n && !s))
                {
                    swNW.AddConnection(swNE); // North sidewalk straight
                    swSW.AddConnection(swSE); // South sidewalk straight
                }
                // Corners / Intersections:
                if (n && e) swNE.AddConnection(swNW); // Corner NE allows corner turn
                if (n && w) swNW.AddConnection(swSW);
                if (s && e) swSE.AddConnection(swSW);
                if (s && w) swSW.AddConnection(swSE);

                // Dead-end curb wrap-around:
                if (connCount <= 1)
                {
                    // Allow walking across the closed end of the street to the opposite sidewalk
                    if (n && !s) swSE.AddConnection(swSW);
                    if (s && !n) swNE.AddConnection(swNW);
                    if (e && !w) swNW.AddConnection(swSW);
                    if (w && !e) swNE.AddConnection(swSE);
                }

                // ─────────────────────────────────────────────────────────────
                // 2. CAR LANES (Offset ±1.5 from road center, right-hand traffic)
                // ─────────────────────────────────────────────────────────────
                // Boundary points:
                LaneWaypoint inN  = null, outN = null;
                LaneWaypoint inS  = null, outS = null;
                LaneWaypoint inE  = null, outE = null;
                LaneWaypoint inW  = null, outW = null;

                if (n)
                {
                    // Heading North exits at North (+1.5, +half); Heading South enters at North (-1.5, +half)
                    outN = GetOrCreateLaneEdge(c + new Vector3(+LaneOffset, 0, +half), gx, gz);
                    inN  = GetOrCreateLaneEdge(c + new Vector3(-LaneOffset, 0, +half), gx, gz);
                }
                if (s)
                {
                    // Heading South exits at South (-1.5, -half); Heading North enters at South (+1.5, -half)
                    outS = GetOrCreateLaneEdge(c + new Vector3(-LaneOffset, 0, -half), gx, gz);
                    inS  = GetOrCreateLaneEdge(c + new Vector3(+LaneOffset, 0, -half), gx, gz);
                }
                if (e)
                {
                    // Heading East exits at East (+half, -1.5); Heading West enters at East (+half, +1.5)
                    outE = GetOrCreateLaneEdge(c + new Vector3(+half, 0, -LaneOffset), gx, gz);
                    inE  = GetOrCreateLaneEdge(c + new Vector3(+half, 0, +LaneOffset), gx, gz);
                }
                if (w)
                {
                    // Heading West exits at West (-half, +1.5); Heading East enters at West (-half, -1.5)
                    outW = GetOrCreateLaneEdge(c + new Vector3(-half, 0, +LaneOffset), gx, gz);
                    inW  = GetOrCreateLaneEdge(c + new Vector3(-half, 0, -LaneOffset), gx, gz);
                }

                // Internal lane waypoints inside this cell:
                if (connCount == 1)
                {
                    // Dead-end road:
                    // 1. Inbound lane (incoming from open side) terminates at endNode (IsDeadEnd = true) where cars despawn.
                    // 2. Outbound lane (right side of road) starts at startNode where cars can spawn and drive into the city.
                    if (n)
                    {
                        if (inN != null)
                        {
                            var endNode = new LaneWaypoint(c + new Vector3(-LaneOffset, 0, 0), gx, gz, isDeadEnd: true);
                            LaneWaypoints.Add(endNode);
                            inN.AddDirectedConnection(endNode);
                        }
                        if (outN != null)
                        {
                            // Heading North (+Z) -> right side is +X (+LaneOffset)
                            var startNode = new LaneWaypoint(c + new Vector3(+LaneOffset, 0, 0), gx, gz);
                            LaneWaypoints.Add(startNode);
                            LaneSpawnPoints.Add(startNode);
                            DeadEndSpawnPoints.Add(startNode);
                            startNode.AddDirectedConnection(outN);
                        }
                    }
                    if (s)
                    {
                        if (inS != null)
                        {
                            var endNode = new LaneWaypoint(c + new Vector3(+LaneOffset, 0, 0), gx, gz, isDeadEnd: true);
                            LaneWaypoints.Add(endNode);
                            inS.AddDirectedConnection(endNode);
                        }
                        if (outS != null)
                        {
                            // Heading South (-Z) -> right side is -X (-LaneOffset)
                            var startNode = new LaneWaypoint(c + new Vector3(-LaneOffset, 0, 0), gx, gz);
                            LaneWaypoints.Add(startNode);
                            LaneSpawnPoints.Add(startNode);
                            DeadEndSpawnPoints.Add(startNode);
                            startNode.AddDirectedConnection(outS);
                        }
                    }
                    if (e)
                    {
                        if (inE != null)
                        {
                            var endNode = new LaneWaypoint(c + new Vector3(0, 0, +LaneOffset), gx, gz, isDeadEnd: true);
                            LaneWaypoints.Add(endNode);
                            inE.AddDirectedConnection(endNode);
                        }
                        if (outE != null)
                        {
                            // Heading East (+X) -> right side is -Z (-LaneOffset)
                            var startNode = new LaneWaypoint(c + new Vector3(0, 0, -LaneOffset), gx, gz);
                            LaneWaypoints.Add(startNode);
                            LaneSpawnPoints.Add(startNode);
                            DeadEndSpawnPoints.Add(startNode);
                            startNode.AddDirectedConnection(outE);
                        }
                    }
                    if (w)
                    {
                        if (inW != null)
                        {
                            var endNode = new LaneWaypoint(c + new Vector3(0, 0, -LaneOffset), gx, gz, isDeadEnd: true);
                            LaneWaypoints.Add(endNode);
                            inW.AddDirectedConnection(endNode);
                        }
                        if (outW != null)
                        {
                            // Heading West (-X) -> right side is +Z (+LaneOffset)
                            var startNode = new LaneWaypoint(c + new Vector3(0, 0, +LaneOffset), gx, gz);
                            LaneWaypoints.Add(startNode);
                            LaneSpawnPoints.Add(startNode);
                            DeadEndSpawnPoints.Add(startNode);
                            startNode.AddDirectedConnection(outW);
                        }
                    }
                }
                else
                {
                    // Straight N-S
                    if (n && s && !e && !w)
                    {
                        var midN = new LaneWaypoint(c + new Vector3(+LaneOffset, 0, 0), gx, gz);
                        var midS = new LaneWaypoint(c + new Vector3(-LaneOffset, 0, 0), gx, gz);
                        LaneWaypoints.Add(midN);
                        LaneWaypoints.Add(midS);
                        LaneSpawnPoints.Add(midN);
                        LaneSpawnPoints.Add(midS);

                        inS?.AddDirectedConnection(midN);
                        midN.AddDirectedConnection(outN);

                        inN?.AddDirectedConnection(midS);
                        midS.AddDirectedConnection(outS);
                    }
                    // Straight E-W
                    else if (e && w && !n && !s)
                    {
                        var midE = new LaneWaypoint(c + new Vector3(0, 0, -LaneOffset), gx, gz);
                        var midW = new LaneWaypoint(c + new Vector3(0, 0, +LaneOffset), gx, gz);
                        LaneWaypoints.Add(midE);
                        LaneWaypoints.Add(midW);
                        LaneSpawnPoints.Add(midE);
                        LaneSpawnPoints.Add(midW);

                        inW?.AddDirectedConnection(midE);
                        midE.AddDirectedConnection(outE);

                        inE?.AddDirectedConnection(midW);
                        midW.AddDirectedConnection(outW);
                    }
                    // Corners / T-Junctions / Cross
                    else
                    {
                        // From South Inbound (+1.5, -half):
                        if (inS != null)
                        {
                            if (outN != null) inS.AddDirectedConnection(outN); // straight
                            if (outE != null) // right turn (inside curve)
                            {
                                var pivot = new LaneWaypoint(c + new Vector3(+LaneOffset, 0, -LaneOffset), gx, gz);
                                LaneWaypoints.Add(pivot);
                                inS.AddDirectedConnection(pivot);
                                pivot.AddDirectedConnection(outE);
                            }
                            if (outW != null) // wide left turn: forward into intersection, arc wide, enter Westbound lane
                            {
                                var p1 = new LaneWaypoint(c + new Vector3(+LaneOffset, 0, +0.6f), gx, gz);
                                var p2 = new LaneWaypoint(c + new Vector3(-0.8f, 0, +LaneOffset), gx, gz);
                                LaneWaypoints.Add(p1);
                                LaneWaypoints.Add(p2);
                                inS.AddDirectedConnection(p1);
                                p1.AddDirectedConnection(p2);
                                p2.AddDirectedConnection(outW);
                            }
                        }

                        // From North Inbound (-1.5, +half):
                        if (inN != null)
                        {
                            if (outS != null) inN.AddDirectedConnection(outS); // straight
                            if (outW != null) // right turn (inside curve)
                            {
                                var pivot = new LaneWaypoint(c + new Vector3(-LaneOffset, 0, +LaneOffset), gx, gz);
                                LaneWaypoints.Add(pivot);
                                inN.AddDirectedConnection(pivot);
                                pivot.AddDirectedConnection(outW);
                            }
                            if (outE != null) // wide left turn: forward into intersection, arc wide, enter Eastbound lane
                            {
                                var p1 = new LaneWaypoint(c + new Vector3(-LaneOffset, 0, -0.6f), gx, gz);
                                var p2 = new LaneWaypoint(c + new Vector3(+0.8f, 0, -LaneOffset), gx, gz);
                                LaneWaypoints.Add(p1);
                                LaneWaypoints.Add(p2);
                                inN.AddDirectedConnection(p1);
                                p1.AddDirectedConnection(p2);
                                p2.AddDirectedConnection(outE);
                            }
                        }

                        // From West Inbound (-half, -1.5):
                        if (inW != null)
                        {
                            if (outE != null) inW.AddDirectedConnection(outE); // straight
                            if (outS != null) // right turn (inside curve)
                            {
                                var pivot = new LaneWaypoint(c + new Vector3(-LaneOffset, 0, -LaneOffset), gx, gz);
                                LaneWaypoints.Add(pivot);
                                inW.AddDirectedConnection(pivot);
                                pivot.AddDirectedConnection(outS);
                            }
                            if (outN != null) // wide left turn: forward into intersection, arc wide, enter Northbound lane
                            {
                                var p1 = new LaneWaypoint(c + new Vector3(+0.6f, 0, -LaneOffset), gx, gz);
                                var p2 = new LaneWaypoint(c + new Vector3(+LaneOffset, 0, +0.8f), gx, gz);
                                LaneWaypoints.Add(p1);
                                LaneWaypoints.Add(p2);
                                inW.AddDirectedConnection(p1);
                                p1.AddDirectedConnection(p2);
                                p2.AddDirectedConnection(outN);
                            }
                        }

                        // From East Inbound (+half, +1.5):
                        if (inE != null)
                        {
                            if (outW != null) inE.AddDirectedConnection(outW); // straight
                            if (outN != null) // right turn (inside curve)
                            {
                                var pivot = new LaneWaypoint(c + new Vector3(+LaneOffset, 0, +LaneOffset), gx, gz);
                                LaneWaypoints.Add(pivot);
                                inE.AddDirectedConnection(pivot);
                                pivot.AddDirectedConnection(outN);
                            }
                            if (outS != null) // wide left turn: forward into intersection, arc wide, enter Southbound lane
                            {
                                var p1 = new LaneWaypoint(c + new Vector3(-0.6f, 0, +LaneOffset), gx, gz);
                                var p2 = new LaneWaypoint(c + new Vector3(-LaneOffset, 0, -0.8f), gx, gz);
                                LaneWaypoints.Add(p1);
                                LaneWaypoints.Add(p2);
                                inE.AddDirectedConnection(p1);
                                p1.AddDirectedConnection(p2);
                                p2.AddDirectedConnection(outS);
                            }
                        }
                    }
                }
            }

            // Collect non-dead-end lane spawn points from all registered lane waypoints
            foreach (var lw in LaneWaypoints)
            {
                if (!lw.IsDeadEnd && lw.Connections.Count > 0 && !LaneSpawnPoints.Contains(lw))
                {
                    LaneSpawnPoints.Add(lw);
                }
            }

            Debug.Log($"[CityWaypointGraph] Built graphs: {SidewalkWaypoints.Count} sidewalk waypoints, {LaneWaypoints.Count} lane waypoints ({LaneSpawnPoints.Count} spawnable, {DeadEndSpawnPoints.Count} dead-end exits).");
        }

        // ── Query API ────────────────────────────────────────────────────────

        public SidewalkWaypoint GetRandomSidewalkWaypoint(System.Random rng)
        {
            if (SidewalkWaypoints.Count == 0) return null;
            // Prefer waypoints that have connections
            for (int attempt = 0; attempt < 10; attempt++)
            {
                var wp = SidewalkWaypoints[rng.Next(SidewalkWaypoints.Count)];
                if (wp.Connections.Count > 0) return wp;
            }
            return SidewalkWaypoints[rng.Next(SidewalkWaypoints.Count)];
        }

        public SidewalkWaypoint GetNextSidewalkWaypoint(SidewalkWaypoint current, SidewalkWaypoint previous, System.Random rng)
        {
            if (current == null || current.Connections.Count == 0) return null;

            // Filter out previous node to avoid immediate U-turn unless it's a dead-end
            var valid = new List<SidewalkWaypoint>();
            foreach (var conn in current.Connections)
            {
                if (conn != previous) valid.Add(conn);
            }

            if (valid.Count > 0)
                return valid[rng.Next(valid.Count)];

            // Fallback: reverse if dead-end
            return previous != null && current.Connections.Contains(previous) ? previous : current.Connections[0];
        }

        public LaneWaypoint GetRandomLaneWaypoint(System.Random rng)
        {
            if (LaneSpawnPoints.Count > 0)
                return LaneSpawnPoints[rng.Next(LaneSpawnPoints.Count)];
            if (LaneWaypoints.Count > 0)
                return LaneWaypoints[rng.Next(LaneWaypoints.Count)];
            return null;
        }

        public LaneWaypoint GetRandomDeadEndSpawn(System.Random rng)
        {
            if (DeadEndSpawnPoints.Count == 0) return null;
            return DeadEndSpawnPoints[rng.Next(DeadEndSpawnPoints.Count)];
        }

        public LaneWaypoint GetRandomLaneEntry(System.Random rng)
        {
            if (LaneSpawnPoints.Count > 0)
                return LaneSpawnPoints[rng.Next(LaneSpawnPoints.Count)];

            if (DeadEndSpawnPoints.Count > 0)
                return DeadEndSpawnPoints[rng.Next(DeadEndSpawnPoints.Count)];

            return null;
        }

        public LaneWaypoint GetNextLaneWaypoint(LaneWaypoint current, LaneWaypoint previous, System.Random rng)
        {
            if (current == null || current.Connections.Count == 0) return null;

            // Directed graph: choose from available outgoing connections
            return current.Connections[rng.Next(current.Connections.Count)];
        }
    }
}
