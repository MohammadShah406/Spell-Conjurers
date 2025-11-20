using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

public class Enemy : MonoBehaviour
{
    public float yPos = 1.5f;
    public Vector2Int gridPosition;
    public int moveRange = 3;
    public int attackRange = 1;
    public float moveSpeed = 5f;

    private GridManager gridManager;
    private GameObject player;

    public Spell[] spells = new Spell[4];
    public int preffered = 0;

    public bool debugMode = true;

    public float dmgMultiplier = 1.0f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && debugMode)
        {
            EnemyManager.Instance.ClearScoreDebug();
        }
    }

    public void Initialize(Vector2Int startPos, GridManager grid, GameObject playerRef)
    {
        gridPosition = startPos;
        gridManager = grid;
        player = playerRef;

        transform.position = new Vector3(startPos.x, yPos, startPos.y);
        grid.GetTile(startPos).occupant = gameObject;

        StartCoroutine(FacePlayer());

        InitializeSpells();
        PrioriizeSpell();
    }

    public IEnumerator TakeTurn(System.Action onComplete)
    {
        EnemyManager.Instance.calculateThreatGrid();
        Dictionary<String, object> result = EnemyManager.Instance.CalculateScore(this);

        Vector2Int location = (Vector2Int)result["location"];

        // NEW: BFS pathfinding to avoid going "through" obstacles.
        List<Vector2Int> path = FindPath(gridPosition, location);

        if (path != null && path.Count > 1)
        {
            // Move along path, limited by moveRange
            yield return MoveAlongPath(path, moveRange);
        }
        else
        {
            // Fallback to original direct move if path not found (should be rare)
            yield return MoveTo(location);
        }

        Debug.Log("Enemy moving to " + gridPosition);

        if (result.ContainsKey("target"))
        {
            GameObject target = (GameObject)result["target"];
            Spell spell = (Spell)result["spell"];
            Debug.Log("Attempting to hit target " + target.name + " with spell " + spell.name);
            AttackPlayer(spell, target);
        }

        SyncGridPosition();
        onComplete?.Invoke();
    }

    // OLD single segment movement retained for fallback
    private IEnumerator MoveTo(Vector2Int targetPos)
    {
        Vector3 start = transform.position;
        Vector3 end = new Vector3(targetPos.x, yPos, targetPos.y);
        float t = 0;

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        transform.position = end;
        SyncGridPosition();
    }

    // NEW: Move along a computed path step-by-step (respecting obstacles).
    private IEnumerator MoveAlongPath(List<Vector2Int> path, int maxSteps)
    {
        // path includes start; skip index 0
        int stepsToTake = Mathf.Min(maxSteps, path.Count - 1);
        for (int i = 1; i <= stepsToTake; i++)
        {
            Vector2Int nextPos = path[i];
            // Clear previous tile occupant
            Tile prevTile = gridManager.GetTile(gridPosition);
            if (prevTile != null && prevTile.occupant == gameObject)
                prevTile.occupant = null;

            // Lerp to next tile
            Vector3 start = transform.position;
            Vector3 end = new Vector3(nextPos.x, yPos, nextPos.y);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * moveSpeed;
                transform.position = Vector3.Lerp(start, end, t);
                yield return null;
            }

            transform.position = end;
            // Occupy new tile
            Tile newTile = gridManager.GetTile(nextPos);
            if (newTile != null)
                newTile.occupant = gameObject;

            gridPosition = nextPos;
        }
        SyncGridPosition();
    }

    // NEW: BFS shortest path avoiding non-walkable / occupied tiles.
    private List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        if (gridManager == null || gridManager.grid == null)
            return null;

        // If the goal itself is not passable, we still try to move as close as possible.
        bool goalPassable = IsPassable(goal) || goal == start;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        Vector2Int? closestToGoal = null;
        int closestDist = int.MaxValue;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            // Track closest reachable if goal not passable/unreachable
            int distToGoal = EnemyManager.ManhattanDistance(current, goal);
            if (distToGoal < closestDist)
            {
                closestDist = distToGoal;
                closestToGoal = current;
            }

            if (current == goal && goalPassable)
                return ReconstructPath(cameFrom, start, goal);

            foreach (var n in GetNeighbors(current))
            {
                if (visited.Contains(n))
                    continue;
                if (!IsPassable(n) && n != goal) // allow goal if scoring picked it and it's currently free
                    continue;

                visited.Add(n);
                cameFrom[n] = current;
                queue.Enqueue(n);
            }
        }

        // Goal unreachable: use closest visited as fallback (but if it's just start, no movement).
        if (closestToGoal.HasValue && closestToGoal.Value != start)
            return ReconstructPath(cameFrom, start, closestToGoal.Value);

        return new List<Vector2Int> { start }; // no movement possible
    }

    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = end;
        path.Add(current);
        while (current != start)
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }

    private IEnumerable<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        // 4-directional movement
        Vector2Int[] dirs =
        {
            new Vector2Int(1,0),
            new Vector2Int(-1,0),
            new Vector2Int(0,1),
            new Vector2Int(0,-1)
        };

        foreach (var d in dirs)
        {
            Vector2Int np = new Vector2Int(pos.x + d.x, pos.y + d.y);
            if (np.x >= 0 && np.x < gridManager.width && np.y >= 0 && np.y < gridManager.height)
                yield return np;
        }
    }

    private bool IsPassable(Vector2Int pos)
    {
        Tile t = gridManager.GetTile(pos);
        if (t == null) return false;
        if (!t.walkable) return false;
        if (t.occupant != null && t.occupant != gameObject) return false;
        return true;
    }

    public void SyncGridPosition()
    {
        if (gridManager == null || gridManager.grid == null)
            return;

        Vector2Int newPos = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.z)
        );

        if (newPos != gridPosition)
        {
            Tile oldTile = gridManager.GetTile(gridPosition);
            if (oldTile != null && oldTile.occupant == gameObject)
                oldTile.occupant = null;
        }

        Tile newTile = gridManager.GetTile(newPos);
        if (newTile != null)
        {
            newTile.occupant = gameObject;
            gridPosition = newPos;
        }
    }

    private IEnumerator FacePlayer()
    {
        Vector3 direction = player.transform.position - transform.position;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float t = 0f;
            float rotateSpeed = 10f;

            while (t < 1f)
            {
                t += Time.deltaTime * rotateSpeed;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
                yield return null;
            }

            transform.rotation = targetRotation;
        }
    }

    private void InitializeSpells()
    {
        for (int i = 0; i < spells.Length; i++)
        {
            spells[i] = JsonManager.Instance.ReturnRandomSpell();
        }

        for (int i = 0; i < spells.Length; i++)
        {
            spells[i].damage = (int)(spells[i].damage * dmgMultiplier);
        }
    }

    public void PrioriizeSpell()
    {
        int rando = UnityEngine.Random.Range(0, spells.Length);
        preffered = rando;
    }

    public void CheckAnySpellRange(int distToPlayer)
    {
        for (int i = 0; i < spells.Length; i++)
        {
            if (distToPlayer <= spells[i].range)
            {
                attackRange = spells[i].range;
                preffered = i;
            }
        }
    }

    public void AttackPlayer()
    {
        if (ActionManager.Instance == null)
        {
            Debug.LogError("ActionManager.Instance is null.");
        }

        StartCoroutine(FacePlayer());
        Debug.Log("Attacking Player with " + spells[preffered].name);
        ActionManager.Instance.UseSpell(spells[preffered], this.gameObject, player.gameObject);
    }

    public void AttackPlayer(Spell spell, GameObject target)
    {
        if (ActionManager.Instance == null)
        {
            Debug.LogError("ActionManager.Instance is null.");
        }

        StartCoroutine(FacePlayer());
        Debug.Log("Attacking Player with " + spell.name);
        ActionManager.Instance.UseSpell(spell, this.gameObject, target);
    }

    private void OnMouseDown()
    {
        if (debugMode)
        {
            EnemyManager.Instance.ClearScoreDebug();
            EnemyManager.Instance.ShowScoreGrid(this);
        }
    }

    private void SetMulitplier(float multiplier)
    {
        dmgMultiplier = multiplier;
    }
}
