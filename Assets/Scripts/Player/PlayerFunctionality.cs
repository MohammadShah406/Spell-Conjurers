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
    public int attackRange = 1;
    public float moveSpeed = 5f;

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
    }

    // Update is called once per frame
    void Update()
    {
        //Unselect spell
        if (Input.GetMouseButtonDown(1)) // RMB
        {
            selectedSpell = null;
            Debug.Log("Selected spell cleared by right-click");
        }
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

}
