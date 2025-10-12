using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float yPos = 1.5f;
    public Vector2Int gridPosition;
    public int moveRange = 3;
    public int attackRange = 1;
    public float moveSpeed = 5f;

    private GridManager gridManager;
    private Transform player;

    public Spell[] spells = new Spell[4];

    public void Initialize(Vector2Int startPos, GridManager grid, Transform playerRef)
    {
        gridPosition = startPos;
        gridManager = grid;
        player = playerRef;

        transform.position = new Vector3(startPos.x, yPos, startPos.y);
        grid.GetTile(startPos).occupant = gameObject;

        StartCoroutine(FacePlayer());

        InitializeSpells();
    }

    public IEnumerator TakeTurn(System.Action onComplete)
    {
        Player playerScript = player.GetComponent<Player>();
        Tile playerTile = gridManager.GetTile(new Vector2Int(Mathf.RoundToInt(player.position.x), Mathf.RoundToInt(player.position.z)));

        if (playerTile == null)
        {
            Debug.LogWarning("Player tile not found.");
            onComplete?.Invoke();
            yield break;
        }

        int distToPlayer = Mathf.Abs(playerTile.gridPosition.x - gridPosition.x) +
                           Mathf.Abs(playerTile.gridPosition.y - gridPosition.y);

        //Attack Player if in range
        if (distToPlayer <= attackRange)
        {
            yield return FacePlayer();
            Debug.Log($"{name} attacks player!");
            yield return new WaitForSeconds(0.5f);
            onComplete?.Invoke();
            yield break;
        }

        // Otherwise, move towards player
        int steps = Mathf.Min(moveRange, distToPlayer - attackRange);
        Vector2Int targetPos = gridPosition;

        for (int i = 0; i < steps; i++)
        {
            // Determine preferred direction
            Vector2Int dir = Vector2Int.zero;
            bool horizontalPriority = Mathf.Abs(playerTile.gridPosition.x - targetPos.x) >=
                                      Mathf.Abs(playerTile.gridPosition.y - targetPos.y);

            if (horizontalPriority)
                dir.x = playerTile.gridPosition.x > targetPos.x ? 1 : -1;
            else
                dir.y = playerTile.gridPosition.y > targetPos.y ? 1 : -1;

            Vector2Int nextPos = targetPos + dir;
            Tile nextTile = gridManager.GetTile(nextPos);

            // If main path blocked, try side directions
            if (nextTile == null || nextTile.IsOccupied || !nextTile.walkable)
            {
                Vector2Int[] sideDirs = horizontalPriority
                    ? new[] { new Vector2Int(0, 1), new Vector2Int(0, -1) } // try up/down
                    : new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0) }; // try left/right

                bool moved = false;
                foreach (var s in sideDirs)
                {
                    Vector2Int altPos = targetPos + s;
                    Tile altTile = gridManager.GetTile(altPos);
                    if (altTile != null && !altTile.IsOccupied && altTile.walkable)
                    {
                        yield return MoveTo(altPos);
                        targetPos = altPos;
                        moved = true;
                        break;
                    }
                }

                if (!moved)
                    break; // nowhere to go
            }
            else
            {
                //Move towards Player
                yield return FacePlayer();
                yield return MoveTo(nextPos);
                targetPos = nextPos;
            }
        }

        // Update grid positions
        gridManager.GetTile(gridPosition).occupant = null;
        gridPosition = targetPos;
        gridManager.GetTile(gridPosition).occupant = gameObject;

        // After moving, check if now in attack range
        distToPlayer = Mathf.Abs(playerTile.gridPosition.x - gridPosition.x) +
                       Mathf.Abs(playerTile.gridPosition.y - gridPosition.y);

        if (distToPlayer <= attackRange)
        {
            yield return FacePlayer();
            Debug.Log($"{name} attacks player after moving!");
            yield return new WaitForSeconds(0.5f);
        }

        onComplete?.Invoke();
    }

    private IEnumerator MoveTo(Vector2Int targetPos)
    {
        Vector3 start = transform.position;
        Vector3 end = new Vector3(targetPos.x, yPos, targetPos.y);
        float t = 0;

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        transform.position = end;
    }

    private IEnumerator FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0; // keep rotation only on the Y-axis
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float t = 0f;
            float rotateSpeed = 10f; // adjust as needed

            while (t < 1f)
            {
                t += Time.deltaTime * rotateSpeed;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
                yield return null;
            }

            transform.rotation = targetRotation;
        }
    }

    private void InitializeSpells()
    {
        for(int i =0; i< spells.Length;i++)
        {
            spells[i] = JsonManager.Instance.ReturnRandomSpell();
        }
    }

}
