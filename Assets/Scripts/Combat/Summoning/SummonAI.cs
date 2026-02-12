using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AI behavior system for summoned entities.
/// Determines movement and attack targets based on SummonAIType.
/// Fully integrated with the grid, pathfinding, and spell systems.
/// </summary>
public static class SummonAI
{
    /// <summary>Execute a summon's AI turn based on its AI type.</summary>
    public static IEnumerator ExecuteTurn(SummonedEntity summon, GridManager grid)
    {
        if (summon == null || !summon.IsAlive || grid == null) yield break;

        switch (summon.aiType)
        {
            case SummonAIType.Aggressive:
                yield return ExecuteAggressive(summon, grid);
                break;
            case SummonAIType.Defensive:
                yield return ExecuteDefensive(summon, grid);
                break;
            case SummonAIType.Stationary:
                yield return ExecuteStationary(summon, grid);
                break;
            case SummonAIType.Support:
                yield return ExecuteSupport(summon, grid);
                break;
        }
    }

    // ── AI Strategies ──

    private static IEnumerator ExecuteAggressive(SummonedEntity summon, GridManager grid)
    {
        GameObject target = FindNearestHostile(summon);
        if (target == null) yield break;

        Vector2Int targetPos = GetGridPosition(target);
        int dist = Pathfinding.ManhattanDistance(summon.GridPosition, targetPos);

        // Attack if in range
        if (dist <= GetAttackRange(summon) && summon.Spells.Length > 0)
        {
            var spell = summon.Spells[0];
            if (spell != null)
            {
                ActionManager.Instance?.UseSpell(spell, summon.gameObject, target);
                yield return new WaitForSeconds(0.5f);
                yield break;
            }
        }

        // Move toward target
        yield return MoveToward(summon, targetPos, grid);

        // Try to attack after moving
        dist = Pathfinding.ManhattanDistance(summon.GridPosition, targetPos);
        if (dist <= GetAttackRange(summon) && summon.Spells.Length > 0)
        {
            var spell = summon.Spells[0];
            if (spell != null)
            {
                ActionManager.Instance?.UseSpell(spell, summon.gameObject, target);
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    private static IEnumerator ExecuteDefensive(SummonedEntity summon, GridManager grid)
    {
        if (summon.Owner == null) yield break;

        // Attack nearby hostiles first
        GameObject nearbyTarget = FindNearestHostileInRange(summon, GetAttackRange(summon));
        if (nearbyTarget != null && summon.Spells.Length > 0)
        {
            ActionManager.Instance?.UseSpell(summon.Spells[0], summon.gameObject, nearbyTarget);
            yield return new WaitForSeconds(0.5f);
            yield break;
        }

        // Move toward owner if too far
        int distToOwner = Pathfinding.ManhattanDistance(summon.GridPosition, summon.Owner.GridPosition);
        if (distToOwner > 2)
        {
            yield return MoveToward(summon, summon.Owner.GridPosition, grid);
        }
    }

    private static IEnumerator ExecuteStationary(SummonedEntity summon, GridManager grid)
    {
        // Don't move, attack anything in range
        GameObject target = FindNearestHostileInRange(summon, GetAttackRange(summon));
        if (target != null && summon.Spells.Length > 0)
        {
            ActionManager.Instance?.UseSpell(summon.Spells[0], summon.gameObject, target);
            yield return new WaitForSeconds(0.5f);
        }
    }

    private static IEnumerator ExecuteSupport(SummonedEntity summon, GridManager grid)
    {
        if (summon.Owner == null) yield break;

        // Move toward owner if far
        int distToOwner = Pathfinding.ManhattanDistance(summon.GridPosition, summon.Owner.GridPosition);
        if (distToOwner > 2)
            yield return MoveToward(summon, summon.Owner.GridPosition, grid);

        // Heal owner if wounded and has support spell
        if (summon.Spells.Length > 0 && summon.Owner.Stats != null
            && summon.Owner.Stats.health < summon.Owner.Stats.maxHealth)
        {
            var spell = summon.Spells[0];
            if (spell != null && spell.support)
            {
                ActionManager.Instance?.UseSpell(spell, summon.gameObject, summon.Owner.GameObject);
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    // ── Movement ──

    private static IEnumerator MoveToward(SummonedEntity summon, Vector2Int target, GridManager grid)
    {
        var path = Pathfinding.FindPathToClosest(grid, summon.GridPosition, target, summon.gameObject);
        if (path == null || path.Count == 0) yield break;

        int stepsToTake = Mathf.Min(summon.moveRange, path.Count);

        // Free old tile
        Tile oldTile = grid.GetTile(summon.GridPosition);
        if (oldTile != null && oldTile.occupant == summon.gameObject)
            oldTile.occupant = null;

        for (int i = 0; i < stepsToTake; i++)
        {
            var nextPos = path[i];
            Tile nextTile = grid.GetTile(nextPos);
            if (nextTile == null || nextTile.occupant != null) break;

            Vector3 targetWorld = new Vector3(nextPos.x, summon.yPos, nextPos.y);
            Vector3 startPos = summon.transform.position;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * summon.moveSpeed;
                summon.transform.position = Vector3.Lerp(startPos, targetWorld, t);
                yield return null;
            }

            summon.transform.position = targetWorld;
            summon.GridPosition = nextPos;
        }

        // Occupy new tile
        Tile newTile = grid.GetTile(summon.GridPosition);
        if (newTile != null) newTile.occupant = summon.gameObject;
    }

    // ── Target Finding ──

    private static GameObject FindNearestHostile(SummonedEntity summon)
    {
        Faction myFaction = summon.Faction;
        float bestDist = float.MaxValue;
        GameObject best = null;

        // Check enemies
        if (myFaction.IsHostile(Faction.Enemy) && EnemyManager.Instance != null)
        {
            foreach (var enemy in EnemyManager.Instance.enemies)
            {
                if (enemy == null) continue;
                var stats = enemy.GetComponent<Stats>();
                if (stats == null || stats.isDead) continue;

                int dist = Pathfinding.ManhattanDistance(summon.GridPosition, enemy.gridPosition);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = enemy.gameObject;
                }
            }
        }

        // Check players
        if (myFaction.IsHostile(Faction.Player) && GameManager.Instance != null)
        {
            foreach (var playerObj in GameManager.Instance.players)
            {
                if (playerObj == null) continue;
                var stats = playerObj.GetComponent<Stats>();
                if (stats == null || stats.isDead) continue;

                var pf = playerObj.GetComponent<PlayerFunctionality>();
                if (pf == null) continue;

                int dist = Pathfinding.ManhattanDistance(summon.GridPosition, pf.gridPosition);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = playerObj;
                }
            }
        }

        return best;
    }

    private static GameObject FindNearestHostileInRange(SummonedEntity summon, int range)
    {
        var target = FindNearestHostile(summon);
        if (target == null) return null;

        int dist = Pathfinding.ManhattanDistance(summon.GridPosition, GetGridPosition(target));
        return dist <= range ? target : null;
    }

    private static int GetAttackRange(SummonedEntity summon)
    {
        if (summon.Spells != null && summon.Spells.Length > 0 && summon.Spells[0] != null)
            return summon.Spells[0].range;
        return summon.attackRange;
    }

    private static Vector2Int GetGridPosition(GameObject obj)
    {
        var pf = obj.GetComponent<PlayerFunctionality>();
        if (pf != null) return pf.gridPosition;
        var enemy = obj.GetComponent<Enemy>();
        if (enemy != null) return enemy.gridPosition;
        var summon = obj.GetComponent<SummonedEntity>();
        if (summon != null) return summon.GridPosition;
        return new Vector2Int(
            Mathf.RoundToInt(obj.transform.position.x),
            Mathf.RoundToInt(obj.transform.position.z));
    }
}
