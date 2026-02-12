using System.Collections;
using UnityEngine;

/// <summary>
/// Combat action that casts a spell from one entity to another.
/// Validates range, resources, cooldown, then delegates to ActionManager for execution.
/// </summary>
public class CastSpellAction : ICombatAction
{
    public string ActionName => $"Cast {spell.name}";
    public ICombatEntity Actor { get; }
    public int ActionPointCost => 1;
    public int Priority => 10;

    private readonly Spell spell;
    private readonly GameObject target;
    private readonly int spellIndex;

    public CastSpellAction(ICombatEntity actor, Spell spell, GameObject target, int spellIndex = -1)
    {
        Actor = actor;
        this.spell = spell;
        this.target = target;
        this.spellIndex = spellIndex;
    }

    public bool CanExecute()
    {
        if (spell == null || target == null || !Actor.IsAlive) return false;

        // Resource check
        Stats actorStats = Actor.Stats;
        if (actorStats == null || actorStats.resource < spell.resourceCost) return false;

        // Cooldown check
        if (spellIndex >= 0 && Actor.SpellCooldowns != null && spellIndex < Actor.SpellCooldowns.Length)
        {
            if (Actor.SpellCooldowns[spellIndex] > 0) return false;
        }

        // AP check
        if (Actor.ActionPoints < ActionPointCost) return false;

        // Range check (Manhattan distance)
        Vector2Int targetGrid = GetTargetGridPosition();
        int dist = Pathfinding.ManhattanDistance(Actor.GridPosition, targetGrid);
        if (dist > spell.range) return false;

        return true;
    }

    public IEnumerator Execute()
    {
        if (!CanExecute()) yield break;

        // Deduct resource
        Actor.Stats.resource -= spell.resourceCost;
        Actor.Stats.UpdateStatsHolder();

        // Deduct AP
        Actor.ActionPoints -= ActionPointCost;

        // Apply cooldown
        if (spellIndex >= 0 && Actor.SpellCooldowns != null && spellIndex < Actor.SpellCooldowns.Length)
            Actor.SpellCooldowns[spellIndex] = spell.cooldown;

        // Delegate to ActionManager for spell execution + projectile
        ActionManager.Instance.UseSpell(spell, Actor.GameObject, target);

        // Brief pause for visual feedback
        yield return new WaitForSeconds(0.3f);
    }

    private Vector2Int GetTargetGridPosition()
    {
        var pf = target.GetComponent<PlayerFunctionality>();
        if (pf != null) return pf.gridPosition;

        var enemy = target.GetComponent<Enemy>();
        if (enemy != null) return enemy.gridPosition;

        var summon = target.GetComponent<SummonedEntity>();
        if (summon != null) return summon.GridPosition;

        return new Vector2Int(
            Mathf.RoundToInt(target.transform.position.x),
            Mathf.RoundToInt(target.transform.position.z));
    }
}
