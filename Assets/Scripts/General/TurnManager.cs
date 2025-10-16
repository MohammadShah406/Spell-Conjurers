using NUnit.Framework.Interfaces;
using System.Collections;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public EnemyManager enemyManager;
    public TurnState currentState = TurnState.PlayerTurn;
    public static TurnManager Instance { get; private set; }
    public enum TurnState
    {
        PlayerTurn,
        EnemyTurn,
        Waiting
    }

    private void Awake()
    {
        Instance = this;
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    void Start()
    {
        if (enemyManager == null)
        {
            Debug.Log("TurnManager: enemyManager not found. Attempting to find");
            enemyManager = GameObject.FindAnyObjectByType<EnemyManager>();
        }
    }

    void Update()
    {
        // For switch turn
        if (Input.GetKeyDown(KeyCode.T))
        {
            EndPlayerTurn();
        }
    }

    public void EndPlayerTurn()
    {
        if (currentState != TurnState.PlayerTurn) return;

        Debug.Log("Player turn ended. Starting enemy turn...");
        currentState = TurnState.EnemyTurn;
        StartCoroutine(HandleEnemyTurn());
    }

    private IEnumerator HandleEnemyTurn()
    {
        // Tell the enemies to act
        yield return StartCoroutine(enemyManager.StartEnemyTurnsCoroutine());

        Debug.Log("Enemy turn complete. Back to player turn.");
        currentState = TurnState.PlayerTurn;

        // Reset player movement for next turn
        var player = FindAnyObjectByType<PlayerFunctionality>();
        if (player != null)
            player.ResetTurn();
    }
}
