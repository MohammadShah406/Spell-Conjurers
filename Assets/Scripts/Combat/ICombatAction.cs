using System.Collections;
using UnityEngine;

/// <summary>
/// A discrete action in the turn-based combat system.
/// Cast spell, move, summon, pass — all implement this.
/// </summary>
public interface ICombatAction
{
    /// <summary>Human-readable name for logging and UI.</summary>
    string ActionName { get; }

    /// <summary>The entity performing this action.</summary>
    ICombatEntity Actor { get; }

    /// <summary>AP cost to perform this action.</summary>
    int ActionPointCost { get; }

    /// <summary>
    /// Priority for action ordering within a turn. Higher = earlier.
    /// Movement (20) > Spells (10) > Summons (5).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Returns true if the action can execute right now
    /// (AP check, range, resource, cooldown, target alive, etc.).
    /// </summary>
    bool CanExecute();

    /// <summary>
    /// Execute the action. Coroutine to support animations/projectiles.
    /// </summary>
    IEnumerator Execute();
}
