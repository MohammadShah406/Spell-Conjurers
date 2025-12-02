using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.Events;

public class TurnManager : MonoBehaviour
{
    public EnemyManager enemyManager;
    public TurnState currentState = TurnState.PlayerTurn;

    public bool waitButton = true; //True = enemy turn starts after player presses button
    public bool waitTurn = false; //False = enemy turn proceeds immediately after each other
    public float waitDuration = 1f;
    private bool buttonPressed = false;

    private bool isTakingTurn;


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
            enemyManager = EnemyManager.Instance;
        }
    }

    void Update()
    {
        
    }

    public void EndPlayerTurn()
    {
        if (currentState != TurnState.PlayerTurn) return;

        Debug.Log("Player turn ended. Starting enemy turn...");

        // Immediately clear player UI/selection/highlights so tiles are not left visible
        if (GameManager.Instance != null && GameManager.Instance.players != null)
        {
            foreach (var playerObj in GameManager.Instance.players)
            {
                if (playerObj == null) continue;
                var pf = playerObj.GetComponent<PlayerFunctionality>();
                if (pf != null)
                {
                    // ResetTurn clears highlighted tiles and selected spell
                    pf.ResetTurn();
                }
            }
        }

        currentState = TurnState.EnemyTurn;

        if (GameManager.Instance.players.Count == 0)
        {
            Debug.Log("No players found. Cannot continue with turns.");
            return;
        }
        StartCoroutine(HandleEnemyTurn());

        GameManager.Instance.CheckGameState();
    }

    private IEnumerator HandleEnemyTurn()
    {

        if(GameManager.Instance.players.Count == 0)
        {
            Debug.Log("No players found. Skipping enemy turn.");
            yield break;
        }

        // Tell the enemies to act
        yield return StartCoroutine(StartEnemyTurnsCoroutine());

        GameManager.Instance.CheckGameState();
        Debug.Log("Enemy turn complete. Back to player turn.");
        currentState = TurnState.PlayerTurn;


        if (GameManager.Instance.players.Count == 0)
        {
            Debug.Log("No players found. Cannot start player turn.");
            yield break;
        }
        else
        {
            GameManager.Instance.players[0].GetComponent<PlayerFunctionality>().OnTurnStart();

            foreach (var enemy in EnemyManager.Instance.enemies)
            {
                enemy.GetComponent<Enemy>().virtualCamera.Priority = 9;
            }

            // Reset player movement for next turn
            var player = FindAnyObjectByType<PlayerFunctionality>();
            if (player != null)
                player.ResetTurn();
        }
           
    }

    public void WaitButtonPressed()
    {
        if (TurnManager.Instance.currentState == TurnManager.TurnState.PlayerTurn)
            EndPlayerTurn();
        else if(TurnManager.Instance.currentState == TurnManager.TurnState.EnemyTurn)
            buttonPressed = true;
    }

    public IEnumerator StartEnemyTurnsCoroutine()
    {
        if (isTakingTurn)
            yield break;

        isTakingTurn = true;
        yield return StartCoroutine(EnemyTurnRoutine());
        isTakingTurn = false;
    }

    //private IEnumerator EnemyTurnRoutine()
    //{
    //    buttonPressed = false;
    //    isTakingTurn = true;

    //    foreach (var enemy in EnemyManager.Instance.enemies)
    //    {
    //        if (!enemy.gameObject.activeSelf)
    //        {
    //            continue;
    //        }
    //        enemy.GetComponent<Stats>().TakeStatusDamage();
    //        yield return enemy.TakeTurn(() => { });

    //        if (waitTurn)
    //        {
    //            yield return new WaitForSeconds(waitDuration);
    //        }
    //        else if (waitButton)
    //        {
    //            while (!buttonPressed)
    //            {
    //                yield return null;
    //            }
    //            buttonPressed = false;
    //        }
    //    }

    //    isTakingTurn = false;
    //    Debug.Log("All enemies finished their turns!");
    //}

    private IEnumerator EnemyTurnRoutine()
    {
        buttonPressed = false;
        isTakingTurn = true;



        // 🔥 iterate over a copy so enemies can safely die / be removed
        var enemiesSnapshot = new List<Enemy>(EnemyManager.Instance.enemies);

        foreach (var enemy in enemiesSnapshot)
        {
            if (enemy == null || !enemy.gameObject.activeSelf)
                continue;

            var stats = enemy.GetComponent<Stats>();
            if (stats == null || stats.health <= 0)
                continue;

            stats.TakeStatusDamage();

            // Enemy may die during this call
            yield return enemy.GetComponent<Enemy>().TakeTurn(() => { });

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
        }

        isTakingTurn = false;
        Debug.Log("All enemies finished their turns!");
    }


}
