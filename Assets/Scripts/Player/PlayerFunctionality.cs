using Microsoft.CodeAnalysis.Scripting;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerFunctionality : MonoBehaviour
{
    public GameObject playerPanelUI;
    public Spell[] spells = new Spell[4];
    public Spell selectedSpell = null;

    private GridManager gridManager;
    public Vector2Int gridPosition;
    public float yPos = 1.5f;

    [Header("Player Settings")]
    public int moveRange = 3;
    public float moveSpeed = 5f;

    private List<Tile> highlightedTiles = new List<Tile>();
    private bool hasMoved = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < spells.Length; i++)
        {
            spells[i] = JsonManager.Instance.ReturnPlayerSpell(i);
            if(spells[i] != null)
            {
                playerPanelUI.transform.GetChild(i).GetChild(0).GetComponent<TextMeshProUGUI>().text = spells[i].name;
            }
        }

        selectedSpell = null;
    }

    // Update is called once per frame
    void Update()
    {
        //Unselect spell
        if (Input.GetKeyDown(KeyCode.Escape) || TurnManager.Instance.currentState != TurnManager.TurnState.PlayerTurn)
        {
            selectedSpell = null;
            Debug.Log("Selected spell cleared by escape");

            foreach (Tile tile in highlightedTiles)
                tile.ResetHighlight();
            highlightedTiles.Clear();
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
          EnemyManager enemyManager = GameObject.FindAnyObjectByType<EnemyManager>();
            if (enemyManager != null)
            {
                enemyManager.initializeThreatGrid(12, 12);
                Debug.Log("MMMMM");
            }
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            EnemyManager enemyManager = GameObject.FindAnyObjectByType<EnemyManager>();
            if (enemyManager != null)
            {
                enemyManager.caculateThreatGrid();
                Debug.Log("CCCCCCC");
            }

        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            EnemyManager enemyManager = GameObject.FindAnyObjectByType<EnemyManager>();
            if (enemyManager != null)
            {
                enemyManager.showGrid();
                Debug.Log("PPPPP");
            }
        }

        // If it's player turn and not moved yet, show reachable tiles
        if (!hasMoved && selectedSpell == null)
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

        foreach (Tile tile in gridManager.grid)
        {
            if (tile == null) continue;

            int distance = Mathf.Abs(tile.gridPosition.x - gridPosition.x) + Mathf.Abs(tile.gridPosition.y - gridPosition.y);
            if (distance <= moveRange && tile.occupant == null)
            {
                tile.Highlight(new Color(0.3f, 0.5f, 1f, 1f)); // soft blue
                highlightedTiles.Add(tile);
            }
            else if(distance <= moveRange && tile.occupant == gameObject)
            {
                tile.Highlight(Color.green);
                highlightedTiles.Add(tile);
            }
            else if (distance <= moveRange && tile.occupant != null)
            {
                tile.Highlight(Color.red);
                highlightedTiles.Add(tile);
            }
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
                    if(clickedTile.occupant == null)
                    {
                        StartCoroutine(MoveToTile(clickedTile));
                    }
                    
                }
            }
        }
    }

    private IEnumerator MoveToTile(Tile targetTile)
    {
        hasMoved = true;

        // Clear highlights
        foreach (Tile tile in highlightedTiles)
            tile.ResetHighlight();
        highlightedTiles.Clear();

        // Free old tile
        gridManager.GetTile(gridPosition).occupant = null;

        // Move smoothly
        Vector3 targetPos = new Vector3(targetTile.gridPosition.x, yPos, targetTile.gridPosition.y);
        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPos;
        gridPosition = targetTile.gridPosition;
        targetTile.occupant = gameObject;

        yield return null;
    }

    public void ResetTurn()
    {
        hasMoved = false;
        selectedSpell = null; // 👈 ensures clean start each turn
        foreach (Tile tile in highlightedTiles)
            tile.ResetHighlight();
        highlightedTiles.Clear();
    }

    public void Initialize(Vector2Int startPos, GridManager grid)
    {
        gridPosition = startPos;
        gridManager = grid;

        transform.position = new Vector3(startPos.x, yPos, startPos.y);
        grid.GetTile(startPos).occupant = gameObject;
    }

    public void SetSelectedSpell(int index)
    {
        selectedSpell = spells[index];

        if (selectedSpell != null)
        {
            Debug.Log($"Selected spell: {selectedSpell.name}");
            HighlightEnemiesInRange();
        }
    }

    public void SyncGridPosition()
    {
        if (gridManager == null || gridManager.grid == null)
            return;

        // Calculate nearest grid coordinates
        Vector2Int newPos = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.z)
        );

        // If position changed, clear the old tile
        if (newPos != gridPosition)
        {
            Tile oldTile = gridManager.GetTile(gridPosition);
            if (oldTile != null && oldTile.occupant == gameObject)
                oldTile.occupant = null;
        }

        // Update to new position
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

        if (Input.GetMouseButtonDown(0)) // Left click on target
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Enemy enemy = hit.collider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    // Check if within spell range
                    int distance = Mathf.Abs(enemy.gridPosition.x - gridPosition.x) + Mathf.Abs(enemy.gridPosition.y - gridPosition.y);
                    if (distance <= selectedSpell.range)
                    {
                        Debug.Log($"Casted {selectedSpell.name} on {enemy.name}");

                        // Example spell effects
                        ActionManager.Instance.UseSpell(selectedSpell, this.gameObject, enemy.gameObject);

                        // End player turn after casting
                        TurnManager.Instance.EndPlayerTurn();
                        foreach (Tile tile in highlightedTiles)
                            tile.ResetHighlight();
                        highlightedTiles.Clear();
                        selectedSpell = null;
                        hasMoved = true;

                    }
                    else
                    {
                        Debug.Log("Target out of spell range!");
                    }
                }
            }
        }
    }

    private void HighlightEnemiesInRange()
    {
        // Clear old highlights first
        foreach (Tile tile in highlightedTiles)
            tile.ResetHighlight();
        highlightedTiles.Clear();

        if (selectedSpell == null || gridManager == null)
            return;

        foreach (Tile tile in gridManager.grid)
        {
            if (tile == null) continue;

            int distance = Mathf.Abs(tile.gridPosition.x - gridPosition.x) + Mathf.Abs(tile.gridPosition.y - gridPosition.y);

            // Highlight all tiles within spell range
            if (distance <= selectedSpell.range)
            {
                // Default spell range color (light red/orange)
                Color baseColor = new Color(1f, 0.5f, 0.4f, 0.5f);
                tile.Highlight(baseColor);
                highlightedTiles.Add(tile);

                if (tile.occupant == gameObject)
                {
                    tile.Highlight(Color.green);
                    highlightedTiles.Add(tile);
                }
                // If occupant is an enemy, make it a stronger red
                else if (tile.occupant != null)
                {
                    Enemy enemy = tile.occupant.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        tile.Highlight(new Color(1f, 0f, 0f, 0.9f)); // strong red for enemies
                    }
                }
                
            }
        }
    }


}
