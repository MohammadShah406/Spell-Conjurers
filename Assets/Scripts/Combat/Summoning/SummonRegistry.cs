using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Tracks all active summons and orchestrates their turns.
/// Handles registration, lifecycle, and faction-ordered turn execution.
/// </summary>
public class SummonRegistry : MonoBehaviour
{
    public static SummonRegistry Instance { get; private set; }

    private List<ISummonable> activeSummons = new List<ISummonable>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>Register a new summon.</summary>
    public void Register(ISummonable summon)
    {
        if (summon == null || activeSummons.Contains(summon)) return;
        activeSummons.Add(summon);
        Debug.Log($"[SummonRegistry] Registered summon owned by {summon.Owner?.EntityName ?? "unknown"} ({activeSummons.Count} active)");
    }

    /// <summary>Unregister a summon.</summary>
    public void Unregister(ISummonable summon)
    {
        activeSummons.Remove(summon);
    }

    /// <summary>
    /// Execute all summon turns in order: player summons first, then enemy summons.
    /// </summary>
    public IEnumerator ExecuteAllSummonTurns()
    {
        var snapshot = new List<ISummonable>(activeSummons);

        // Player summons act first
        var sorted = snapshot
            .Where(s => s != null)
            .OrderBy(s => s.SummonFaction == Faction.PlayerSummon ? 0 : 1)
            .ToList();

        foreach (var summon in sorted)
        {
            if (summon == null) continue;

            if (summon.RemainingTurns == 0)
            {
                Debug.Log("[SummonRegistry] Summon expired, despawning.");
                summon.Despawn();
                continue;
            }

            yield return summon.ExecuteSummonTurn();
        }

        // Clean up null references
        activeSummons.RemoveAll(s => s == null);
    }

    /// <summary>Get all summons owned by a specific entity.</summary>
    public List<ISummonable> GetSummonsOwnedBy(ICombatEntity owner)
    {
        return activeSummons.Where(s => s != null && s.Owner == owner).ToList();
    }

    /// <summary>Get all summons of a specific faction.</summary>
    public List<ISummonable> GetSummonsByFaction(Faction faction)
    {
        return activeSummons.Where(s => s != null && s.SummonFaction == faction).ToList();
    }

    /// <summary>Number of active summons.</summary>
    public int ActiveCount => activeSummons.Count;

    /// <summary>Despawn all summons.</summary>
    public void DespawnAll()
    {
        var snapshot = new List<ISummonable>(activeSummons);
        foreach (var s in snapshot) s?.Despawn();
        activeSummons.Clear();
    }

    /// <summary>Despawn all summons owned by a specific entity.</summary>
    public void DespawnAllOwnedBy(ICombatEntity owner)
    {
        var toRemove = activeSummons.Where(s => s != null && s.Owner == owner).ToList();
        foreach (var s in toRemove) s?.Despawn();
        activeSummons.RemoveAll(s => s == null || toRemove.Contains(s));
    }
}
