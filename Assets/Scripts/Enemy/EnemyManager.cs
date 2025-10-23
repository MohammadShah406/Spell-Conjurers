using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.TestTools;

public class EnemyManager : MonoBehaviour
{
    public List<Enemy> enemies = new List<Enemy>();
    
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

    public void initializeThreatGrid(int height, int width)
    {
        widthGrid = width;
        heightGrid = height;
        gridManager = GridManager.Instance;
        GameObject[] players = GameManager.Instance.players.ToArray();

        playerMaxDamageList.Clear();
        foreach (GameObject player in players)
        {
            int[] playerData = { 30, 1 };
            playerMaxDamageList[player] = playerData;
        }

        // Use [width, height] so indexing is threatGrid[x,y] to match grid[x,y]
        threatGrid = new float[widthGrid, heightGrid];

        for (int x = 0; x < widthGrid; x++)
        {
            for (int y = 0; y < heightGrid; y++)
                threatGrid[x, y] = 0f;
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



    public void ShowScoreGrid(Enemy enemy)
    {
        // Clear previous debug text
        ClearScoreDebug();
        Debug.Log("[EnemyManager] Generating score grid visualization...");

        if (gridManager == null) gridManager = GridManager.Instance;
        if (gridManager == null) return;

        int w = widthGrid;
        int h = heightGrid;

        float[,] tileScores = new float[w, h];
        float minScore = float.MaxValue;
        float maxScore = float.MinValue;

        // Compute score for each tile
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Tile tile = gridManager.grid[x, y];
                if (tile == null)
                {
                    tileScores[x, y] = float.NegativeInfinity;
                    continue;
                }

                // Compute the AI score for this tile using the same formulas as CalculateScore
                Vector2Int loc = new Vector2Int(x, y);
                float score = ComputeTileScoreForEnemy(enemy, loc);

                // Normalize undefined/occupied to zero for visualization
                if (float.IsNegativeInfinity(score) || float.IsNaN(score))
                    score = 0f;

                tileScores[x, y] = score;
                if (score < minScore) minScore = score;
                if (score > maxScore) maxScore = score;
            }
        }

        bool singleValue = Mathf.Approximately(minScore, maxScore);

        // Create text labels for visualization
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Tile tile = gridManager.grid[x, y];
                if (tile == null) continue;

                float tileScore = tileScores[x, y];

                GameObject textObj = new GameObject($"ScoreDebug_{x}_{y}");
                textObj.transform.SetParent(gridManager.map.transform);
                textObj.transform.position = tile.transform.position + Vector3.up * 1.5f;

                TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
                tmp.fontSize = 2;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.text = tileScore.ToString("F2");

                float t = singleValue ? 0.5f : Mathf.InverseLerp(minScore, maxScore, tileScore);
                tmp.color = Color.Lerp(Color.red, Color.green, t);

    #if UNITY_EDITOR
                // Make text always face Scene camera in the Editor
                textObj.AddComponent<SceneBillboard>();
    #endif
            }
        }

        // Find & mark best reachable tile (the yellow sphere should be achievable this turn)
        Vector2Int bestReachableLoc = new Vector2Int(-1, -1);
        float bestReachableVal = float.MinValue;
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                // skip invalid/empty display cells
                if (float.IsNegativeInfinity(tileScores[x, y])) continue;

                Vector2Int loc = new Vector2Int(x, y);
                int moveDistance = ManhattanDistance(loc, enemy.gridPosition);
                if (moveDistance <= enemy.moveRange)
                {
                    // tile must be empty and reachable (ComputeTileScoreForEnemy already returns -Inf for occupied)
                    float val = tileScores[x, y];
                    if (val > bestReachableVal)
                    {
                        bestReachableVal = val;
                        bestReachableLoc = loc;
                    }
                }
            }
        }

        // Place yellow marker only if a reachable tile exists
        if (bestReachableLoc.x >= 0)
        {
            Tile bestTile = gridManager.GetTile(bestReachableLoc);
            if (bestTile != null)
            {
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "ScoreDebug_BestTile";
                marker.transform.SetParent(gridManager.map.transform);
                marker.transform.position = bestTile.transform.position + Vector3.up * 1.2f;
                marker.transform.localScale = Vector3.one * 0.3f;
                marker.GetComponent<Renderer>().material.color = Color.yellow;
                Destroy(marker.GetComponent<Collider>());
            }

            Debug.Log($"[EnemyManager] Best reachable tile score: {bestReachableVal:F2} at {bestReachableLoc}");
        }
        else
        {
            Debug.Log($"[EnemyManager] No reachable scored tile for this enemy this turn. (Best overall shown but not reachable)");
        }
    }

    private float ComputeTileScoreForEnemy(Enemy enemy, Vector2Int loc)
    {
        if (gridManager == null) gridManager = GridManager.Instance;
        if (gridManager == null) return float.NegativeInfinity;

        // bounds check
        if (loc.x < 0 || loc.x >= widthGrid || loc.y < 0 || loc.y >= heightGrid)
            return float.NegativeInfinity;

        Tile tile = gridManager.grid[loc.x, loc.y];
        if (tile == null) return float.NegativeInfinity;
        if (tile.occupant != null) return float.NegativeInfinity;

        GameObject[] players = GameManager.Instance.players.ToArray();

        // 1) Attack-based scoring
        float bestAttackScore = float.MinValue;
        int moveDistance = ManhattanDistance(loc, enemy.gridPosition);

        if (moveDistance > enemy.moveRange)
            return float.NegativeInfinity; // mark as unreachable

        if (enemy.moveRange >= 0 && moveDistance <= enemy.moveRange)
        {
            foreach (GameObject playerObj in players)
            {
                if (playerObj == null) continue;
                Stats playerStats = playerObj.GetComponent<Stats>();
                PlayerFunctionality playerFunc = playerObj.GetComponent<PlayerFunctionality>();
                if (playerStats == null || playerFunc == null) continue;

                int distToPlayerFromTile = ManhattanDistance(loc, playerFunc.gridPosition);

                foreach (Spell spell in enemy.spells)
                {
                    if (spell == null) continue;
                    if (distToPlayerFromTile <= spell.range)
                    {
                        float Damage = spell.damage * (spell.accuracy / 100f);
                        float KillBonus = (playerStats.health - Damage) <= 0f ? 1f : 0f;
                        float supportTarget = (playerStats.type == Stats.Type.Support) ? 1f : 0f;
                        float distanceBonus = enemy.moveRange > 0 ? (enemy.moveRange - moveDistance) / (float)enemy.moveRange : 0f;
                        float resourceBonus = (spell.resourceCost != 0) ? (Damage / (float)spell.resourceCost) * 0.01f : 0f;
                        float Threat = threatGrid[loc.x, loc.y];

                        float currentScore = weightDamage * Damage
                                           + weightKill * KillBonus
                                           + weightPriorityRole * supportTarget
                                           + weightProximity * distanceBonus
                                           + weightSpellEffeciency * resourceBonus
                                           - weightThreat * Threat;

                       
                        currentScore = Mathf.Clamp(currentScore, 0f, 100f);

                        if (currentScore > bestAttackScore)
                            bestAttackScore = currentScore;
                    }
                }
            }
        }

        // 2) Positional/out-of-range heuristic
        float bestPositional = float.MinValue;
        foreach (GameObject playerObj in players)
        {
            if (playerObj == null) continue;
            Stats playerStats = playerObj.GetComponent<Stats>();
            PlayerFunctionality playerFunc = playerObj.GetComponent<PlayerFunctionality>();
            if (playerStats == null || playerFunc == null) continue;

            float supportTarget = (playerStats.type == Stats.Type.Support) ? 1f : 0f;
            float Vulnerability = 1f - (playerStats.health / (float)playerStats.maxHealth);
            float distanceFactor = 1f / (1f + ManhattanDistance(loc, playerFunc.gridPosition));
            float Threat = threatGrid[loc.x, loc.y];

            float tempTileScore = supportTarget * oorWeightPriorityRole
                                + Vulnerability * oorWeightLowHpUnit
                                + distanceFactor * oorWeightCloseTarget
                                - Threat * oorWeightThreat;

            // clamp positional to a reasonable range
            tempTileScore = Mathf.Clamp(tempTileScore, -100f, 100f);

            if (tempTileScore > bestPositional) bestPositional = tempTileScore;
        }

        float finalScore = Mathf.Max(bestAttackScore, bestPositional);

        // If both remained unset, indicate invalid/unscored tile
        if (finalScore == float.MinValue)
            return float.NegativeInfinity;

        return finalScore;
    }


    public void ClearScoreDebug()
    {
        if (gridManager == null) gridManager = GridManager.Instance;
        if (gridManager == null) return;

        // Remove previously created debug objects (ScoreDebug_* and marker)
        List<GameObject> toRemove = new List<GameObject>();
        foreach (Transform child in gridManager.map.transform)
        {
            if (child == null) continue;
            if (child.name.StartsWith("ScoreDebug_") || child.name == "ScoreDebug_BestTile")
                toRemove.Add(child.gameObject);
        }

        // Destroy outside of the transform enumeration
        foreach (var go in toRemove)
            DestroyImmediate(go);
    }

}
