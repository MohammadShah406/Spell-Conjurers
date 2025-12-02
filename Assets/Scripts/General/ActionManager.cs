using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

public class ActionManager : MonoBehaviour 
{
    public static ActionManager Instance { get; private set; }
    public UnityEvent onSpellUsed;
    public UnityEvent onSpellMiss;

    [Header("Projectile Settings")]
    public float projectileSpeed = 12f;
    public float projectileLifetimeAfterImpact = 1.5f; 
    public Vector3 spawnOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Projectile Slam Settings")]
    [Tooltip("Speed used when the projectile slams downward at the end.")]
    public float projectileSlamSpeed = 30f;
    [Tooltip("Optional pause (seconds) after reaching above target before slamming down.")]
    public float slamPause = 0.05f;

    [Tooltip("Optional container to keep the hierarchy clean.")]
    public Transform projectileContainer;
    public GameObject projectilePrefab;

    public Spell currentSpell;
    public GameObject currentFrom;
    public GameObject currentTo;

    private void Awake()
    {
        Instance = this;
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
            onSpellMiss.Invoke();
            to.GetComponent<Stats>().ShowFloatingText("Miss", Color.red);
            Debug.Log("Spell whiffed");
            return;
        }

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

        Vector2Int fromGrid = TryGetGridPosition(from);
        Vector2Int toGrid = TryGetGridPosition(to);
        List<Vector2Int> path = FindPath(fromGrid, toGrid);

        GameObject proj;
        if (projectilePrefab != null)
        {
            proj = Instantiate(projectilePrefab);
            var psr = proj.GetComponentInChildren<ParticleSystemRenderer>();
            if (psr != null)
                psr.material.color = new Color(currentSpell.ColorR, currentSpell.ColorG, currentSpell.ColorB, 0.5f);
            var rend = proj.GetComponent<Renderer>();
            if (rend != null)
                rend.material.color = new Color(currentSpell.ColorR, currentSpell.ColorG, currentSpell.ColorB);
            proj.name = $"{currentSpell.name}_Projectile";
            if (projectileContainer != null)
                proj.transform.parent = projectileContainer;

            float travelY = currentFrom.transform.position.y;
            Vector3 startWorld = new Vector3(fromGrid.x, travelY, fromGrid.y) + new Vector3(spawnOffset.x, 0f, spawnOffset.z);
            proj.transform.position = startWorld;

            StartCoroutine(MoveProjectileAlongPath(proj, path, to, currentSpell, travelY));
        }
    }

    private IEnumerator MoveProjectileAlongPath(GameObject projectile, List<Vector2Int> path, GameObject target, Spell spell, float travelY)
    {
        Projectile projComponent = projectile.GetComponent<Projectile>();
        projComponent.target = target;
        projComponent.targetStats = target.GetComponent<Stats>();
        projComponent.targetTransform = target.transform.position;

        if (projComponent != null && projComponent.isDestroyed)
            yield break;

        if (projectile == null)
            yield break;

        if (path == null || path.Count < 2)
        {
            Vector3 targetFlat = new Vector3(
                target.transform.position.x,
                travelY,
                target.transform.position.z) + new Vector3(spawnOffset.x, 0f, spawnOffset.z);

            while (projectile != null && Vector3.Distance(projectile.transform.position, targetFlat) > 0.05f)
            {
                Vector3 current = projectile.transform.position;
                if (current.y != travelY)
                {
                    current.y = travelY;
                    projectile.transform.position = current;
                }

                projectile.transform.position = Vector3.MoveTowards(
                    projectile.transform.position,
                    targetFlat,
                    projectileSpeed * Time.deltaTime);
                yield return null;
            }
        }
        else
        {
            for (int i = 1; i < path.Count; i++)
            {
                Vector3 waypoint = new Vector3(path[i].x, travelY, path[i].y) + new Vector3(spawnOffset.x, 0f, spawnOffset.z);
                while (projectile != null && Vector3.Distance(projectile.transform.position, waypoint) > 0.02f)
                {
                    Vector3 current = projectile.transform.position;
                    if (current.y != travelY)
                    {
                        current.y = travelY;
                        projectile.transform.position = current;
                    }

                    projectile.transform.position = Vector3.MoveTowards(
                        projectile.transform.position,
                        waypoint,
                        projectileSpeed * Time.deltaTime);
                    yield return null;
                }
                if (projectile == null)
                    yield break;
            }

            if (target == null)
                yield break;

            Vector3 aboveTarget = new Vector3(
                target.transform.position.x,
                travelY,
                target.transform.position.z) + new Vector3(spawnOffset.x, 0f, spawnOffset.z);

            while (projectile != null && Vector3.Distance(projectile.transform.position, aboveTarget) > 0.05f)
            {
                Vector3 current = projectile.transform.position;
                if (current.y != travelY)
                {
                    current.y = travelY;
                    projectile.transform.position = current;
                }

                projectile.transform.position = Vector3.MoveTowards(
                    projectile.transform.position,
                    aboveTarget,
                    projectileSpeed * Time.deltaTime);
                yield return null;
            }
        }

        // Small pause before slam (optional)
        if (slamPause > 0f)
            yield return new WaitForSeconds(slamPause);

        // Slam down to target's actual Y
        if (projectile != null && target != null)
        {
            float targetY = target.transform.position.y; // Ground / model base
            Vector3 slamTarget = new Vector3(
                projectile.transform.position.x,
                targetY,
                projectile.transform.position.z);

            while (projectile != null && projectile.transform.position.y > targetY + 0.01f)
            {
                Vector3 pos = projectile.transform.position;
                pos.y = Mathf.MoveTowards(pos.y, targetY, projectileSlamSpeed * Time.deltaTime);
                projectile.transform.position = pos;
                yield return null;
            }
        }

        // Immediate destruction upon reaching destination (changed from delayed Destroy with lifetime).
        if (projectile != null)
            Destroy(projectile);

        if (target == null && projectile != null)
            Destroy(projectile);
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

        return new Vector2Int(
            Mathf.RoundToInt(obj.transform.position.x),
            Mathf.RoundToInt(obj.transform.position.z));
    }

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

                if (t.occupant != null && next != goal)
                    continue;

                visited.Add(next);
                cameFrom[next] = current;
                q.Enqueue(next);
            }
        }

        if (!cameFrom.ContainsKey(goal))
        {
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


