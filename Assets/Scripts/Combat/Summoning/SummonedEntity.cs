using System.Collections;
using UnityEngine;

/// <summary>
/// Base summoned entity with lifecycle, AI behavior, and full ICombatEntity integration.
/// Extend this for specific summon behaviors or configure via Inspector fields.
/// </summary>
[RequireComponent(typeof(Stats))]
public class SummonedEntity : MonoBehaviour, ISummonable, ICombatEntity
{
    // ── ISummonable ──
    public ICombatEntity Owner { get; private set; }
    public int RemainingTurns { get; private set; } = -1;
    public Faction SummonFaction => Owner != null
        ? (Owner.Faction == Faction.Player ? Faction.PlayerSummon : Faction.EnemySummon)
        : Faction.Neutral;

    // ── ICombatEntity ──
    public string EntityName => gameObject.name;
    public GameObject GameObject => gameObject;
    public Vector2Int GridPosition { get; set; }
    public Stats Stats { get; private set; }
    public Faction Faction => SummonFaction;
    ICombatEntity ICombatEntity.Owner => Owner;
    public bool IsAlive => Stats != null && !Stats.isDead;
    public int Initiative => 5;
    public int ActionPoints { get; set; }
    public int MaxActionPoints => 1;
    public Spell[] Spells => summonSpells;
    public int[] SpellCooldowns => spellCooldowns;

    [Header("Summon Settings")]
    public int maxLifetimeTurns = 3;
    public float yPos = 1.5f;
    public int attackRange = 1;
    public float moveSpeed = 4f;
    public int moveRange = 2;

    [Header("Summon Spells")]
    public Spell[] summonSpells = new Spell[0];
    private int[] spellCooldowns = new int[0];

    [Header("AI")]
    public SummonAIType aiType = SummonAIType.Aggressive;

    private GridManager gridManager;

    private void Awake()
    {
        Stats = GetComponent<Stats>();
    }

    public void InitializeSummon(ICombatEntity owner, Vector2Int spawnPosition, GridManager grid)
    {
        Owner = owner;
        GridPosition = spawnPosition;
        gridManager = grid;
        RemainingTurns = maxLifetimeTurns;
        ActionPoints = MaxActionPoints;

        transform.position = new Vector3(spawnPosition.x, yPos, spawnPosition.y);

        var tile = grid.GetTile(spawnPosition);
        if (tile != null) tile.occupant = gameObject;

        // Initialize cooldown array
        if (summonSpells != null && summonSpells.Length > 0)
            spellCooldowns = new int[summonSpells.Length];

        // Register with systems
        SummonRegistry.Instance?.Register(this);
        TurnManager.Instance?.RegisterEntity(this);

        Debug.Log($"[SummonedEntity] '{EntityName}' summoned by {owner.EntityName} at {spawnPosition}, lifetime={maxLifetimeTurns}");
    }

    public virtual IEnumerator ExecuteSummonTurn()
    {
        if (!IsAlive)
        {
            Despawn();
            yield break;
        }

        // Tick lifetime
        if (RemainingTurns > 0) RemainingTurns--;

        if (RemainingTurns == 0)
        {
            Debug.Log($"[SummonedEntity] '{EntityName}' lifetime expired.");
            Despawn();
            yield break;
        }

        ActionPoints = MaxActionPoints;

        // Execute AI behavior
        yield return SummonAI.ExecuteTurn(this, gridManager);
    }

    public void Despawn()
    {
        Debug.Log($"[SummonedEntity] '{EntityName}' despawning.");

        if (gridManager != null)
        {
            var tile = gridManager.GetTile(GridPosition);
            if (tile != null && tile.occupant == gameObject)
                tile.occupant = null;
        }

        SummonRegistry.Instance?.Unregister(this);
        TurnManager.Instance?.UnregisterEntity(this);
        Destroy(gameObject);
    }

    public void OnTurnStart()
    {
        ActionPoints = MaxActionPoints;
        Stats?.TakeStatusDamage();

        // Tick cooldowns
        if (spellCooldowns != null)
        {
            for (int i = 0; i < spellCooldowns.Length; i++)
            {
                if (spellCooldowns[i] > 0) spellCooldowns[i]--;
            }
        }

        if (!IsAlive) Despawn();
    }

    public void OnTurnEnd() { }
}

/// <summary>
/// Types of AI behavior for summoned entities.
/// </summary>
public enum SummonAIType
{
    /// <summary>Move toward and attack nearest hostile.</summary>
    Aggressive,
    /// <summary>Stay near owner and defend.</summary>
    Defensive,
    /// <summary>Stay in place, attack anything in range.</summary>
    Stationary,
    /// <summary>Follow owner, heal/buff allies.</summary>
    Support
}
