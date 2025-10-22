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

    public static EnemyManager Instance { get; private set; }


    [Header("Weight Settings")]
    public float weightDamage = 1.0f;    // damage
    public float weightKill = 5.0f;    // sure kill
    public float weightPriorityRole = 2.0f;    // target priority
    public float weightProximity = 0.5f;    // proximity
    public float weightSpellEffeciency = 1.5f;   // efficiency
    public float weightThreat = 0.8f; //Threat avoidance 


    [Header("Out of Range Weight Settings")]
    public float oorWeightPriorityRole = 1.0f;    // prioritize supports
    public float oorWeightLowHpUnit = 5.0f;    // target low-HP units
    public float oorWeightCloseTarget = 2.0f;    // prefer closer targets
    public float oorWeightThreat = 0.8f;  // Threat avoidance if target is not within range


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
        gridManager = GridManager.Instance;
        GameObject[] players = GameManager.Instance.players.ToArray();

        foreach (GameObject player in players)
        {
            int[] playerData = { 30, 1 };
            playerMaxDamageList.Add(player, playerData);
        }

        threatGrid = new float[height, width];
        for (int i =0; i < height; i++)
        {
            for (int j =0; j < width; j++)
                threatGrid[i, j] =0;
        }
    }

    public void clearThreatGrid()
    {
        for (int i =0; i < threatGrid.GetLength(0); i++)
        {
            for (int j =0; j < threatGrid.GetLength(1); j++)
                threatGrid[i, j] =0;
        }

    }
    public void calculateThreatGrid()
    {
        clearThreatGrid();
        GameObject[] players = GameManager.Instance.players.ToArray();
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
    }

    public void UpdateMaxDamage(GameObject player, int damage, int range =1)
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

        for (int i =0; i < rows; i++)
        {
            string rowStr = "";
            for (int j =0; j < cols; j++)
            {
                rowStr += threatGrid[i, j].ToString("F2") + "\t";
            }
            Debug.Log(rowStr);
        }
    }

    public Dictionary<String, object> CalculateScore(Enemy enemy)
    {
        Dictionary<String, object> result = new Dictionary<String, object>();
        float score =0;
        GameObject[] players = GameManager.Instance.players.ToArray();

        // Track the best overall values while iterating
        float bestScore = float.MinValue;
        Vector2Int bestLocation = enemy.gridPosition;
        GameObject bestTarget = null;
        Spell bestSpell = null;

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
                        if (moveDistance <= enemy.moveRange && gridManager.grid[location.x,location.y].occupant == null)
                        {
                            // Ensure float division for accuracy
                            float Damage = spell.damage * (spell.accuracy /100f);
                            float KillBonus = (playerStats.health - Damage) <=0 ?1f :0f;
                            float supportTarget = (playerStats.type == Stats.Type.Support) ?1f :0f;
                            float distanceBonus = (enemy.moveRange - moveDistance) / (float)enemy.moveRange;
                            float resourceBonus = (Damage / (float)spell.resourceCost) *0.01f;
                            float Threat = threatGrid[location.x, location.y];

                            float currentScore = weightDamage * Damage + weightKill * KillBonus + weightPriorityRole * supportTarget + weightProximity * distanceBonus +
                                weightSpellEffeciency * resourceBonus - weightThreat * Threat;

                            currentScore = Mathf.Clamp(currentScore,0f,100f);

                            if (currentScore > bestScore)
                            {
                                bestScore = currentScore;
                                bestLocation = location;
                                bestTarget = player;
                                bestSpell = spell;
                            }
                        }

                    }
                }
            }
        }

        if (bestScore > float.MinValue)
        {
            // Store consistent keys
            result["location"] = bestLocation;
            result["target"] = bestTarget;
            result["spell"] = bestSpell;
            result["score"] = bestScore;
            return result;
        }
        else
        {
            List<Vector2Int> Locations = new List<Vector2Int>();
            Locations = GetPositionsWithinManhattanDistance(enemy.gridPosition, enemy.moveRange, widthGrid, heightGrid);
            foreach (Vector2Int location in Locations)
            {
                float tileScore =0;
                if (gridManager.grid[location.x, location.y].occupant != null) continue;
                foreach (GameObject player in players)
                {
                    Stats playerStats = player.GetComponent<Stats>();
                    PlayerFunctionality playerFunctionality = player.GetComponent<PlayerFunctionality>();

                    float supportTarget = (playerStats.type == Stats.Type.Support) ?1f :0f;
                    float Vulnerability =1f - (playerStats.health / playerStats.maxHealth);
                    float distanceFactor =1f / (1f + ManhattanDistance(location, playerFunctionality.gridPosition));
                    //float KillBonus = (playerStats.health - Damage) <=0 ?1 :0;
                    float Threat = threatGrid[location.x, location.y];

                    float tempTileScore = supportTarget * oorWeightPriorityRole + Vulnerability * oorWeightLowHpUnit + distanceFactor* oorWeightCloseTarget - Threat*oorWeightThreat;
                    if (tempTileScore > tileScore) tileScore = tempTileScore;
                }
                if (tileScore > score)
                {
                    score = tileScore;
                    result["location"] = location;
                    result["score"] = score;
                }
            }
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

                    if (newX >=0 && newX < gridWidth && newY >=0 && newY < gridHeight)
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
