using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int gridPosition;
    public bool walkable = true;
    public GameObject occupant; // Enemy, Player, Rock, etc.

    public bool IsOccupied => occupant != null;

    private Renderer rend;
    private Color defaultColor;
    public bool isHighlighted = false;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        defaultColor = rend.material.color;
    }
    public void Highlight(Color color)
    {
        rend.material.color = color;
        isHighlighted = true;
    }

    public void ResetHighlight()
    {
        rend.material.color = defaultColor;
        isHighlighted = false;
    }


}
