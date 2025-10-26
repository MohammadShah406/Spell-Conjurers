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

    private List<Tile> highlightedTiles = new List<Tile>();
    private bool hasMoved = false;

    public GameObject playerSpellPanel;
    public GameObject spellTextHolder;
    public GameObject playerStatsHolder;
    public GameObject ui_SelectedEnemy;
    public GameObject selectedEnemy;
    public Stats playerStats;

    public bool turnStarted = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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

    // Update is called once per frame
    void Update()
    {
        //Unselect spell and Enemy Details
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ResetSpellTextHolder();
            selectedSpell = null;
            Debug.Log("Selected spell cleared by escape");

            foreach (Tile tile in highlightedTiles)
                tile.ResetHighlight();
            highlightedTiles.Clear();

            ui_SelectedEnemy.SetActive(false);
            playerSpellPanel.SetActive(true);
            playerStatsHolder.SetActive(true);
        }

        // Always allow clicking to view enemy details
        GetEnemyDetails();

        // Handle turn transitions
        if (TurnManager.Instance.currentState != TurnManager.TurnState.PlayerTurn)
        {
            if (turnStarted)
            {
                // Only run this once when the turn actually ends
                ResetSpellTextHolder();
                selectedSpell = null;
                foreach (Tile tile in highlightedTiles)
                    tile.ResetHighlight();
                highlightedTiles.Clear();

                ui_SelectedEnemy.SetActive(false);
                playerSpellPanel.SetActive(true);
                playerStatsHolder.SetActive(true);
                OnTurnEnd();
            }

            return; // Don’t process movement or spells during enemy turn
        }

        // If it's player turn and not moved yet, show reachable tiles
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
        selectedSpell = null; 
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

    public void OnTurnStart()
    {
        //10% of max resource is added every turn
        playerStats.resource = Mathf.Clamp((int)(playerStats.resource + (playerStats.maxResource * 0.1)), 0, playerStats.maxResource);
        playerStats.UpdateStatsHolder();

        turnStarted = true;
    }

    public void OnTurnEnd()
    {
        turnStarted = false;
    }

    public void GetEnemyDetails()
    {
        // Only intercept clicks for details when NOT in the middle of casting a spell on your turn.
        if (Input.GetMouseButtonDown(0))
        {
            // If player is casting a spell this turn, keep the click for UseSpell
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
