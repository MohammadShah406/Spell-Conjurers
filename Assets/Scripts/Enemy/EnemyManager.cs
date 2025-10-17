using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

public class EnemyManager : MonoBehaviour
{
    public List<Enemy> enemies = new List<Enemy>();
    private Player[] players;
    float[,] threatGrid;
    private bool isTakingTurn;
    public Transform player;
    private Dictionary<GameObject, float> playerMaxDamageList = new Dictionary<GameObject, float>();
    private GridManager gridManager;

    public void AddEnemy(Enemy enemy)
    {
        enemies.Add(enemy);
    }

    public void StartEnemyTurns()
    {
        if (!isTakingTurn)
            StartCoroutine(EnemyTurnRoutine());
    }

    public IEnumerator StartEnemyTurnsCoroutine()
    {
        if (isTakingTurn)
            yield break;

        isTakingTurn = true;
        yield return StartCoroutine(EnemyTurnRoutine());
        isTakingTurn = false;
    }
    private IEnumerator EnemyTurnRoutine()
    {
        isTakingTurn = true;

        foreach (var enemy in enemies)
        {
            enemy.GetComponent<Stats>().TakeStatusDamage();
            yield return enemy.TakeTurn(() => { });
            yield return new WaitForSeconds(0.25f);
        }

        isTakingTurn = false;
        Debug.Log("All enemies finished their turns!");
    }

    public void initializeThreatGrid( int height, int width)
    {
        gridManager = GameObject.FindFirstObjectByType<GridManager>();
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            playerMaxDamageList.Add(player, 30);
        }

        threatGrid = new float[height, width];
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
                threatGrid[i, j] = 0;
        }
    }

    public void clearThreatGrid()
    {
        for (int i = 0; i < threatGrid.GetLength(0); i++)
        {
            for (int j = 0; j < threatGrid.GetLength(1); j++)
                threatGrid[i, j] = 0;
        }

    }
    public void caculateThreatGrid()
    {
        clearThreatGrid();
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            PlayerFunctionality playerFunctionality = player.GetComponent<PlayerFunctionality>();
            if (playerFunctionality != null)
            {
                foreach (Tile tile in gridManager.grid)
                {
                    if (tile == null) continue;

                    int distance = Mathf.Abs(tile.gridPosition.x - playerFunctionality.gridPosition.x) + Mathf.Abs(tile.gridPosition.y - playerFunctionality.gridPosition.y);
                    if (distance <= playerFunctionality.moveRange && tile.occupant == null)
                    {
                        if (playerMaxDamageList.ContainsKey(player))
                        {
                            if (playerMaxDamageList.TryGetValue(player, out float dmg))
                                threatGrid[tile.gridPosition.x, tile.gridPosition.y] = threatGrid[tile.gridPosition.x, tile.gridPosition.y] + dmg;
                        }
                    }
                }
            }
        }
    }

    public void UpdateMaxDamage(GameObject player, float damage)
    {
        if (playerMaxDamageList.ContainsKey(player))
        {
            playerMaxDamageList[player] = Mathf.Max(playerMaxDamageList[player], damage);
        }
    }

    public void showGrid()
    {
        int rows = threatGrid.GetLength(0);
        int cols = threatGrid.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            string rowStr = "";
            for (int j = 0; j < cols; j++)
            {
                rowStr += threatGrid[i, j].ToString("F2") + "\t"; // F2 formats to 2 decimal places
            }
            Debug.Log(rowStr);
        }
    }
}
