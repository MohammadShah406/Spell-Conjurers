using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Unified turn manager for the combat system.
/// Manages phase-based turns: Player → Summons → Enemies → Round End.
/// Supports entity registration, action point tracking, and lifecycle events.
/// </summary>
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("References")]
    public EnemyManager enemyManager;

    [Header("Turn Settings")]
    public float enemyActionDelay = 0.5f;
    public float waitDuration = 1f;
    public bool waitButton = true;
    public bool waitTurn = false;

    [Header("Events")]
    public UnityEvent onPlayerTurnStart;
    public UnityEvent onEnemyTurnStart;
    public UnityEvent onRoundEnd;
    public UnityEvent onRoundStart;

    // --- State ---
    public TurnState currentState { get; private set; } = TurnState.PlayerTurn;
    private bool isTakingTurn = false;
    private bool buttonPressed = false;
    private int currentRound = 0;

    /// <summary>All entities registered in the combat.</summary>
    private List<ICombatEntity> combatEntities = new List<ICombatEntity>();

    public enum TurnState
    {
        PlayerTurn,
        EnemyTurn,
        SummonTurn,
        Waiting,
        RoundEnd
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (enemyManager == null)
            enemyManager = EnemyManager.Instance;
    }

    // ──────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────

    /// <summary>Convenience property for scripts checking turn state.</summary>
    public bool IsPlayerTurn => currentState == TurnState.PlayerTurn;

    /// <summary>Current round number.</summary>
    public int CurrentRound => currentRound;

    /// <summary>Called when the player ends their turn (button press or after casting).</summary>
    public void EndPlayerTurn()
    {
        if (currentState != TurnState.PlayerTurn) return;

        Debug.Log("[TurnManager] Player turn ended.");
        ClearPlayerUI();

        currentState = TurnState.EnemyTurn;
        onEnemyTurnStart?.Invoke();

        if (GameManager.Instance.players.Count == 0)
        {
            Debug.Log("[TurnManager] No players. Cannot proceed.");
            return;
        }

        StartCoroutine(HandlePostPlayerPhase());
        GameManager.Instance.CheckGameState();
    }

    /// <summary>Called by UI button to advance when waitButton mode is active.</summary>
    public void WaitButtonPressed()
    {
        if (currentState == TurnState.PlayerTurn)
            EndPlayerTurn();
        else if (currentState == TurnState.EnemyTurn)
            buttonPressed = true;
    }

    /// <summary>Register an entity for combat tracking.</summary>
    public void RegisterEntity(ICombatEntity entity)
    {
        if (entity != null && !combatEntities.Contains(entity))
        {
            combatEntities.Add(entity);
            Debug.Log($"[TurnManager] Registered entity: {entity.EntityName} ({entity.Faction})");
        }
    }

    /// <summary>Unregister an entity (death, despawn).</summary>
    public void UnregisterEntity(ICombatEntity entity)
    {
        combatEntities.Remove(entity);
    }

    /// <summary>Get all registered entities of a faction.</summary>
    public List<ICombatEntity> GetEntitiesByFaction(Faction faction)
    {
        return combatEntities.Where(e => e != null && e.IsAlive && e.Faction == faction).ToList();
    }

    /// <summary>Get all alive entities.</summary>
    public List<ICombatEntity> GetAllAliveEntities()
    {
        return combatEntities.Where(e => e != null && e.IsAlive).ToList();
    }

    // ──────────────────────────────────────────────
    // Phase Execution
    // ──────────────────────────────────────────────

    private IEnumerator HandlePostPlayerPhase()
    {
        if (GameManager.Instance.players.Count == 0) yield break;

        // Phase 1: Summons act
        if (SummonRegistry.Instance != null)
        {
            currentState = TurnState.SummonTurn;
            yield return SummonRegistry.Instance.ExecuteAllSummonTurns();
        }

        // Phase 2: Enemies act
        currentState = TurnState.EnemyTurn;
        yield return StartCoroutine(ExecuteEnemyPhase());

        // Phase 3: Post-combat check
        GameManager.Instance.CheckGameState();
        if (GameManager.Instance.lostGame || GameManager.Instance.GameEnded)
            yield break;

        // Phase 4: Begin new round
        currentRound++;
        onRoundEnd?.Invoke();

        Debug.Log($"[TurnManager] Round {currentRound} complete. Player turn begins.");
        currentState = TurnState.PlayerTurn;
        onPlayerTurnStart?.Invoke();
        StartPlayerTurn();
    }

    private IEnumerator ExecuteEnemyPhase()
    {
        if (isTakingTurn) yield break;
        isTakingTurn = true;
        buttonPressed = false;

        // Snapshot to handle mid-turn death
        var snapshot = new List<Enemy>(enemyManager.enemies);

        foreach (var enemy in snapshot)
        {
            if (enemy == null || !enemy.gameObject.activeSelf) continue;

            var stats = enemy.GetComponent<Stats>();
            if (stats == null || stats.health <= 0) continue;

            // Status damage at start of enemy's individual turn
            stats.TakeStatusDamage();

            // Enemy AI turn
            yield return enemy.TakeTurn(() => { });

            // Pacing between enemy actions
            if (waitTurn)
            {
                yield return new WaitForSeconds(waitDuration);
            }
            else if (waitButton)
            {
                while (!buttonPressed)
                    yield return null;
                buttonPressed = false;
            }
            else
            {
                yield return new WaitForSeconds(enemyActionDelay);
            }
        }

        isTakingTurn = false;
        Debug.Log("[TurnManager] All enemies finished their turns.");
    }

    private void StartPlayerTurn()
    {
        if (GameManager.Instance.players.Count == 0) return;

        var player = GameManager.Instance.players[0];
        if (player != null)
        {
            var pf = player.GetComponent<PlayerFunctionality>();
            if (pf != null) pf.OnTurnStart();
        }

        // Lower enemy camera priorities
        if (enemyManager != null)
        {
            foreach (var enemy in enemyManager.enemies)
            {
                if (enemy != null && enemy.virtualCamera != null)
                    enemy.virtualCamera.Priority = 9;
            }
        }

        // Reset player movement state
        var playerFunc = FindAnyObjectByType<PlayerFunctionality>();
        if (playerFunc != null) playerFunc.ResetTurn();
    }

    private void ClearPlayerUI()
    {
        if (GameManager.Instance?.players == null) return;

        foreach (var playerObj in GameManager.Instance.players)
        {
            if (playerObj == null) continue;
            var pf = playerObj.GetComponent<PlayerFunctionality>();
            if (pf != null) pf.ResetTurn();
        }
    }

    /// <summary>Legacy coroutine entry point for backward compatibility.</summary>
    public IEnumerator StartEnemyTurnsCoroutine()
    {
        yield return ExecuteEnemyPhase();
    }
}
