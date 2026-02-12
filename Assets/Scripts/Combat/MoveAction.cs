using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Combat action for grid-based movement.
/// Validates path availability, moves entity along BFS path, updates tile occupancy.
/// </summary>
public class MoveAction : ICombatAction
{
    public string ActionName => $"Move to {targetPosition}";
    public ICombatEntity Actor { get; }
    public int ActionPointCost => 1;
    public int Priority => 20; // Movement executes before spells

    private readonly Vector2Int targetPosition;
    private readonly GridManager gridManager;
    private readonly float moveSpeed;
    private readonly float yPos;

    public MoveAction(
        ICombatEntity actor, Vector2Int target, GridManager grid,
        float moveSpeed = 5f, float yPos = 1.5f)
    {
        Actor = actor;
        targetPosition = target;
        gridManager = grid;
        this.moveSpeed = moveSpeed;
        this.yPos = yPos;
    }

    public bool CanExecute()
    {
        if (!Actor.IsAlive) return false;
        if (Actor.ActionPoints < ActionPointCost) return false;
        if (Actor.GridPosition == targetPosition) return false;

        Tile tile = gridManager.GetTile(targetPosition);
        if (tile == null || !tile.walkable || tile.occupant != null) return false;

        var path = Pathfinding.FindPath(gridManager, Actor.GridPosition, targetPosition, Actor.GameObject);
        return path != null && path.Count > 0;
    }

    public IEnumerator Execute()
    {
        if (!CanExecute()) yield break;

        var path = Pathfinding.FindPath(gridManager, Actor.GridPosition, targetPosition, Actor.GameObject);
        if (path == null || path.Count == 0) yield break;

        Actor.ActionPoints -= ActionPointCost;

        Transform transform = Actor.GameObject.transform;

        // Free old tile
        Tile oldTile = gridManager.GetTile(Actor.GridPosition);
        if (oldTile != null && oldTile.occupant == Actor.GameObject)
            oldTile.occupant = null;

        // Walk along path
        foreach (var step in path)
        {
            Vector3 target = new Vector3(step.x, yPos, step.y);

            while (Vector3.Distance(transform.position, target) > 0.01f)
            {
                Vector3 direction = target - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(direction.normalized, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation, lookRot, 720f * Time.deltaTime);
                }

                transform.position = Vector3.MoveTowards(
                    transform.position, target, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = target;
        }

        // Occupy new tile
        Actor.GridPosition = targetPosition;
        Tile newTile = gridManager.GetTile(targetPosition);
        if (newTile != null) newTile.occupant = Actor.GameObject;
    }
}
