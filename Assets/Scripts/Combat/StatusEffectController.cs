using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optional component for advanced status effect management.
/// Delegates to Stats for actual effect storage and processing.
/// Attach to entities that need richer status queries or external integration.
/// </summary>
[RequireComponent(typeof(Stats))]
public class StatusEffectController : MonoBehaviour
{
    private Stats stats;

    private void Awake()
    {
        stats = GetComponent<Stats>();
    }

    /// <summary>Add a new status effect.</summary>
    public void ApplyStatus(string name, int duration, int damagePerTurn)
    {
        stats.StatusDamage(name, duration, damagePerTurn);
    }

    /// <summary>Process all active effects at turn start. Returns total damage dealt.</summary>
    public int ProcessTurnStart()
    {
        int healthBefore = stats.health;
        stats.TakeStatusDamage();
        return Mathf.Max(0, healthBefore - stats.health);
    }

    /// <summary>Check if entity has a specific status.</summary>
    public bool HasStatus(string statusName)
    {
        return stats.HasStatus(statusName);
    }

    /// <summary>Remove all instances of a specific status.</summary>
    public void RemoveStatus(string statusName)
    {
        stats.RemoveStatus(statusName);
    }

    /// <summary>Clear all status effects.</summary>
    public void ClearAll()
    {
        stats.ClearAllStatuses();
    }

    /// <summary>Read-only view of active status effects.</summary>
    public IReadOnlyList<StatusEffect> ActiveEffects => stats.statuses.AsReadOnly();
}
