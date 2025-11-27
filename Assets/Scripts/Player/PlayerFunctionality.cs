using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerFunctionality : MonoBehaviour
{ 
    public Spell[] spells = new Spell[4];
    public Spell selectedSpell = null;
    public Spell enemySelectedSpell = null;
    public string spellFrom = null;

    private GridManager gridManager;
    public Vector2Int gridPosition;
    public float yPos = 1.5f;

    [Header("Player Settings")]
    public int moveRange = 3;
    public float moveSpeed = 5f;
    public float rotateSpeed = 1000; // degrees/second

    private List<Tile> highlightedTiles = new List<Tile>();
    private bool hasMoved = false;

    // Cache reachable tiles (distance) each turn to avoid recomputation until movement
    private Dictionary<Tile, int> reachableTileDistances = new Dictionary<Tile, int>();

    public GameObject playerSpellPanel;
    public GameObject spellTextHolder;
    public GameObject playerStatsHolder;
    public GameObject ui_SelectedEnemy;
    public GameObject selectedEnemy;
    public Stats playerStats;

    public bool turnStarted = true;
    [SerializeField] private GameObject playerVisual;

    private void OnEnable()
    {
        playerSpellPanel = UIController.Instance.getGameView.playerSpellPanel;
        spellTextHolder = UIController.Instance.getGameView.spellTextHolder;
        playerStatsHolder = UIController.Instance.getGameView.playerStatsHolder;
        ui_SelectedEnemy = UIController.Instance.getGameView.ui_SelectedEnemy;
    }
    void Start()
    {
        if(playerStats == null)
        {
            playerStats = GetComponent<Stats>();
        }
        for (int i = 0; i < spells.Length; i++)
        {
            spells[i] = JsonManager.Instance.ReturnPlayerSpell(i);
            if(spells[i] != null)
            {
                playerSpellPanel.transform.GetChild(i).GetChild(0).GetComponent<TextMeshProUGUI>().text = spells[i].name;
            }
        }

        selectedSpell = null;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ResetSpellTextHolder();
            selectedSpell = null;
            Debug.Log("Selected spell cleared by escape");

            foreach (Tile tile in highlightedTiles)
                tile.ResetHighlight();
            highlightedTiles.Clear();
            reachableTileDistances.Clear();

            ui_SelectedEnemy.SetActive(false);
            playerSpellPanel.SetActive(true);
            playerStatsHolder.SetActive(true);
        }

        GetEnemyDetails();

        if (TurnManager.Instance.currentState != TurnManager.TurnState.PlayerTurn)
        {
            if (turnStarted)
            {
                ResetSpellTextHolder();
                selectedSpell = null;
                foreach (Tile tile in highlightedTiles)
                    tile.ResetHighlight();
                highlightedTiles.Clear();
                reachableTileDistances.Clear();

                ui_SelectedEnemy.SetActive(false);
                playerSpellPanel.SetActive(true);
                playerStatsHolder.SetActive(true);
                OnTurnEnd();
            }

            return;
        }

        if (!hasMoved && TurnManager.Instance.currentState == TurnManager.TurnState.PlayerTurn && selectedSpell == null)
        {
            HandleTileHighlights();
        }

        if (selectedSpell != null) 
        {
            UseSpell();
        }
    }

    public void HandleTileHighlights()
    {
        HighlightReachableTiles();
        HandleTileClick();
    }

    private void HighlightReachableTiles()
    {
        if (highlightedTiles.Count > 0) return;

        // Compute reachable tiles via BFS considering obstacles
        reachableTileDistances = ComputeReachableTiles(gridPosition, moveRange);

        foreach (var kvp in reachableTileDistances)
        {
            Tile tile = kvp.Key;
            if (tile == null) continue;

            // Occupant logic
            if (tile.gridPosition == gridPosition)
            {
                tile.Highlight(Color.green);
            }
            else if (tile.occupant == null)
            {
                tile.Highlight(new Color(0.3f, 0.5f, 1f, 1f)); // reachable & free
            }
            else
            {
                // Occupied but reachable (show as red)
                tile.Highlight(Color.red);
            }

            highlightedTiles.Add(tile);
        }
    }

    private void HandleTileClick()
    {
        if (Input.GetMouseButtonDown(0)) // LMB
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Tile clickedTile = hit.collider.GetComponent<Tile>();
                if (clickedTile != null && highlightedTiles.Contains(clickedTile))
                {
                    // Only move to empty, walkable tiles that were computed reachable
                    if (clickedTile.occupant == null && reachableTileDistances.ContainsKey(clickedTile))
                    {
                        StartCoroutine(MoveAlongPath(clickedTile));
                    }
                }
            }
        }
    }

    //  BFS pathfinding movement
    private IEnumerator MoveAlongPath(Tile targetTile)
    {
        hasMoved = true;

        foreach (Tile tile in highlightedTiles)
            tile.ResetHighlight();
        highlightedTiles.Clear();
        reachableTileDistances.Clear();

        // Get path (list of grid positions excluding current)
        List<Vector2Int> path = FindPath(gridPosition, targetTile.gridPosition);
        if (path == null || path.Count == 0)
        {
            Debug.Log("No path found to target tile.");
            hasMoved = false; // allow retry
            yield break;
        }

        // Free old tile
        Tile currentTile = gridManager.GetTile(gridPosition);
        if (currentTile != null && currentTile.occupant == gameObject)
            currentTile.occupant = null;

        // Step through path
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 targetPos = new Vector3(path[i].x, yPos, path[i].y);

            while (Vector3.Distance(transform.position, targetPos) > 0.01f)
            {
                // Rotate toward movement direction (Y-axis only)
                Vector3 flatTarget = new Vector3(targetPos.x, transform.position.y, targetPos.z);
                Vector3 direction = flatTarget - transform.position;
                direction.y = 0f;

                if (direction.sqrMagnitude > 0.0001f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(direction.normalized, Vector3.up);

                    // Use the visualObject(fallback to root if visual is not assigned)
                    Transform rotTarget = playerVisual != null ? playerVisual.transform : transform;
                    rotTarget.rotation = Quaternion.RotateTowards(rotTarget.rotation, lookRot, rotateSpeed * Time.deltaTime);
                }

                // Move toward target
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                yield return null;
            }

            // Snap to exact step position
            transform.position = targetPos;
        }

        gridPosition = targetTile.gridPosition;
        targetTile.occupant = gameObject;

        yield return null;
    }

    // BFS to compute reachable tiles within range
    private Dictionary<Tile, int> ComputeReachableTiles(Vector2Int start, int range)
    {
        Dictionary<Tile, int> result = new Dictionary<Tile, int>();
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        Dictionary<Vector2Int, int> dist = new Dictionary<Vector2Int, int>();

        q.Enqueue(start);
        dist[start] = 0;

        while (q.Count > 0)
        {
            Vector2Int current = q.Dequeue();
            int currentDist = dist[current];
            Tile currentTile = gridManager.GetTile(current);
            if (currentTile != null && currentDist <= range)
            {
                result[currentTile] = currentDist;
            }

            if (currentDist == range) continue;

            foreach (Vector2Int dir in fourDirs)
            {
                Vector2Int next = new Vector2Int(current.x + dir.x, current.y + dir.y);
                if (dist.ContainsKey(next)) continue;

                Tile nextTile = gridManager.GetTile(next);
                if (nextTile == null) continue;
                if (!nextTile.walkable) continue;
                // Treat other units as blocking for traversal
                if (nextTile.occupant != null && nextTile.occupant != gameObject) continue;

                dist[next] = currentDist + 1;
                q.Enqueue(next);
            }
        }

        return result;
    }

    // Path reconstruction using BFS (uniform cost grid)
    private List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        if (start == goal) return new List<Vector2Int>();

        Queue<Vector2Int> q = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        q.Enqueue(start);
        cameFrom[start] = start;

        while (q.Count > 0)
        {
            Vector2Int current = q.Dequeue();
            if (current == goal) break;

            foreach (Vector2Int dir in fourDirs)
            {
                Vector2Int next = new Vector2Int(current.x + dir.x, current.y + dir.y);
                if (cameFrom.ContainsKey(next)) continue;

                Tile nextTile = gridManager.GetTile(next);
                if (nextTile == null) continue;
                if (!nextTile.walkable) continue;
                // Can't traverse through other occupants
                if (nextTile.occupant != null && next != goal) continue;
                // Allow goal if empty (already verified before calling)

                cameFrom[next] = current;
                q.Enqueue(next);
            }
        }

        if (!cameFrom.ContainsKey(goal))
        {
            return null; // unreachable
        }

        // Reconstruct path (reverse)
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int cur = goal;
        while (cur != start)
        {
            path.Add(cur);
            cur = cameFrom[cur];
        }
        path.Reverse();
        return path;
    }

    private static readonly Vector2Int[] fourDirs = new[]
    {
        new Vector2Int(1,0),
        new Vector2Int(-1,0),
        new Vector2Int(0,1),
        new Vector2Int(0,-1)
    };

    public void ResetTurn()
    {
        hasMoved = false;
        selectedSpell = null; 
        foreach (Tile tile in highlightedTiles)
            tile.ResetHighlight();
        highlightedTiles.Clear();
        reachableTileDistances.Clear();
    }

    public void Initialize(Vector2Int startPos, GridManager grid)
    {
        gridPosition = startPos;
        gridManager = grid;

        transform.position = new Vector3(startPos.x, yPos, startPos.y);
        grid.GetTile(startPos).occupant = gameObject;
    }

    public void setSpellFrom(string from)
    {
        if (TurnManager.Instance.currentState != TurnManager.TurnState.PlayerTurn)
            return;
        spellFrom = from;
    }

    public void SetSelectedSpell(int index)
    {
        if (TurnManager.Instance.currentState != TurnManager.TurnState.PlayerTurn)
            return;
        switch (spellFrom)
        {
            case "Player":
                selectedSpell = spells[index];

                if (selectedSpell != null)
                {
                    Debug.Log($"Selected spell: {selectedSpell.name}");
                    HighlightEnemiesInRange();
                    SetSpellTextHolder(selectedSpell);

                }
                break;
            case "Enemy":
                enemySelectedSpell = selectedEnemy.GetComponent<Enemy>().spells[index];
                if(enemySelectedSpell != null)
                {
                    SetSpellTextHolder(enemySelectedSpell);
                }
                break;
            default:
                Debug.Log("spellFrom not set");
                break;
        }
        
    }

    private void SetSpellTextHolder(Spell spell)
    {
        spellTextHolder.gameObject.SetActive(true);

        spellTextHolder.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Spell : " + spell.name;
        spellTextHolder.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "Damage : " + spell.damage;
        spellTextHolder.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = "Cost : " + spell.resourceCost.ToString();
        spellTextHolder.transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = "Range : " + spell.range.ToString();

        if (spell.support)
        {
            spellTextHolder.transform.GetChild(4).GetComponent<TextMeshProUGUI>().text = "Support : " + "Yes";
        }
        else
        {
            spellTextHolder.transform.GetChild(4).GetComponent<TextMeshProUGUI>().text = "Support : " + "No";
        }

        spellTextHolder.transform.GetChild(5).GetComponent<TextMeshProUGUI>().text = "Description : " + spell.description;
    }

    private void ResetSpellTextHolder()
    {
        spellTextHolder.gameObject.SetActive(false);
    }

    public void SyncGridPosition()
    {
        if (gridManager == null || gridManager.grid == null)
            return;

        Vector2Int newPos = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.z)
        );

        if (newPos != gridPosition)
        {
            Tile oldTile = gridManager.GetTile(gridPosition);
            if (oldTile != null && oldTile.occupant == gameObject)
                oldTile.occupant = null;
        }

        Tile newTile = gridManager.GetTile(newPos);
        if (newTile != null)
        {
            newTile.occupant = gameObject;
            gridPosition = newPos;
        }
    }

    public void UseSpell()
    {
        if (TurnManager.Instance.currentState != TurnManager.TurnState.PlayerTurn)
            return;

        if (selectedSpell == null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Enemy enemy = hit.collider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    int distance = Mathf.Abs(enemy.gridPosition.x - gridPosition.x) + Mathf.Abs(enemy.gridPosition.y - gridPosition.y);
                    if (distance <= selectedSpell.range)
                    {
                        if(selectedSpell.resourceCost > playerStats.resource)
                        {
                            Debug.Log("Not enough resource");
                            return;
                        }
                        else
                        {
                            playerStats.resource -= selectedSpell.resourceCost;
                            playerStats.UpdateStatsHolder();
                        }

                        Debug.Log($"Casted {selectedSpell.name} on {enemy.name}");

                        ActionManager.Instance.UseSpell(selectedSpell, this.gameObject, enemy.gameObject);

                        TurnManager.Instance.EndPlayerTurn();
                        foreach (Tile tile in highlightedTiles)
                            tile.ResetHighlight();
                        highlightedTiles.Clear();
                        reachableTileDistances.Clear();
                        selectedSpell = null;
                        hasMoved = true;

                    }
                    else
                    {
                        Debug.Log("Target out of spell range!");
                    }
                }
                else
                {
                    PlayerFunctionality player = hit.collider.GetComponent<PlayerFunctionality>();
                    if(player != null)
                    {
                        
                            if (selectedSpell.resourceCost > playerStats.resource)
                            {
                                Debug.Log("Not enough resource");
                                return;
                            }
                            else
                            {
                                playerStats.resource -= selectedSpell.resourceCost;
                                playerStats.UpdateStatsHolder();
                            }

                            Debug.Log($"Casted {selectedSpell.name} on {player.name}");

                            ActionManager.Instance.UseSpell(selectedSpell, this.gameObject, player.gameObject);

                            TurnManager.Instance.EndPlayerTurn();
                            foreach (Tile tile in highlightedTiles)
                                tile.ResetHighlight();
                            highlightedTiles.Clear();
                            reachableTileDistances.Clear();
                            selectedSpell = null;
                            hasMoved = true;
                            playerStats.UpdateStatsHolder();

                    }
                }
            }
        }
    }

    private void HighlightEnemiesInRange()
    {
        foreach (Tile tile in highlightedTiles)
            tile.ResetHighlight();
        highlightedTiles.Clear();
        reachableTileDistances.Clear();

        if (selectedSpell == null || gridManager == null)
            return;

        foreach (Tile tile in gridManager.grid)
        {
            if (tile == null) continue;

            int distance = Mathf.Abs(tile.gridPosition.x - gridPosition.x) + Mathf.Abs(tile.gridPosition.y - gridPosition.y);

            if (distance <= selectedSpell.range)
            {
                Color baseColor = new Color(1f, 0.5f, 0.4f, 0.5f);
                tile.Highlight(baseColor);
                highlightedTiles.Add(tile);

                if (tile.occupant == gameObject)
                {
                    tile.Highlight(Color.green);
                }
                else if (tile.occupant != null)
                {
                    Enemy enemy = tile.occupant.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        tile.Highlight(new Color(1f, 0f, 0f, 0.9f));
                    }
                }
            }
        }
    }

    public void OnTurnStart()
    {
        SyncGridPosition();
        playerStats.resource = Mathf.Clamp((int)(playerStats.resource + (playerStats.maxResource * 0.1)), 0, playerStats.maxResource);
        playerStats.UpdateStatsHolder();
        turnStarted = true;
    }

    public void OnTurnEnd()
    {
        SyncGridPosition();
        turnStarted = false;
    }

    public void GetEnemyDetails()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (selectedSpell != null && TurnManager.Instance.currentState == TurnManager.TurnState.PlayerTurn)
                return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Enemy enemy = hit.collider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    selectedEnemy = enemy.gameObject;
                    Debug.Log("Selected Enemy: " + selectedEnemy.name);
                    SetSelectedEnemyDetails(selectedEnemy);
                    ui_SelectedEnemy.SetActive(true);
                    playerSpellPanel.SetActive(false);
                    playerStatsHolder.SetActive(false);
                    ResetSpellTextHolder();
                }
            }
        }
    }

    public void SetSelectedEnemyDetails(GameObject selectedEnemy)
    {
        if (selectedEnemy != null)
        {
            if (ui_SelectedEnemy != null)
            {
                ui_SelectedEnemy.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = "Name: " + selectedEnemy.name;
                ui_SelectedEnemy.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = "Health: " + selectedEnemy.GetComponent<Stats>().health;
                ui_SelectedEnemy.transform.GetChild(0).GetChild(2).GetComponent<TextMeshProUGUI>().text = "Resource: " + selectedEnemy.GetComponent<Stats>().resource;

                ui_SelectedEnemy.transform.GetChild(1).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = selectedEnemy.GetComponent<Enemy>().spells[0].name;
                ui_SelectedEnemy.transform.GetChild(1).GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>().text = selectedEnemy.GetComponent<Enemy>().spells[1].name;
                ui_SelectedEnemy.transform.GetChild(1).GetChild(2).GetChild(0).GetComponent<TextMeshProUGUI>().text = selectedEnemy.GetComponent<Enemy>().spells[2].name;
                ui_SelectedEnemy.transform.GetChild(1).GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().text = selectedEnemy.GetComponent<Enemy>().spells[3].name;
            }
        }
    }

    
}
