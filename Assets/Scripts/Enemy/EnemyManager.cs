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
    private Dictionary<GameObject, int[]> playerMaxDamageList = new Dictionary<GameObject, int[]>();
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

    public void initializeThreatGrid(int height, int width)
    {
        gridManager = GameObject.FindFirstObjectByType<GridManager>();
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        //eM_PlayerDatas = new EM_PlayerData[4];

        foreach (GameObject player in players)
        {
            int[] playerData = { 30, 1 };
            playerMaxDamageList.Add(player, playerData);
        }

        /* for (int i = 0; i < players.Length; i++)
         {
             eM_PlayerDatas[i].player = players[i];
             eM_PlayerDatas[i].maxDamage = 30;
             eM_PlayerDatas[i].maxRange = 1;
         }*/

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
                    if (playerMaxDamageList.TryGetValue(player, out int[] playerData))
                    {
                        int distance = Mathf.Abs(tile.gridPosition.x - playerFunctionality.gridPosition.x) + Mathf.Abs(tile.gridPosition.y - playerFunctionality.gridPosition.y);
                        if (distance <= playerFunctionality.moveRange + playerData[1] && tile.occupant == null && playerMaxDamageList.ContainsKey(player))
                        {
                            threatGrid[tile.gridPosition.x, tile.gridPosition.y] = threatGrid[tile.gridPosition.x, tile.gridPosition.y] + playerData[0];
                        }
                    }
                }
            }
        }
        /* for (int i = 0; i <= players.Length; i++)
         {
             PlayerFunctionality playerFunctionality = players[i].GetComponent<PlayerFunctionality>();
             if (playerFunctionality != null)
             {
                 foreach (Tile tile in gridManager.grid)
                 {
                     if (tile == null) continue;

                     int distance = Mathf.Abs(tile.gridPosition.x - playerFunctionality.gridPosition.x) + Mathf.Abs(tile.gridPosition.y - playerFunctionality.gridPosition.y);
                     if (distance <= playerFunctionality.moveRange + eM_PlayerDatas[i].maxRange && tile.occupant == null)
                     {
                         threatGrid[tile.gridPosition.x, tile.gridPosition.y] = threatGrid[tile.gridPosition.x, tile.gridPosition.y] + eM_PlayerDatas[i].maxDamage;
                     }
                 }
             }

         }*/
    }

    public void UpdateMaxDamage(GameObject player, int damage, int range = 1)
    {
        if (playerMaxDamageList.ContainsKey(player))
        {
            int[] playerData = { damage, range };
            playerMaxDamageList[player][0] = Mathf.Max(playerMaxDamageList[player][0], damage);
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
                rowStr += threatGrid[i, j].ToString("F2") + "\t";
            }
            Debug.Log(rowStr);
        }
    }

    public void calculateScore(Enemy enemy)
    {
        foreach (Tile tile in gridManager.grid)
        {
            if (tile == null) continue;
            int distance = Mathf.Abs(tile.gridPosition.x - enemy.gridPosition.x) + Mathf.Abs(tile.gridPosition.y - enemy.gridPosition.y);
            if (distance <= enemy.moveRange && tile.occupant == null)
            {
                foreach (Spell spell in enemy.spells)
                {
                    if (spell == null) continue;
                    
                }
            }
        }
    }
}
