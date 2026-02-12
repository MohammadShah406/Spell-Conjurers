using System.Collections;
using UnityEngine;

/// <summary>
/// Combat action that summons an entity onto the grid.
/// </summary>
public class SummonAction : ICombatAction
{
    public string ActionName => $"Summon at {spawnPosition}";
    public ICombatEntity Actor { get; }
    public int ActionPointCost => 1;
    public int Priority => 5; // Summons resolve after movement and spells

    private readonly GameObject summonPrefab;
    private readonly Vector2Int spawnPosition;
    private readonly GridManager gridManager;
    private readonly int duration;
    private readonly Spell sourceSpell;

    public SummonAction(
        ICombatEntity actor, GameObject summonPrefab, Vector2Int spawnPos,
        GridManager grid, int duration = 3, Spell sourceSpell = null)
    {
        Actor = actor;
        this.summonPrefab = summonPrefab;
        spawnPosition = spawnPos;
        gridManager = grid;
        this.duration = duration;
        this.sourceSpell = sourceSpell;
    }

    public bool CanExecute()
    {
        if (!Actor.IsAlive || summonPrefab == null) return false;
        if (Actor.ActionPoints < ActionPointCost) return false;

        Tile tile = gridManager.GetTile(spawnPosition);
        if (tile == null || !tile.walkable || tile.occupant != null) return false;

        if (sourceSpell != null && Actor.Stats.resource < sourceSpell.resourceCost)
            return false;

        return true;
    }

    public IEnumerator Execute()
    {
        if (!CanExecute()) yield break;

        Actor.ActionPoints -= ActionPointCost;

        if (sourceSpell != null)
        {
            Actor.Stats.resource -= sourceSpell.resourceCost;
            Actor.Stats.UpdateStatsHolder();
        }

        var summonObj = Object.Instantiate(summonPrefab);
        summonObj.name = $"{Actor.EntityName}_Summon";

        var summon = summonObj.GetComponent<SummonedEntity>();
        if (summon != null)
        {
            summon.maxLifetimeTurns = duration;
            summon.InitializeSummon(Actor, spawnPosition, gridManager);
        }
        else
        {
            Debug.LogError("[SummonAction] Prefab missing SummonedEntity component!");
            Object.Destroy(summonObj);
        }

        yield return new WaitForSeconds(0.5f);
    }
}
