using System;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using Newtonsoft.Json;
using static GridManager;
using static UnityEditor.Experimental.GraphView.GraphView;
[System.Serializable]
public class MapData
{
    public int[][] mapData;
}
public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 10; 
    public int height = 10;
    public GameObject tilePrefab;
    public GameObject obstaclePrefab;
    public GameObject map;
    public Tile[,] grid;


    [Header("Enemy Settings")]
    public GameObject EnemyHolder;
    public int enemyCount = 3;
    public EnemyManager enemyManager;
    public GameObject enemyPrefab;

    [Header("Player Settings")]
    public GameObject playerPrefab;
    private GameObject playerObj;

    public static GridManager Instance { get; private set; }


    public int[][] mapDetails;

    void Awake()
    {
        Instance = this;
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
    }

    public void ResetGame(int enemyCount = 3, float dmgMultiplier = 1)
    {
        Debug.Log("[GridManager] Resetting game...");
        this.enemyCount = enemyCount;

        // --- 0. Destroy all existing tiles ---
        foreach (Transform child in map.transform)
        {
            Destroy(child.gameObject);
        }

        // Clear grid reference
        //grid = new Tile[width, height];

        // --- 1. Recreate the tile grid ---
        /*for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                var tileObj = Instantiate(tilePrefab, new Vector3(x, 0, z), Quaternion.identity);
                tileObj.transform.parent = map.transform;
                var tile = tileObj.GetComponent<Tile>();

                tile.gridPosition = new Vector2Int(x, z);
                grid[x, z] = tile;
            }
        }*/

      

        mapDetails = LoadRoundMap(GameManager.Instance.roundNo);
        BuildMapFromData();

        //// --- 2. Clear all tiles' occupants ---
        //for (int x = 0; x < width; x++)
        //{
        //    for (int y = 0; y < height; y++)
        //    {
        //        if (grid[x, y] != null)
        //            grid[x, y].occupant = null;
        //    }
        //}


        // --- 3. Destroy existing enemies and player ---
        // Destroy enemies
        foreach (Transform child in EnemyHolder.transform)
        {
            Destroy(child.gameObject);
        }

        // Destroy player
        if (playerObj != null)
        {
            Destroy(playerObj);
            playerObj = null;
        }

        // --- 4. Clear manager lists ---
        if (enemyManager != null)
            enemyManager.enemies.Clear();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.players.Clear();
            GameManager.Instance.enemies.Clear();
        }

        // --- 5. Respawn everything ---
        SpawnPlayer();
        SpawnEnemies(enemyCount);

        // --- 6. Reinitialize threat grid ---
        if (enemyManager != null)
        {
            enemyManager.initializeThreatGrid(height, width);
            enemyManager.SetEnemyMultipliers(dmgMultiplier);
        }

        Debug.Log("[GridManager] Game reset complete.");
    }

    public void NextRound()
    {

    }


    public void Start()
    {
        //SpawnEnemies(enemyCount);

        //enemyManager.initializeThreatGrid(height, width);   
    }

    public Tile GetTile(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height)
            return null;

        return grid[pos.x, pos.y];
    }

    public void SpawnEnemies(int count)
    {
        // Get all tiles in the back row
        List<Vector2Int> availablePositions = new List<Vector2Int>();
        int backRowY = height - 1;

        for (int x = 0; x < width; x++)
        {
            if (grid[x, backRowY].occupant == null)
                availablePositions.Add(new Vector2Int(x, backRowY));
        }

        // Safety check
        if (availablePositions.Count == 0)
        {
            Debug.LogWarning("No available tiles in the back row to spawn enemies!");
            return;
        }

        // Clamp to avoid spawning more enemies than spaces
        int spawnCount = Mathf.Min(count, availablePositions.Count);

        for (int i = 0; i < spawnCount; i++)
        {
            // Pick a random free tile
            if(availablePositions == null)
            {
                return;
            }
            int randomIndex = UnityEngine.Random.Range(0, availablePositions.Count);
            Vector2Int pos = availablePositions[randomIndex];
            availablePositions.RemoveAt(randomIndex); // prevent reusing the same tile

            // Spawn enemy
            var enemyObj = Instantiate(enemyPrefab);
            enemyObj.name = "Enemy " + i;
            enemyObj.transform.parent = EnemyHolder.transform;
            enemyObj.transform.position = new Vector3(pos.x, 1.5f, pos.y);

            // Initialize and register
            var enemy = enemyObj.GetComponent<Enemy>();
            enemy.Initialize(pos, this, playerObj);
            enemyManager.AddEnemy(enemy);
            GameManager.Instance.enemies.Add(enemyObj);

            // Mark the tile as occupied
            GetTile(pos).occupant = enemyObj;
        }
    }


    private void SpawnPlayer()
    {
        Vector2Int playerStartPos = new Vector2Int(width / 2, 0); // center bottom of grid
        var instantiatedPlayerObj = Instantiate(playerPrefab, new Vector3(playerStartPos.x, 1.5f, playerStartPos.y), Quaternion.identity);
        playerObj = instantiatedPlayerObj;
        GameManager.Instance.players.Add(playerObj);
        GameManager.Instance.playerFunctionality.Add(playerObj.GetComponent<PlayerFunctionality>());
        GetTile(playerStartPos).occupant = instantiatedPlayerObj;

        var playerScript = instantiatedPlayerObj.GetComponent<PlayerFunctionality>();
        if (playerScript != null)
        {
            playerScript.Initialize(playerStartPos, this);
        }
    }

    public static int[][] LoadRoundMap(int roundNumber)
    {
        string fileName = $"Round_{roundNumber}";
        string path = Path.Combine(Application.dataPath, "Resources", "Rounds", fileName + ".json");

        if (!File.Exists(path))
        {
            Debug.LogError($"JSON file not found: {path}");
            return null;
        }

        try
        {
            string jsonText = File.ReadAllText(path);
            //MapData data = JsonUtility.FromJson<MapData>(jsonText);
            MapData data = JsonConvert.DeserializeObject<MapData>(jsonText);


            if (data == null || data.mapData == null)
            {
                Debug.LogError($"Invalid or empty map data in {fileName}.json");
                return null;
            }

            return data.mapData;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to load {fileName}.json: {ex.Message}");
            return null;
        }
    }

    public void BuildMapFromData()
    {
        if (mapDetails == null || mapDetails.Length == 0)
        {
            Debug.LogError("Map details are empty");
            return;
        }

        height = mapDetails.Length;
        width = mapDetails[0].Length;
        grid = new Tile[width, height];

        // Clear previous map
        foreach (Transform child in map.transform)
        {
            Destroy(child.gameObject);
        }

        // Clear previous enemies
        foreach (Transform child in EnemyHolder.transform)
        {
            Destroy(child.gameObject);
        }

        // Create Grid
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 tilePos = new Vector3(x, 0, z);
                var tileObj = Instantiate(tilePrefab, tilePos, Quaternion.identity);
                tileObj.transform.parent = map.transform;

                var tile = tileObj.GetComponent<Tile>();
                tile.gridPosition = new Vector2Int(x, z);
                grid[x, z] = tile;

                int cell = mapDetails[z][x];

                switch (cell)
                {
                    case 0: 
                        break;

                    case 1: 
                        SpawnObstacle(tileObj.transform, tile);
                        break;

                    case 2: 
                        //SpawnEnemy(tilePos);
                        break;

                    case 3: 
                        //SpawnPlayer(tilePos);
                        break;
                }
            }
        }

        Debug.Log("Map built successfully.");
    }

    private void SpawnObstacle(Transform tileTransform , Tile tile)
    {
        var obstacle = Instantiate(obstaclePrefab, tileTransform.position + Vector3.up, Quaternion.identity, tileTransform);
        tile.occupant = obstacle;
        tile.walkable = false;
    }
}
