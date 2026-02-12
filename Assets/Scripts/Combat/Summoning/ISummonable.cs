using System.Collections;
using UnityEngine;

/// <summary>
/// Interface for summoned entities with lifecycle management.
/// </summary>
public interface ISummonable
{
    /// <summary>The entity that summoned this.</summary>
    ICombatEntity Owner { get; }

    /// <summary>Remaining turns before automatic despawn. -1 = permanent.</summary>
    int RemainingTurns { get; }

    /// <summary>The faction this summon belongs to.</summary>
    Faction SummonFaction { get; }

    /// <summary>Initialize the summon on the grid.</summary>
    void InitializeSummon(ICombatEntity owner, Vector2Int spawnPosition, GridManager grid);

    /// <summary>Execute this summon's AI turn.</summary>
    IEnumerator ExecuteSummonTurn();

    /// <summary>Remove the summon from play and clean up.</summary>
    void Despawn();
}
