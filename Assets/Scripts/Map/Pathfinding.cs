using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized BFS pathfinding utility for the grid.
/// Eliminates duplicate pathfinding code across systems.
/// </summary>
public static class Pathfinding
{
    private static readonly Vector2Int[] FourDirs =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    /// <summary>Manhattan distance between two grid positions.</summary>
    public static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    /// <summary>
    /// BFS shortest path from start to goal.
    /// Returns list of positions EXCLUDING start, inclusive of goal.
    /// Returns null if goal is unreachable.
    /// </summary>
    public static List<Vector2Int> FindPath(
        GridManager grid, Vector2Int start, Vector2Int goal,
        GameObject mover = null, bool allowOccupiedGoal = false)
    {
        if (grid == null || grid.grid == null) return null;
        if (start == goal) return new List<Vector2Int>();

        var queue = new Queue<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(start);
        cameFrom[start] = start;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == goal) break;

            foreach (var dir in FourDirs)
            {
                var next = current + dir;
                if (cameFrom.ContainsKey(next)) continue;

                Tile tile = grid.GetTile(next);
                if (tile == null || !tile.walkable) continue;

                bool isOccupied = tile.occupant != null && tile.occupant != mover;
                if (isOccupied && !(next == goal && allowOccupiedGoal)) continue;

                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }

        if (!cameFrom.ContainsKey(goal))
            return null;

        return ReconstructPath(cameFrom, start, goal);
    }

    /// <summary>
    /// Find a path to the closest reachable position to the goal.
    /// Used when the goal itself is unreachable.
    /// </summary>
    public static List<Vector2Int> FindPathToClosest(
        GridManager grid, Vector2Int start, Vector2Int goal,
        GameObject mover = null)
    {
        if (grid == null || grid.grid == null) return null;
        if (start == goal) return new List<Vector2Int>();

        var queue = new Queue<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Vector2Int closest = start;
        int closestDist = ManhattanDistance(start, goal);

        queue.Enqueue(start);
        cameFrom[start] = start;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current == goal)
            {
                closest = goal;
                break;
            }

            int dist = ManhattanDistance(current, goal);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = current;
            }

            foreach (var dir in FourDirs)
            {
                var next = current + dir;
                if (cameFrom.ContainsKey(next)) continue;

                Tile tile = grid.GetTile(next);
                if (tile == null || !tile.walkable) continue;
                if (tile.occupant != null && tile.occupant != mover && next != goal) continue;

                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }

        if (closest == start) return null;
        return ReconstructPath(cameFrom, start, closest);
    }

    /// <summary>
    /// BFS computation of all reachable tiles within a given range.
    /// Returns tile to distance mapping.
    /// </summary>
    public static Dictionary<Tile, int> ComputeReachableTiles(
        GridManager grid, Vector2Int start, int range, GameObject mover = null)
    {
        var result = new Dictionary<Tile, int>();
        var queue = new Queue<Vector2Int>();
        var dist = new Dictionary<Vector2Int, int>();

        queue.Enqueue(start);
        dist[start] = 0;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int currentDist = dist[current];

            Tile currentTile = grid.GetTile(current);
            if (currentTile != null && currentDist <= range)
                result[currentTile] = currentDist;

            if (currentDist >= range) continue;

            foreach (var dir in FourDirs)
            {
                var next = current + dir;
                if (dist.ContainsKey(next)) continue;

                Tile tile = grid.GetTile(next);
                if (tile == null || !tile.walkable) continue;
                if (tile.occupant != null && tile.occupant != mover) continue;

                dist[next] = currentDist + 1;
                queue.Enqueue(next);
            }
        }

        return result;
    }

    /// <summary>
    /// Get all grid positions within Manhattan distance of origin, clamped to grid bounds.
    /// </summary>
    public static List<Vector2Int> GetPositionsInRange(
        Vector2Int origin, int range, int gridWidth, int gridHeight)
    {
        var positions = new List<Vector2Int>();
        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                if (Mathf.Abs(dx) + Mathf.Abs(dy) > range) continue;
                int x = origin.x + dx;
                int y = origin.y + dy;
                if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
                    positions.Add(new Vector2Int(x, y));
            }
        }
        return positions;
    }

    private static List<Vector2Int> ReconstructPath(
        Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int start, Vector2Int end)
    {
        var path = new List<Vector2Int>();
        var current = end;
        while (current != start)
        {
            path.Add(current);
            current = cameFrom[current];
        }
        path.Reverse();
        return path;
    }
}
