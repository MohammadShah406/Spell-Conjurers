using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 10; 
    public int height = 10;
    public GameObject tilePrefab;
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

    void Awake()
    {
        Instance = this;
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }


        grid = new Tile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                var tileObj = Instantiate(tilePrefab, new Vector3(x, 0, z), Quaternion.identity);
                tileObj.transform.parent = map.transform;
                var tile = tileObj.GetComponent<Tile>();

                tile.gridPosition = new Vector2Int(x, z);
                grid[x, z] = tile;
            }
        }
        SpawnPlayer();
        
    }

    public void Start()
    {
        SpawnEnemies(enemyCount);

        enemyManager.initializeThreatGrid(height, width);   
    }

    public Tile GetTile(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height)
            return null;

        return grid[pos.x, pos.y];
    }

    public void SpawnEnemies(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2Int pos = new Vector2Int(i * 2, height - 1); // back row
            var enemyObj = Instantiate(enemyPrefab);
            enemyObj.name = "Enemy " + i;
            enemyObj.transform.parent = EnemyHolder.transform;
            var enemy = enemyObj.GetComponent<Enemy>();
            enemy.Initialize(pos, this, playerObj);
            enemyManager.AddEnemy(enemy);
            GameManager.Instance.enemies.Add(enemyObj);
        }
    }

    private void SpawnPlayer()
    {
        Vector2Int playerStartPos = new Vector2Int(width / 2, 0); // center bottom of grid
        var instantiatedPlayerObj = Instantiate(playerPrefab, new Vector3(playerStartPos.x, 1.5f, playerStartPos.y), Quaternion.identity);
        playerObj = instantiatedPlayerObj;
        GameManager.Instance.players.Add(playerObj);
        GetTile(playerStartPos).occupant = instantiatedPlayerObj;

        var playerScript = instantiatedPlayerObj.GetComponent<PlayerFunctionality>();
        if (playerScript != null)
        {
            playerScript.Initialize(playerStartPos, this);
        }
    }
}
