using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

public class Enemy : MonoBehaviour
{
    public float yPos = 1.5f;
    public Vector2Int gridPosition;
    public int moveRange = 3;
    public int attackRange = 1;
    public float moveSpeed = 5f;

    private GridManager gridManager;
    private GameObject player;

    public Spell[] spells = new Spell[4];
    public int preffered = 0;

    public bool debugMode = true;

    public float dmgMultiplier = 1.0f;

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape) && debugMode)
        {
            EnemyManager.Instance.ClearScoreDebug();
        }
    }

    public void Initialize(Vector2Int startPos, GridManager grid, GameObject playerRef)
    {
        gridPosition = startPos;
        gridManager = grid;
        player = playerRef;

        transform.position = new Vector3(startPos.x, yPos, startPos.y);
        grid.GetTile(startPos).occupant = gameObject;

        StartCoroutine(FacePlayer());

        InitializeSpells();
        PrioriizeSpell();
    }

    public IEnumerator TakeTurn(System.Action onComplete)
    {

        EnemyManager.Instance.calculateThreatGrid();
        Dictionary<String, object> result = new Dictionary<String, object>();
        result = EnemyManager.Instance.CalculateScore(this);

        Vector2Int location = new Vector2Int();
        GameObject target = null;
        Spell spell = null;

        location = (Vector2Int)result["location"];
        yield return MoveTo(location);
        Debug.Log("Enemy moving to " + location);
        if (result.ContainsKey("target"))
        {  
            target = (GameObject)result["target"];
            spell = (Spell)result["spell"];
            Debug.Log("Attempting to hit target " + target.name + " with spell " + spell.name);
            AttackPlayer(spell, target);
        }

        SyncGridPosition();
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
        SyncGridPosition();
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


    private IEnumerator FacePlayer()
    {
        Vector3 direction = player.transform.position - transform.position;
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

        for (int i = 0; i < spells.Length; i++)
        {
            spells[i].damage = (int)(spells[i].damage * dmgMultiplier);
        }
    }

    public void PrioriizeSpell()
    {
        int rando = UnityEngine.Random.Range(0, spells.Length);
        preffered = rando;
    }

    public void CheckAnySpellRange(int distToPlayer)
    {

        for (int i = 0; i < spells.Length; i++)
        {
            if (distToPlayer <= spells[i].range)
            {
                attackRange = spells[i].range;
                preffered = i;
            }
        }
    }

    public void AttackPlayer()
    {
        if (ActionManager.Instance == null)
        {
            Debug.LogError("ActionManager.Instance is null.");
        }


        StartCoroutine(FacePlayer());
        Debug.Log("Attacking Player with " + spells[preffered].name);
        ActionManager.Instance.UseSpell(spells[preffered], this.gameObject, player.gameObject);
    }

    public void AttackPlayer(Spell spell, GameObject target)
    {
        if (ActionManager.Instance == null)
        {
            Debug.LogError("ActionManager.Instance is null.");
        }


        StartCoroutine(FacePlayer());
        Debug.Log("Attacking Player with " + spell.name);
        ActionManager.Instance.UseSpell(spell, this.gameObject, target);
    }

    private void OnMouseDown()
    {
        if(debugMode)
        {
            EnemyManager.Instance.ClearScoreDebug();
            EnemyManager.Instance.ShowScoreGrid(this);
        }
    }

    private void SetMulitplier(float multiplier)
    {
        dmgMultiplier = multiplier;
    }
    
}
