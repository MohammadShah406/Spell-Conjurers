using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
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
    private int widthGrid;
    private int heightGrid;

    public float WD = 1.0f;    // damage
    public float WK = 5.0f;    // sure kill
    public float WA = 2.0f;    // target priority
    public float WDb = 0.5f;    // proximity
    public float WR = 1.5f;   // efficiency
    public float WTh = 0.8f; //Threat avoidance 


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
        widthGrid = width;
        heightGrid = height;
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

    public Dictionary<String, object> calculateScore(Enemy enemy)
    {
        Dictionary<String, object> result = new Dictionary<String, object>();
        float score = 0;
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            Stats playerStats = player.GetComponent<Stats>();
            PlayerFunctionality playerFunctionality = player.GetComponent<PlayerFunctionality>();
            int distance = ManhattanDistance(enemy.gridPosition, playerFunctionality.gridPosition);
            foreach (Spell spell in enemy.spells)
            {
                if (distance <= spell.range + enemy.moveRange)
                {
                    List<Vector2Int> Locations = new List<Vector2Int>();
                    Locations = GetPositionsWithinManhattanDistance(playerFunctionality.gridPosition, spell.range, widthGrid, heightGrid);
                    foreach (Vector2Int location in Locations)
                    {
                        int moveDistance = ManhattanDistance(location, enemy.gridPosition);
                        if (moveDistance <= enemy.moveRange)
                        {
                            float Damage = spell.damage * (spell.accuracy / 100);
                            float KillBonus = (playerStats.health - Damage) <= 0 ? 1 : 0;
                            float supportTarget = (playerStats.type == Stats.Type.Support) ? 1 : 0;
                            float distanceBonus = (enemy.moveRange - moveDistance) / enemy.moveRange;
                            float resourceBonus = (Damage / spell.resourceCost) * 0.01f;
                            float Threat = threatGrid[location.x, location.y];

                            float currentScore = WD * Damage + WK * KillBonus + WA * supportTarget + WDb * distanceBonus +
                                WR * resourceBonus - WTh * Threat;
                            currentScore = Math.Clamp(score, 0, 100);
                            if (currentScore > score)
                            {
                                score = currentScore;
                                result["Location"] = location;
                                result["target"] = player;
                                result["sepll"] = spell;
                            }
                        }

                    }
                }
            }
        }

        if (score != 0)
            return result;
        else
        {
            Vector2Int finalLocation = new Vector2Int(0,0);
            result["Location"] = finalLocation;
            return result;
        }
            
    }

    public static List<Vector2Int> GetPositionsWithinManhattanDistance(
        Vector2Int origin, int distance, int gridWidth, int gridHeight)
    {
        List<Vector2Int> positions = new List<Vector2Int>();

        for (int dx = -distance; dx <= distance; dx++)
        {
            for (int dy = -distance; dy <= distance; dy++)
            {
   
                if (Mathf.Abs(dx) + Mathf.Abs(dy) <= distance)
                {
                    int newX = origin.x + dx;
                    int newY = origin.y + dy;

                    if (newX >= 0 && newX < gridWidth && newY >= 0 && newY < gridHeight)
                    {
                        positions.Add(new Vector2Int(newX, newY));
                    }
                }
            }
        }

        return positions;
    }
    public static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

}
