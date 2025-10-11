using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int gridPosition;
    public bool walkable = true;
    public GameObject occupant; // Enemy, Player, Rock, etc.

    public bool IsOccupied => occupant != null;
}
