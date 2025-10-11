using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 10; 
    public int height = 10;
    public GameObject tilePrefab;
    public GameObject map;
    public Tile[,] grid;

    [Header("Enemy Settings")]
    public int enemyCount = 3;
    public EnemyManager enemyManager;
    public GameObject enemyPrefab;

    [Header("Player Settings")]
    public GameObject playerPrefab;
    private Transform playerTransform;

    void Awake()
    {
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
        SpawnEnemies(enemyCount);
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
            var enemy = enemyObj.GetComponent<Enemy>();
            enemy.Initialize(pos, this, playerTransform);
            enemyManager.AddEnemy(enemy);
        }
    }

    private void SpawnPlayer()
    {
        Vector2Int playerStartPos = new Vector2Int(width / 2, 0); // center bottom of grid
        var playerObj = Instantiate(playerPrefab, new Vector3(playerStartPos.x, 1.5f, playerStartPos.y), Quaternion.identity);
        playerTransform = playerObj.transform;
        GetTile(playerStartPos).occupant = playerObj;

        // Optionally initialize player if your Player script needs it
        var playerScript = playerObj.GetComponent<Player>();
        if (playerScript != null)
        {
            //playerScript.Initialize(playerStartPos, this);
        }
    }
}
