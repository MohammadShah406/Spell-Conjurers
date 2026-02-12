using UnityEngine;

/// <summary>
/// Implemented by any entity participating in the turn queue:
/// players, enemies, and summons.
/// </summary>
public interface ICombatEntity
{
    string EntityName { get; }
    GameObject GameObject { get; }
    Vector2Int GridPosition { get; set; }
    Stats Stats { get; }

    /// <summary>Faction for allegiance, targeting, and AI.</summary>
    Faction Faction { get; }

    /// <summary>Owner entity for summons; null for non-summons.</summary>
    ICombatEntity Owner { get; }

    /// <summary>Whether the entity is alive and in play.</summary>
    bool IsAlive { get; }

    /// <summary>Initiative for turn ordering. Higher values go first.</summary>
    int Initiative { get; }

    /// <summary>Action points remaining this turn.</summary>
    int ActionPoints { get; set; }

    /// <summary>Maximum action points per turn.</summary>
    int MaxActionPoints { get; }

    /// <summary>The spells available to this entity.</summary>
    Spell[] Spells { get; }

    /// <summary>Per-spell cooldown tracker. Indexed by spell array index.</summary>
    int[] SpellCooldowns { get; }

    /// <summary>Called at the start of this entity's turn.</summary>
    void OnTurnStart();

    /// <summary>Called at the end of this entity's turn.</summary>
    void OnTurnEnd();
}

public enum Faction
{
    Player,
    Enemy,
    Neutral,
    /// <summary>Summons aligned to the player faction.</summary>
    PlayerSummon,
    /// <summary>Summons aligned to the enemy faction.</summary>
    EnemySummon
}

/// <summary>
/// Extension methods for Faction allegiance queries.
/// </summary>
public static class FactionExtensions
{
    /// <summary>Returns true if faction a and b are allied.</summary>
    public static bool IsAlly(this Faction a, Faction b)
    {
        if (a == b) return true;
        if (a == Faction.Player && b == Faction.PlayerSummon) return true;
        if (a == Faction.PlayerSummon && b == Faction.Player) return true;
        if (a == Faction.Enemy && b == Faction.EnemySummon) return true;
        if (a == Faction.EnemySummon && b == Faction.Enemy) return true;
        return false;
    }

    /// <summary>Returns true if faction a and b are hostile to each other.</summary>
    public static bool IsHostile(this Faction a, Faction b)
    {
        if (a == Faction.Neutral || b == Faction.Neutral) return false;
        return !IsAlly(a, b);
    }
}
