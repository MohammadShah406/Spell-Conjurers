using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

public class ActionManager : MonoBehaviour 
{
    public static ActionManager Instance { get; private set; }
    public UnityEvent onSpellUsed;

    [Header("Projectile Settings")]
    public float projectileSpeed = 12f;
    public float projectileLifetimeAfterImpact = 1.5f;
    public Vector3 spawnOffset = new Vector3(0f, 1f, 0f);

    [Tooltip("Optional container to keep the hierarchy clean.")]
    public Transform projectileContainer;
    public GameObject projectilePrefab;


    public Spell currentSpell;
    public GameObject currentFrom;
    public GameObject currentTo;



    private void Awake()
    {
        Instance = this;
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    public void UseSpell(Spell spell, GameObject from, GameObject to)
    {
        int roll = Random.Range(0, 101);
        if (roll > spell.accuracy)
        {
            Debug.Log("Spell whiffed");
            return;
        }


        //string actionLog = "";
        //if (spell.damage > 0)
        //{
        //    to.GetComponent<Stats>().takeDamage(spell.damage);
        //    actionLog += from.name + " dealt " + spell.damage + " to " + to.name + ". ";
        //}
        //if (spell.selfDamage > 0)
        //{
        //    from.GetComponent<Stats>().takeDamage(spell.selfDamage);
        //    actionLog += from.name + " dealt " + spell.selfDamage + " to itself. ";
        //}

        SpellFunction.Instance.SetVariables(spell, from, to);
        SpellFunction.Instance.CustomSpellFunction();
        setCurrentValues();
        onSpellUsed.Invoke();
    }

    private void setCurrentValues()
    {
        currentSpell = SpellFunction.Instance.spell;
        currentFrom = SpellFunction.Instance.from;
        currentTo = SpellFunction.Instance.to;
    }

    public void spawnProjectile()
    {
        GameObject from = currentFrom;
        GameObject to = currentTo;


        if (from == null || to == null)
        {
            Debug.LogWarning("spawnProjectile aborted: from or to is null.");
            return;
        }

        // Determine grid positions (fallback to world rounded positions)
        Vector2Int fromGrid = TryGetGridPosition(from);
        Vector2Int toGrid = TryGetGridPosition(to);

        // BFS path (list includes start -> end). If none, fallback to straight line.
        List<Vector2Int> path = FindPath(fromGrid, toGrid);

        // Create projectile object
        GameObject proj;
        if (projectilePrefab != null)
        {
            proj = Instantiate(projectilePrefab);
        }
        else
        {
            proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            proj.transform.localScale = Vector3.one * 0.4f;
        }

        proj.name = $"{currentSpell.name}_Projectile";
        if (projectileContainer != null)
            proj.transform.parent = projectileContainer;

        Vector3 startWorld = new Vector3(fromGrid.x, from.transform.position.y, fromGrid.y) + spawnOffset;
        proj.transform.position = startWorld;

        // Start movement coroutine
        StartCoroutine(MoveProjectileAlongPath(proj, path, to, currentSpell));
    }

    private IEnumerator MoveProjectileAlongPath(GameObject projectile, List<Vector2Int> path, GameObject target, Spell spell)
    {
        if (projectile == null)
            yield break;

        // If we have a path of grid tiles, move through them; else direct line
        if (path == null || path.Count < 2)
        {
            // Direct line fallback
            Vector3 targetPos = target.transform.position + spawnOffset;
            while (projectile != null && Vector3.Distance(projectile.transform.position, targetPos) > 0.05f)
            {
                projectile.transform.position = Vector3.MoveTowards(
                    projectile.transform.position,
                    targetPos,
                    projectileSpeed * Time.deltaTime);
                yield return null;
            }
        }
        else
        {
            // Skip index 0 (start)
            for (int i = 1; i < path.Count; i++)
            {
                Vector3 waypoint = new Vector3(path[i].x, projectile.transform.position.y, path[i].y) + spawnOffset;
                while (projectile != null && Vector3.Distance(projectile.transform.position, waypoint) > 0.02f)
                {
                    projectile.transform.position = Vector3.MoveTowards(
                        projectile.transform.position,
                        waypoint,
                        projectileSpeed * Time.deltaTime);
                    yield return null;
                }
                if (projectile == null)
                    yield break;
            }

            // Final adjust to target anchor (in case target moved slightly)
            Vector3 finalPos = target.transform.position + spawnOffset;
            while (projectile != null && Vector3.Distance(projectile.transform.position, finalPos) > 0.05f)
            {
                projectile.transform.position = Vector3.MoveTowards(
                    projectile.transform.position,
                    finalPos,
                    projectileSpeed * Time.deltaTime);
                yield return null;
            }
        }

        // Impact (visual only; damage already handled by spell logic)
        if (projectile != null)
        {
            // Optional: simple impact feedback
            // You can extend: particle effect, sound, etc.
            Destroy(projectile, projectileLifetimeAfterImpact);
        }
    }

    private Vector2Int TryGetGridPosition(GameObject obj)
    {
        if (obj == null)
            return Vector2Int.zero;

        PlayerFunctionality pf = obj.GetComponent<PlayerFunctionality>();
        if (pf != null)
            return pf.gridPosition;

        Enemy enemy = obj.GetComponent<Enemy>();
        if (enemy != null)
            return enemy.gridPosition;

        // Fallback: approximate from world position
        return new Vector2Int(
            Mathf.RoundToInt(obj.transform.position.x),
            Mathf.RoundToInt(obj.transform.position.z));
    }

    // BFS shortest path avoiding non-walkable / occupied tiles (except target occupant)
    private List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        GridManager gm = GridManager.Instance;
        if (gm == null || gm.grid == null)
            return null;

        if (start == goal)
            return new List<Vector2Int> { start, goal };

        Queue<Vector2Int> q = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        q.Enqueue(start);
        visited.Add(start);
        cameFrom[start] = start;

        Vector2Int[] dirs =
        {
            new Vector2Int(1,0),
            new Vector2Int(-1,0),
            new Vector2Int(0,1),
            new Vector2Int(0,-1)
        };

        while (q.Count > 0)
        {
            var current = q.Dequeue();
            if (current == goal)
                break;

            foreach (var d in dirs)
            {
                Vector2Int next = new Vector2Int(current.x + d.x, current.y + d.y);
                if (visited.Contains(next))
                    continue;

                Tile t = gm.GetTile(next);
                if (t == null)
                    continue;
                if (!t.walkable)
                    continue;

                // Allow stepping onto goal even if it has occupant (projectile should still reach)
                if (t.occupant != null && next != goal)
                    continue;

                visited.Add(next);
                cameFrom[next] = current;
                q.Enqueue(next);
            }
        }

        if (!cameFrom.ContainsKey(goal))
        {
            // Unreachable -> attempt closest visited to goal
            Vector2Int closest = start;
            int bestDist = int.MaxValue;
            foreach (var v in visited)
            {
                int dist = Mathf.Abs(v.x - goal.x) + Mathf.Abs(v.y - goal.y);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    closest = v;
                }
            }
            if (closest == start)
                return null;
            return ReconstructPath(cameFrom, start, closest);
        }

        return ReconstructPath(cameFrom, start, goal);
    }

    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int cur = end;
        path.Add(cur);
        while (cur != start)
        {
            cur = cameFrom[cur];
            path.Add(cur);
        }
        path.Reverse();
        return path;
    }

}


